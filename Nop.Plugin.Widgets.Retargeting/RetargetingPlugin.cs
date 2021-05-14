using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Tax;
using Nop.Plugin.Widgets.Retargeting.Infrastructure.Cache;
using Nop.Services.Caching;
using Nop.Services.Catalog;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Tax;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Widgets.Retargeting
{
    public class RetargetingPlugin : BasePlugin, IWidgetPlugin
    {
        #region Fields

        private readonly ITaxService _taxService;
        private readonly IOrderService _orderService;
        private readonly ISettingService _settingService;
        private readonly IProductService _productService;
        private readonly IDiscountService _discountService;
        private readonly IPermissionService _permissionService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IPictureService _pictureService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly ICacheKeyService _cacheKeyService;
        private readonly ILocalizationService _localizationService;
        private readonly IAddressService _addressService;
        private readonly IStateProvinceService _stateProvinceService;

        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IStaticCacheManager _staticCacheManager;

        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly OrderSettings _orderSettings;
        private readonly MediaSettings _mediaSettings;

        #endregion

        #region Ctor

        public RetargetingPlugin(
            ITaxService taxService,
            IOrderService orderService,
            ISettingService settingService,
            IProductService productService,
            IDiscountService discountService,
            IPermissionService permissionService,
            IShoppingCartService shoppingCartService,
            IProductAttributeService productAttributeService,
            IPriceCalculationService priceCalculationService,
            IUrlRecordService urlRecordService,
            IPictureService pictureService,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            ICacheKeyService cacheKeyService,
            ILocalizationService localizationService,
            IAddressService addressService,
            IStateProvinceService stateProvinceService,

            ILogger logger,
            IWebHelper webHelper,
            IWorkContext workContext,
            IStoreContext storeContext,
            IProductAttributeParser productAttributeParser,
            IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccessor,
            IStaticCacheManager staticCacheManager,

            ShoppingCartSettings shoppingCartSettings,
            OrderSettings orderSettings,
            MediaSettings mediaSettings)
        {
            _taxService = taxService;
            _orderService = orderService;
            _settingService = settingService;
            _productService = productService;
            _discountService = discountService;
            _permissionService = permissionService;
            _shoppingCartService = shoppingCartService;
            _productAttributeService = productAttributeService;
            _priceCalculationService = priceCalculationService;
            _urlRecordService = urlRecordService;
            _pictureService = pictureService;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _cacheKeyService = cacheKeyService;
            _localizationService = localizationService;
            _addressService = addressService;
            _stateProvinceService = stateProvinceService;

            _logger = logger;
            _webHelper = webHelper;
            _workContext = workContext;
            _storeContext = storeContext;
            _productAttributeParser = productAttributeParser;
            _urlHelperFactory = urlHelperFactory;
            _actionContextAccessor = actionContextAccessor;
            _staticCacheManager = staticCacheManager;

            _shoppingCartSettings = shoppingCartSettings;
            _mediaSettings = mediaSettings;
            _orderSettings = orderSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get the combination code
        /// </summary>
        /// <param name="attributeCombinationXml">Attribute combination Xml</param>
        /// <returns></returns>
        public string GetCombinationCode(string attributeCombinationXml)
        {
            var result = "";

            var attributes = _productAttributeParser.ParseProductAttributeMappings(attributeCombinationXml);
            for (var i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];
                var valuesStr = _productAttributeParser.ParseValues(attributeCombinationXml, attribute.Id);

                for (var j = 0; j < valuesStr.Count; j++)
                {
                    if (attribute.ShouldHaveValues() && !string.IsNullOrEmpty(valuesStr[j]))
                    {
                        result += valuesStr[j];
                        if (i != attributes.Count - 1 || j != valuesStr.Count - 1)
                            result += "-";
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Normalizes a string value
        /// </summary>
        /// <param name="stringValue">Normalized value</param>
        /// <returns></returns>
        public string NormalizeStringValue(string stringValue)
        {
            return stringValue.Trim().Replace(",", "&#44;").Replace("\"", "&#34;");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets widget zones where this widget should be rendered
        /// </summary>
        /// <returns>Widget zones</returns>
        public IList<string> GetWidgetZones()
        {
            return new List<string>() { PublicWidgetZones.ContentBefore };
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return _webHelper.GetStoreLocation() + "Admin/WidgetsRetargeting/Configure";
        }

        /// <summary>
        /// Gets a name of a view component for displaying widget
        /// </summary>
        /// <param name="widgetZone">Name of the widget zone</param>
        /// <returns>View component name</returns>
        public string GetWidgetViewComponentName(string widgetZone)
        {
            return RetargetingDefaults.RETARGETING_VIEW_COMPONENT_NAME;
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new RetargetingSettings()
            {
                ProductBoxSelector = RetargetingDefaults.ProductBoxSelector,
                AddToCartCatalogButtonSelector = RetargetingDefaults.AddToCartCatalogButtonSelector,
                AddToCartButtonIdDetailsPrefix = RetargetingDefaults.AddToCartButtonIdDetailsPrefix,
                AddToWishlistCatalogButtonSelector = RetargetingDefaults.AddToWishlistCatalogButtonSelector,
                AddToWishlistButtonIdDetailsPrefix = RetargetingDefaults.AddToWishlistButtonIdDetailsPrefix,
                ProductPriceLabelDetailsSelector = RetargetingDefaults.ProductPriceLabelDetailsSelector,
                ProductMainPictureIdDetailsPrefix = RetargetingDefaults.ProductMainPictureIdDetailsPrefix,
                HelpTopicSystemNames = RetargetingDefaults.HelpTopicSystemNames,
                ProductReviewAddedResultSelector = RetargetingDefaults.ProductReviewAddedResultSelector,

                MerchantEmail = "",

                RecommendationHomePage = false,
                RecommendationCategoryPage = false,
                RecommendationProductPage = false,
                RecommendationCheckoutPage = false,
                RecommendationThankYouPage = false,
                RecommendationOutOfStockPage = false,
                RecommendationSearchPage = false,
                RecommendationPageNotFound = false
            };
            _settingService.SaveSetting(settings);

            //locales
            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
            { 
                ["Plugins.Widgets.Retargeting.Configuration"] = "Configuration",
                ["Plugins.Widgets.Retargeting.PreconfigureSystem"] = "Preconfigure system",
                ["Plugins.Widgets.Retargeting.PreconfigureButton"] = "Preconfigure",
                ["Plugins.Widgets.Retargeting.PreconfigureCompleted"] = "Preconfigure completed",
                ["Plugins.Widgets.Retargeting.PreconfigureError"] = "Preconfigure error",
                ["Plugins.Widgets.Retargeting.ResetSettings"] = "Reset settings",
                ["Plugins.Widgets.Retargeting.SettingsReset"] = "Settings have been reset",
                ["Plugins.Widgets.Retargeting.ExceptionLoadPlugin"] = "Cannot load the plugin",
                
                ["Plugins.Widgets.Retargeting.TrackingApiKey"] = "Tracking API KEY",
                ["Plugins.Widgets.Retargeting.TrackingApiKey.Hint"] = "To use Retargeting you need the Tracking API KEY from your Retargeting account.",
                ["Plugins.Widgets.Retargeting.RestApiKey"] = "REST API KEY",
                ["Plugins.Widgets.Retargeting.RestApiKey.Hint"] = "To use the REST API you need the REST API KEY from your Retargeting account.",
                ["Plugins.Widgets.Retargeting.UseHttpPostInsteadOfAjaxInAddToCart"] = "Use HTTP POST method (add to cart)",
                ["Plugins.Widgets.Retargeting.UseHttpPostInsteadOfAjaxInAddToCart.Hint"] = "Check to use the HTTP POST method for adding to cart(wishlist) instead of ajax.",
                ["Plugins.Widgets.Retargeting.ProductBoxSelector"] = "Product box selector",
                ["Plugins.Widgets.Retargeting.ProductBoxSelector.Hint"] = "Product box selector.",
                ["Plugins.Widgets.Retargeting.AddToCartCatalogButtonSelector"] = "Add to cart button selector (catalog)",
                ["Plugins.Widgets.Retargeting.AddToCartCatalogButtonSelector.Hint"] = "Add to cart button selector (catalog).",
                ["Plugins.Widgets.Retargeting.AddToCartButtonIdDetailsPrefix"] = "Add to cart button id prefix (product details)",
                ["Plugins.Widgets.Retargeting.AddToCartButtonIdDetailsPrefix.Hint"] = "Add to cart button id prefix (product details).",
                ["Plugins.Widgets.Retargeting.AddToWishlistCatalogButtonSelector"] = "Add to wishlist button selector (catalog)",
                ["Plugins.Widgets.Retargeting.AddToWishlistCatalogButtonSelector.Hint"] = "Add to wishlist button selector (catalog).",
                ["Plugins.Widgets.Retargeting.AddToWishlistButtonIdDetailsPrefix"] = "Add to wishlist button id prefix (product details)",
                ["Plugins.Widgets.Retargeting.AddToWishlistButtonIdDetailsPrefix.Hint"] = "Add to wishlist button id  (product details).",
                ["Plugins.Widgets.Retargeting.ProductPriceLabelDetailsSelector"] = "Price label selector (product details)",
                ["Plugins.Widgets.Retargeting.ProductPriceLabelDetailsSelector.Hint"] = "Price label selector (product details).",
                ["Plugins.Widgets.Retargeting.ProductMainPictureIdDetailsPrefix"] = "Product main picture id prefix",
                ["Plugins.Widgets.Retargeting.ProductMainPictureIdDetailsPrefix.Hint"] = "Product main picture id prefix (required only if main picture zoom is enabled).",
                ["Plugins.Widgets.Retargeting.HelpTopicSystemNames"] = "Help topic system names",
                ["Plugins.Widgets.Retargeting.HelpTopicSystemNames.Hint"] = "Comma separated help topic system names.",
                ["Plugins.Widgets.Retargeting.ProductReviewAddedResultSelector"] = "Product review added result selector",
                ["Plugins.Widgets.Retargeting.ProductReviewAddedResultSelector.Hint"] = "Product review added result selector.",
                ["Plugins.Widgets.Retargeting.DiscountTypeNote"] = "Note: Retargeting can generate discounts through it's API. One of the generated discount types is Custom. We allow Retargeting to generate only Free Shipping discount as a Custom discount type.",
                ["Plugins.Widgets.Retargeting.CustomizationNote"] = "Note: this plugin may work incorrectly in case you made some customization of your website.",
                ["Plugins.Widgets.Retargeting.SubscribeRetargeting"] = "Please enter your email to receive an information about special offers from Retargeting.",
                ["Plugins.Widgets.Retargeting.Subscribe"] = "Subscribe",
                ["Plugins.Widgets.Retargeting.MerchantEmail"] = "Email",
                ["Plugins.Widgets.Retargeting.MerchantEmail.Hint"] = "Enter your email to subscribe to Retargeting news.",
                ["Plugins.Widgets.Retargeting.Subscribe.Error"] = "An error has occurred] = details in the log",
                ["Plugins.Widgets.Retargeting.Subscribe.Success"] = "You have subscribed to Retargeting news",
                ["Plugins.Widgets.Retargeting.Unsubscribe.Success"] = "You have unsubscribed from Retargeting news",
               
                ["Plugins.Widgets.Retargeting.RecommendationHomePage"] = "Recommendation for Home page",
                ["Plugins.Widgets.Retargeting.RecommendationHomePage.Hint"] = "To use Retargeting Recommendation Engine for Home page.",
                ["Plugins.Widgets.Retargeting.RecommendationCategoryPage"] = "Recommendation for Category page",
                ["Plugins.Widgets.Retargeting.RecommendationCategoryPage.Hint"] = "To use Retargeting Recommendation Engine for Category page.",
                ["Plugins.Widgets.Retargeting.RecommendationProductPage"] = "Recommendation for Product page",
                ["Plugins.Widgets.Retargeting.RecommendationProductPage.Hint"] = "To use Retargeting Recommendation Engine for Product page.",
                ["Plugins.Widgets.Retargeting.RecommendationCheckoutPage"] = "Recommendation for Checkout page",
                ["Plugins.Widgets.Retargeting.RecommendationCheckoutPage.Hint"] = "To use Retargeting Recommendation Engine for Checkout page.",
                ["Plugins.Widgets.Retargeting.RecommendationThankYouPage"] = "Recommendation for Thank You Page",
                ["Plugins.Widgets.Retargeting.RecommendationThankYouPage.Hint"] = "To use Retargeting Recommendation Engine for Thank You page.",
                ["Plugins.Widgets.Retargeting.RecommendationOutOfStockPage"] = "Recommendation for Out Of Stock page",
                ["Plugins.Widgets.Retargeting.RecommendationOutOfStockPage.Hint"] = "To use Retargeting Recommendation Engine for Out Of Stock page.",
                ["Plugins.Widgets.Retargeting.RecommendationSearchPage"] = "Recommendation for Search page",
                ["Plugins.Widgets.Retargeting.RecommendationSearchPage.Hint"] = "To use Retargeting Recommendation Engine for Search page.",
                ["Plugins.Widgets.Retargeting.RecommendationPageNotFound"] = "Recommendation for Page Not Found",
                ["Plugins.Widgets.Retargeting.RecommendationPageNotFound.Hint"] = "To use Retargeting Recommendation Engine for Page Not Found."
            });

            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<RetargetingSettings>();

            //locales
            _localizationService.DeletePluginLocaleResources("Plugins.Widgets.Retargeting.Configuration");

            base.Uninstall();
        }

        public void Preconfigure()
        {
            _shoppingCartSettings.DisplayCartAfterAddingProduct = false;
            _shoppingCartSettings.DisplayWishlistAfterAddingProduct = false;
            _settingService.SaveSetting(_shoppingCartSettings);

            _orderSettings.DisableOrderCompletedPage = false;
            _settingService.SaveSetting(_orderSettings);

            _mediaSettings.DefaultPictureZoomEnabled = true;
            _settingService.SaveSetting(_mediaSettings);
        }

        /// <summary>
        /// Export products to CSV
        /// </summary>
        /// <returns>Result in CSV format</returns>
        public string ExportProductsToCsv()
        {
            const string separator = ",";
            var sb = new StringBuilder();

            //get URL helper
            var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

            //headers
            sb.Append("product id,product name,product url,image url,stock,price,sale price,brand,category,extra data");
            sb.Append(Environment.NewLine);

            var products = _productService.SearchProducts(visibleIndividuallyOnly: true);
            foreach (var product in products)
            {
                var productsToProcess = new List<Product>();
                switch (product.ProductType)
                {
                    case ProductType.SimpleProduct:
                        {
                            //simple product doesn't have child products
                            productsToProcess.Add(product);
                        }
                        break;
                    case ProductType.GroupedProduct:
                        {
                            //grouped products could have several child products
                            var associatedProducts = _productService.GetAssociatedProducts(product.Id);

                            if (product.VisibleIndividually)
                                productsToProcess.AddRange(associatedProducts);
                        }
                        break;
                    default:
                        continue;
                }

                foreach (var currentProduct in productsToProcess)
                {
                    //product id
                    sb.Append(currentProduct.Id);
                    sb.Append(separator);

                    //product name
                    sb.Append(NormalizeStringValue(currentProduct.Name));
                    sb.Append(separator);

                    //product url
                    sb.Append(urlHelper.RouteUrl("Product", new { SeName = _urlRecordService.GetSeName(product) }, _webHelper.IsCurrentConnectionSecured() ? "https" : "http"));
                    sb.Append(separator);

                    //image url
                    var productPicturesCacheKey = _cacheKeyService.PrepareKeyForDefaultCache(ModelCacheEventConsumer.ProductPicturesModelKey, currentProduct.Id, _webHelper.IsCurrentConnectionSecured(), _storeContext.CurrentStore.Id);

                    var cachedProductPictures = _staticCacheManager.Get(productPicturesCacheKey, () =>
                    {
                        return _pictureService.GetPicturesByProductId(currentProduct.Id);
                    });

                    var imageUrl = cachedProductPictures.FirstOrDefault() != null ? _pictureService.GetPictureUrl(cachedProductPictures.FirstOrDefault().Id) : "";

                    sb.Append(imageUrl);
                    sb.Append(separator);

                    //stock
                    sb.Append(currentProduct.StockQuantity);
                    sb.Append(separator);

                    //price
                    GetProductPrice(currentProduct, out var price, out var priceWithDiscount);

                    sb.Append(price);
                    sb.Append(separator);

                    //sale price
                    sb.Append(priceWithDiscount);
                    sb.Append(separator);

                    //brand
                    var manufacturerName = "";
                    var productManufacturers = _manufacturerService.GetProductManufacturersByProductId(currentProduct.Id);
                    var defaultProductManufacturer = productManufacturers.FirstOrDefault();

                    if (defaultProductManufacturer != null)
                        manufacturerName = NormalizeStringValue(_manufacturerService.GetManufacturerById(defaultProductManufacturer.ManufacturerId).Name);

                    sb.Append(manufacturerName);
                    sb.Append(separator);

                    //category
                    var categoryName = "Default category";
                    var productCategories = _categoryService.GetProductCategoriesByProductId(currentProduct.Id);
                    var defaultProductCategory = productCategories.FirstOrDefault();

                    if (defaultProductCategory != null)
                        categoryName = NormalizeStringValue((_categoryService.GetCategoryById(defaultProductCategory.CategoryId)).Name);

                    sb.Append(categoryName);
                    sb.Append(separator);

                    //extra data
                    //categories (breadcrumb)
                    var categoryString = string.Empty;
                    if (defaultProductCategory != null)
                    {
                        var categoryBreadcrumb = _categoryService.GetCategoryBreadCrumb(_categoryService.GetCategoryById(defaultProductCategory.CategoryId)).Select(catBr => NormalizeStringValue(_localizationService.GetLocalized(catBr, x => x.Name))).ToList();

                        categoryString = string.Join("|", categoryBreadcrumb);
                    }
                    else
                    {
                        categoryString = categoryName;
                    }

                    //attributes
                    var variations = new List<Dictionary<string, object>>();
                    var attributeCodes = new List<string>();

                    var allAttributesXml = _productAttributeParser.GenerateAllCombinations(currentProduct, true);
                    foreach (var attributesXml in allAttributesXml)
                    {
                        var warnings = new List<string>();
                        warnings.AddRange(_shoppingCartService.GetShoppingCartItemAttributeWarnings(_workContext.CurrentCustomer,
                            ShoppingCartType.ShoppingCart, currentProduct, 1, attributesXml, true));
                        if (warnings.Count != 0)
                            continue;

                        var existingCombination = _productAttributeParser.FindProductAttributeCombination(currentProduct, attributesXml);
                        if (existingCombination != null)
                        {
                            var varCode = GetCombinationCode(attributesXml);
                            if (!attributeCodes.Contains(varCode))
                            {
                                attributeCodes.Add(varCode);

                                var combinationPrice = existingCombination.OverriddenPrice != null ? existingCombination.OverriddenPrice.Value.ToString(new CultureInfo("en-US", false).NumberFormat) : price;
                                var combinationPriceWithDiscount = existingCombination.OverriddenPrice != null ? existingCombination.OverriddenPrice.Value.ToString(new CultureInfo("en-US", false).NumberFormat) : priceWithDiscount;

                                variations.Add(new Dictionary<string, object> {
                                    {"\"code\"", $"\"{varCode}\"" },
                                    {"\"price\"", $"\"{combinationPrice}\"" },
                                    {"\"sale price\"", $"\"{combinationPriceWithDiscount}\"" },
                                    {"\"stock\"", existingCombination.StockQuantity},
                                    {"\"margin\"", null},
                                    {"\"in_supplier_stock\"", existingCombination.StockQuantity > 0 }
                                });
                            }
                        }
                    }

                    //media gallery
                    var imageUrls = new List<string>();
                    foreach (var picture in cachedProductPictures)
                        imageUrls.Add($"\"{_pictureService.GetPictureUrl(picture.Id)}\"".Replace("/", "\\/"));

                    //extra data object
                    var extraData = new Dictionary<string, object>
                    {
                        {"\"margin\"", null},
                        {"\"categories\"", $"\"{categoryString}\"" },
                        {"\"variations\"",  variations},
                        {"\"media_gallery\"", imageUrls},
                        {"\"in_supplier_stock\"", currentProduct.StockQuantity > 0 }
                    };

                    sb.Append($"\"{Regex.Unescape(JsonConvert.SerializeObject(extraData))}\"");
                    sb.Append(separator);

                    sb.Append(Environment.NewLine);
                }
            }
            return sb.ToString();
        }

        public void GetProductPrice(Product product, out string price, out string priceWithDiscount)
        {
            var priceBase = decimal.Zero;
            var priceWithDiscountBase = decimal.Zero;

            if (_permissionService.Authorize(StandardPermissionProvider.DisplayPrices))
            {
                if (!product.CustomerEntersPrice)
                {
                    if (!product.CallForPrice)
                    {
                        priceBase = _taxService.GetProductPrice(product, _priceCalculationService.GetFinalPrice(product, _workContext.CurrentCustomer, includeDiscounts: false), out _);
                        priceWithDiscountBase = _taxService.GetProductPrice(product, _priceCalculationService.GetFinalPrice(product, _workContext.CurrentCustomer, includeDiscounts: true), out _);
                    }
                }
            }

            //Retargeting doesn't allow price = 0
            if (priceBase == decimal.Zero)
                priceBase = decimal.One;

            //Retargeting doesn't allow price with discount = 0; in this case we should use a price without discount
            if (priceWithDiscountBase == decimal.Zero)
                priceWithDiscountBase = priceBase;

            price = priceBase.ToString(new CultureInfo("en-US", false).NumberFormat);
            priceWithDiscount = priceWithDiscountBase.ToString(new CultureInfo("en-US", false).NumberFormat);
        }

        public void SendOrder(int orderId)
        {
            var retargetingSettings = _settingService.LoadSetting<RetargetingSettings>(_storeContext.CurrentStore.Id);

            if (!string.IsNullOrEmpty(retargetingSettings.RestApiKey))
            {
                var order = _orderService.GetOrderById(orderId);
                if (order != null && !order.Deleted && _workContext.CurrentCustomer.Id == order.CustomerId)
                {
                    var sb = new StringBuilder();

                    var billingAddress = _addressService.GetAddressById(order.BillingAddressId);

                    sb.AppendFormat("api_key={0}&", retargetingSettings.RestApiKey);
                    sb.AppendFormat("0[order_no]={0}&", order.Id);
                    sb.AppendFormat("0[lastname]={0}&", WebUtility.UrlEncode(billingAddress?.LastName));
                    sb.AppendFormat("0[firstname]={0}&", WebUtility.UrlEncode(billingAddress?.FirstName));
                    sb.AppendFormat("0[email]={0}&", WebUtility.UrlEncode(billingAddress?.Email));
                    sb.AppendFormat("0[phone]={0}&", WebUtility.UrlEncode(billingAddress?.PhoneNumber));
                    sb.AppendFormat("0[state]={0}&", WebUtility.UrlEncode(_stateProvinceService.GetStateProvinceByAddress(billingAddress)?.Name));
                    sb.AppendFormat("0[city]={0}&", WebUtility.UrlEncode(billingAddress?.City));
                    sb.AppendFormat("0[adress]={0}&", WebUtility.UrlEncode($"{billingAddress?.Address1} {billingAddress?.Address2}"));
                    sb.AppendFormat("0[discount]={0}&", order.OrderDiscount.ToString("0.00", CultureInfo.InvariantCulture));
                    sb.AppendFormat("0[shipping]={0}&", order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax
                                                        ? order.OrderShippingInclTax.ToString("0.00", CultureInfo.InvariantCulture)
                                                        : order.OrderShippingExclTax.ToString("0.00", CultureInfo.InvariantCulture));
                    sb.AppendFormat("0[rebates]={0}&", 0);
                    sb.AppendFormat("0[fees]={0}&", 0);
                    sb.AppendFormat("0[total]={0}&", order.OrderTotal.ToString("0.00", CultureInfo.InvariantCulture));

                    //discount codes
                    var discountCode = "";
                    var discountsWithCouponCodes =
                            _discountService.GetAllDiscountUsageHistory(orderId: order.Id)
                                .Where(x => !string.IsNullOrEmpty(_discountService.GetDiscountById(x.DiscountId)?.CouponCode))
                                .ToList();
                    for (var i = 0; i < discountsWithCouponCodes.Count; i++)
                    {
                        discountCode += _discountService.GetDiscountById(discountsWithCouponCodes[i].DiscountId).CouponCode;

                        if (i < discountsWithCouponCodes.Count - 1)
                            discountCode += ",";
                    }
                    sb.AppendFormat("0[discount_code]={0}&", discountCode);

                    //order items
                    var orderItems = _orderService.GetOrderItems(order.Id);
                    for (var i = 0; i < orderItems.Count; i++)
                    {
                        sb.AppendFormat("1[{0}][id]={1}&", i, orderItems.ElementAt(i).Id);
                        sb.AppendFormat("1[{0}][quantity]={1}&", i, orderItems.ElementAt(i).Quantity);
                        sb.AppendFormat("1[{0}][price]={1}&", i, order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax
                                                                                    ? orderItems.ElementAt(i).UnitPriceInclTax
                                                                                    : orderItems.ElementAt(i).UnitPriceExclTax);
                        var variationCode = "";
                        var values = _productAttributeParser.ParseProductAttributeValues(orderItems.ElementAt(i).AttributesXml);
                        for (var j = 0; j < values.Count; j++)
                        {
                            variationCode += values[j].Id;
                            if (j < values.Count - 1)
                                variationCode += "-";
                        }
                        sb.AppendFormat("1[{0}][variation_code]={1}&", i, variationCode);
                    }

                    //send order using Retargeting REST API 
                    var restApiHelper = new RetargetingRestApiHelper();
                    var response = restApiHelper.GetJson(_logger, "https://retargeting.biz/api/1.0/order/save.json", HttpMethod.Post, sb.ToString());

                    //order note
                    _orderService.InsertOrderNote(new OrderNote
                    {
                        OrderId = order.Id,
                        Note = string.Format("Retargeting REST API. Saving the order data result: {0}", response),
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    _orderService.UpdateOrder(order);
                }
            }
        }

        public bool IsProductCombinationInStock(Product product, string attributeXml,
            out string variationCode, out Dictionary<string, object> variationDetails)
        {
            variationCode = "";
            variationDetails = new Dictionary<string, object>();

            if (product == null)
                return false;

            var productIsInStock = false;

            switch (product.ManageInventoryMethod)
            {
                case ManageInventoryMethod.ManageStock:
                    {
                        #region Manage stock

                        if (!product.DisplayStockAvailability)
                            productIsInStock = true;

                        var stockQuantity = _productService.GetTotalStockQuantity(product);
                        if (stockQuantity > 0)
                            productIsInStock = true;
                        else
                            //out of stock
                            switch (product.BackorderMode)
                            {
                                case BackorderMode.AllowQtyBelow0:
                                    productIsInStock = true;
                                    break;
                                case BackorderMode.AllowQtyBelow0AndNotifyCustomer:
                                    productIsInStock = true;
                                    break;
                                case BackorderMode.NoBackorders:
                                default:
                                    break;
                            }

                        #endregion
                    }
                    break;

                case ManageInventoryMethod.ManageStockByAttributes:
                    {
                        #region Manage stock by attributes

                        if (!product.DisplayStockAvailability)
                            productIsInStock = true;

                        var combination = _productAttributeParser.FindProductAttributeCombination(product, attributeXml);
                        if (combination != null)
                        {
                            //combination exists
                            var stockQuantity = combination.StockQuantity;
                            if (stockQuantity > 0)
                                productIsInStock = true;
                            else if (combination.AllowOutOfStockOrders)
                                productIsInStock = true;
                        }
                        else
                        {
                            //no combination configured
                            if (!product.AllowAddingOnlyExistingAttributeCombinations)
                                productIsInStock = true;
                        }

                        #endregion
                    }
                    break;
                case ManageInventoryMethod.DontManageStock:
                default:
                    productIsInStock = true;
                    break;
            }

            var values = _productAttributeParser.ParseProductAttributeValues(attributeXml);
            if (values.Count > 0)
            {
                for (var i = 0; i < values.Count; i++)
                {
                    variationCode += values[i].Id;
                    if (i < values.Count - 1)
                        variationCode += "-";

                    var attributeMapping = _productAttributeService.GetProductAttributeMappingById(values[i].ProductAttributeMappingId);
                    var productAttribute = _productAttributeService.GetProductAttributeById(attributeMapping.ProductAttributeId);
                    if (attributeMapping != null && productAttribute != null)
                    {
                        variationDetails.Add(
                            values[i].Id.ToString(),
                            new
                            {
                                category_name = _localizationService.GetLocalized(productAttribute, x => x.Name),
                                category = productAttribute.Id,
                                value = values[i].Name
                            });
                    }
                }
            }

            return productIsInStock;
        }

        #endregion

        /// <summary>
        /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
        /// </summary>
        public bool HideInWidgetList => false;
    }
}