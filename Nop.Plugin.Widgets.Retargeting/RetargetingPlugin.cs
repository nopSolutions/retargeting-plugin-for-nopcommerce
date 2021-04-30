using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
using Nop.Services.Catalog;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
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
    /// <summary>
    /// PLugin
    /// </summary>
    public class RetargetingPlugin : BasePlugin, IWidgetPlugin
    {
        #region Fields

        private readonly IAddressService _addressService;
        private readonly IDiscountService _discountService;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderService _orderService;
        private readonly IPermissionService _permissionService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductService _productService;
        private readonly ISettingService _settingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ITaxService _taxService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IPictureService _pictureService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;

        private readonly ILogger _logger;
        private readonly IStoreContext _storeContext;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IActionContextAccessor _actionContextAccessor;

        private readonly MediaSettings _mediaSettings;
        private readonly OrderSettings _orderSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        #endregion

        #region Ctor

        public RetargetingPlugin(
            IAddressService addressService,
            IDiscountService discountService,
            ILocalizationService localizationService,
            IOrderService orderService,
            IPermissionService permissionService,
            IPriceCalculationService priceCalculationService,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IProductService productService,
            ISettingService settingService,
            IShoppingCartService shoppingCartService,
            IStateProvinceService stateProvinceService,
            ITaxService taxService,
            IUrlRecordService urlRecordService,
            IPictureService pictureService,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,

            ILogger logger,
            IStoreContext storeContext,
            IWebHelper webHelper,
            IWorkContext workContext,
            IStaticCacheManager staticCacheManager,
            IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccessor,

            MediaSettings mediaSettings,
            OrderSettings orderSettings,
            ShoppingCartSettings shoppingCartSettings)
        {
            _addressService = addressService;
            _discountService = discountService;
            _localizationService = localizationService;
            _orderService = orderService;
            _permissionService = permissionService;
            _priceCalculationService = priceCalculationService;
            _productAttributeParser = productAttributeParser;
            _productAttributeService = productAttributeService;
            _productService = productService;
            _settingService = settingService;
            _shoppingCartService = shoppingCartService;
            _stateProvinceService = stateProvinceService;
            _taxService = taxService;
            _urlRecordService = urlRecordService;
            _pictureService = pictureService;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;

            _logger = logger;
            _storeContext = storeContext;
            _webHelper = webHelper;
            _workContext = workContext;
            _staticCacheManager = staticCacheManager;
            _urlHelperFactory = urlHelperFactory;
            _actionContextAccessor = actionContextAccessor;

            _mediaSettings = mediaSettings;
            _orderSettings = orderSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get the combination code
        /// </summary>
        /// <param name="attributeCombinationXml">Attribute combination Xml</param>
        /// <returns></returns>
        public async Task<string> GetCombinationCodeAsync(string attributeCombinationXml)
        {
            var result = "";

            var attributes = await _productAttributeParser.ParseProductAttributeMappingsAsync(attributeCombinationXml);
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
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the widget zones
        /// </returns>
        public Task<IList<string>> GetWidgetZonesAsync()
        {
            return Task.FromResult<IList<string>>(new List<string> { PublicWidgetZones.ContentBefore });
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
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task InstallAsync()
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
            await _settingService.SaveSettingAsync(settings);

            //locales
            await _localizationService.AddLocaleResourceAsync(new Dictionary<string, string>
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
                ["Plugins.Widgets.Retargeting.AddToWishlistButtonIdDetailsPrefix.Hint"] = "Add to wishlist button id (product details).",
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
                ["Plugins.Widgets.Retargeting.Subscribe.Error"] = "An error has occurred. See details in the log",
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

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UninstallAsync()
        {
            //settings
            await _settingService.DeleteSettingAsync<RetargetingSettings>();

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Widgets.Retargeting.Configuration");

            await base.UninstallAsync();
        }

        /// <summary>
        /// Preconfigure plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task PreconfigureAsync()
        {
            _shoppingCartSettings.DisplayCartAfterAddingProduct = false;
            _shoppingCartSettings.DisplayWishlistAfterAddingProduct = false;
            await _settingService.SaveSettingAsync(_shoppingCartSettings);

            _orderSettings.DisableOrderCompletedPage = false;
            await _settingService.SaveSettingAsync(_orderSettings);

            _mediaSettings.DefaultPictureZoomEnabled = true;
            await _settingService.SaveSettingAsync(_mediaSettings);
        }

        /// <summary>
        /// Export products to CSV
        /// </summary>
        /// <returns>Result in CSV format</returns>
        public async Task<string> ExportProductsToCsvAsync()
        {
            const char separator = ',';
            var sb = new StringBuilder();

            //get URL helper
            var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);

            //headers
            sb.Append("product id,product name,product url,image url,stock,price,sale price,brand,category,extra data");
            sb.Append(Environment.NewLine);

            var products = await _productService.SearchProductsAsync(visibleIndividuallyOnly: true);
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
                            var associatedProducts = await _productService.GetAssociatedProductsAsync(product.Id);

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
                    sb.Append(urlHelper.RouteUrl("Product", new { SeName = await _urlRecordService.GetSeNameAsync(currentProduct) }, _webHelper.GetCurrentRequestProtocol()));
                    sb.Append(separator);

                    //image url
                    var productPicturesCacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ModelCacheEventConsumer.ProductPicturesModelKey, currentProduct.Id, _webHelper.IsCurrentConnectionSecured() ? Uri.UriSchemeHttps : Uri.UriSchemeHttp, (await _storeContext.GetCurrentStoreAsync()).Id);

                    var cachedProductPictures = await _staticCacheManager.GetAsync(productPicturesCacheKey, async () =>
                    {
                        return await _pictureService.GetPicturesByProductIdAsync(currentProduct.Id);
                    });

                    var imageUrl = cachedProductPictures.FirstOrDefault() != null ? (await _pictureService.GetPictureUrlAsync(cachedProductPictures.FirstOrDefault())).Url : "";

                    sb.Append(imageUrl);
                    sb.Append(separator);

                    //stock
                    sb.Append(currentProduct.StockQuantity);
                    sb.Append(separator);

                    //price
                    var (price, priceWithDiscount) = await GetProductPriceAsync(currentProduct);

                    sb.Append(price);
                    sb.Append(separator);

                    //sale price
                    sb.Append(priceWithDiscount);
                    sb.Append(separator);

                    //brand
                    var manufacturerName = "";
                    var productManufacturers = await _manufacturerService.GetProductManufacturersByProductIdAsync(currentProduct.Id);
                    var defaultProductManufacturer = productManufacturers.FirstOrDefault();

                    if (defaultProductManufacturer != null)
                        manufacturerName = NormalizeStringValue((await _manufacturerService.GetManufacturerByIdAsync(defaultProductManufacturer.ManufacturerId)).Name);

                    sb.Append(manufacturerName);
                    sb.Append(separator);

                    //category
                    var categoryName = "Default category";
                    var productCategories = await _categoryService.GetProductCategoriesByProductIdAsync(currentProduct.Id);
                    var defaultProductCategory = productCategories.FirstOrDefault();

                    if (defaultProductCategory != null)
                        categoryName = NormalizeStringValue((await _categoryService.GetCategoryByIdAsync(defaultProductCategory.CategoryId)).Name);

                    sb.Append(categoryName);
                    sb.Append(separator);

                    //extra data
                    //categories (breadcrumb)
                    var categoryString = string.Empty;
                    if (defaultProductCategory != null)
                    {
                        var categoryBreadcrumb = await (await _categoryService.GetCategoryBreadCrumbAsync(await _categoryService.GetCategoryByIdAsync(defaultProductCategory.CategoryId))).SelectAwait(async catBr => NormalizeStringValue(await _localizationService.GetLocalizedAsync(catBr, x => x.Name))).ToListAsync();

                        categoryString = string.Join("|", categoryBreadcrumb);
                    } else {
                        categoryString = categoryName;
                    }

                    //attributes
                    var variations = new List<Dictionary<string, object>>();
                    var attributeCodes = new List<string>();

                    var allAttributesXml = await _productAttributeParser.GenerateAllCombinationsAsync(currentProduct, true);
                    foreach (var attributesXml in allAttributesXml)
                    {
                        var warnings = new List<string>();
                        warnings.AddRange(await _shoppingCartService.GetShoppingCartItemAttributeWarningsAsync(await _workContext.GetCurrentCustomerAsync(),
                            ShoppingCartType.ShoppingCart, currentProduct, 1, attributesXml, true));
                        if (warnings.Count != 0)
                            continue;

                        var existingCombination = await _productAttributeParser.FindProductAttributeCombinationAsync(currentProduct, attributesXml);
                        if (existingCombination != null)
                        {
                            var varCode = await GetCombinationCodeAsync(attributesXml);
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
                        imageUrls.Add($"\"{await _pictureService.GetPictureUrlAsync(picture)}\"".Replace("/", "\\/"));

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

        /// <summary>
        /// Gets product price
        /// </summary>
        /// <param name="product"></param>
        /// <returns>Product price and price with discount</returns>
        public async Task<(string price, string priceWithDiscount)> GetProductPriceAsync(Product product)
        {
            var priceBase = decimal.Zero;
            var priceWithDiscountBase = decimal.Zero;

            if (await _permissionService.AuthorizeAsync(StandardPermissionProvider.DisplayPrices))
            {
                if (!product.CustomerEntersPrice)
                {
                    if (!product.CallForPrice)
                    {
                        var (minPossiblePriceWithoutDiscount, minPossiblePriceWithDiscount, _, _) = await _priceCalculationService.GetFinalPriceAsync(product, await _workContext.GetCurrentCustomerAsync());

                        if (product.HasTierPrices)
                        {
                            var (tierPriceMinPossiblePriceWithoutDiscount, tierPriceMinPossiblePriceWithDiscount, _, _) = await _priceCalculationService.GetFinalPriceAsync(product, await _workContext.GetCurrentCustomerAsync(), quantity: int.MaxValue);

                            //calculate price for the maximum quantity if we have tier prices, and choose minimal
                            minPossiblePriceWithoutDiscount = Math.Min(minPossiblePriceWithoutDiscount, tierPriceMinPossiblePriceWithoutDiscount);
                            minPossiblePriceWithDiscount = Math.Min(minPossiblePriceWithDiscount, tierPriceMinPossiblePriceWithDiscount);
                        }

                        var (finalPriceWithoutDiscountBase, _) = await _taxService.GetProductPriceAsync(product, minPossiblePriceWithoutDiscount);
                        var (finalPriceWithDiscountBase, _) = await _taxService.GetProductPriceAsync(product, minPossiblePriceWithDiscount);

                        priceBase = finalPriceWithoutDiscountBase;
                        priceWithDiscountBase = finalPriceWithDiscountBase;
                    }
                }
            }

            //Retargeting doesn't allow price = 0
            if (priceBase == decimal.Zero)
                priceBase = decimal.One;

            //Retargeting doesn't allow price with discount = 0; in this case we should use a price without discount
            if (priceWithDiscountBase == decimal.Zero)
                priceWithDiscountBase = priceBase;

            var price = priceBase.ToString(new CultureInfo("en-US", false).NumberFormat);
            var priceWithDiscount = priceWithDiscountBase.ToString(new CultureInfo("en-US", false).NumberFormat);

            return (price, priceWithDiscount);
        }

        /// <summary>
        /// Sends order
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task SendOrderAsync(int orderId)
        {
            var retargetingSettings = await _settingService.LoadSettingAsync<RetargetingSettings>((await _storeContext.GetCurrentStoreAsync()).Id);

            if (!string.IsNullOrEmpty(retargetingSettings.RestApiKey))
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                var currentCustomerId = (await _workContext.GetCurrentCustomerAsync()).Id;

                if (order != null && !order.Deleted && currentCustomerId == order.CustomerId)
                {
                    var sb = new StringBuilder();

                    var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);

                    sb.AppendFormat("api_key={0}&", retargetingSettings.RestApiKey);
                    sb.AppendFormat("0[order_no]={0}&", order.Id);
                    sb.AppendFormat("0[lastname]={0}&", WebUtility.UrlEncode(billingAddress?.LastName));
                    sb.AppendFormat("0[firstname]={0}&", WebUtility.UrlEncode(billingAddress?.FirstName));
                    sb.AppendFormat("0[email]={0}&", WebUtility.UrlEncode(billingAddress?.Email));
                    sb.AppendFormat("0[phone]={0}&", WebUtility.UrlEncode(billingAddress?.PhoneNumber));
                    sb.AppendFormat("0[state]={0}&", WebUtility.UrlEncode((await _stateProvinceService.GetStateProvinceByAddressAsync(billingAddress))?.Name));
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
                    var discountsWithCouponCodes =await (await _discountService.GetAllDiscountUsageHistoryAsync(orderId: order.Id)).WhereAwait(async x => !string.IsNullOrEmpty((await _discountService.GetDiscountByIdAsync(x.DiscountId))?.CouponCode)).ToListAsync();

                    for (var i = 0; i < discountsWithCouponCodes.Count; i++)
                    {
                        discountCode += (await _discountService.GetDiscountByIdAsync(discountsWithCouponCodes[i].DiscountId)).CouponCode;

                        if (i < discountsWithCouponCodes.Count - 1)
                            discountCode += ",";
                    }
                    sb.AppendFormat("0[discount_code]={0}&", discountCode);

                    //order items
                    var orderItems = await _orderService.GetOrderItemsAsync(order.Id);
                    for (var i = 0; i < orderItems.Count; i++)
                    {
                        sb.AppendFormat("1[{0}][id]={1}&", i, orderItems.ElementAt(i).Id);
                        sb.AppendFormat("1[{0}][quantity]={1}&", i, orderItems.ElementAt(i).Quantity);
                        sb.AppendFormat("1[{0}][price]={1}&", i, order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax
                                                                                    ? orderItems.ElementAt(i).UnitPriceInclTax
                                                                                    : orderItems.ElementAt(i).UnitPriceExclTax);
                        var variationCode = "";
                        var values = await _productAttributeParser.ParseProductAttributeValuesAsync(orderItems.ElementAt(i).AttributesXml);
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
                    var response = await restApiHelper.GetJsonAsync(_logger, "https://retargeting.biz/api/1.0/order/save.json", HttpMethod.Post, sb.ToString());

                    //order note
                    await _orderService.InsertOrderNoteAsync(new OrderNote
                    {
                        OrderId = order.Id,
                        Note = string.Format("Retargeting REST API. Saving the order data result: {0}", response),
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    await _orderService.UpdateOrderAsync(order);
                }
            }
        }

        /// <summary>
        /// Indicates whether the product combination is in stock
        /// </summary>
        /// <param name="product"></param>
        /// <param name="attributeXml"></param>
        /// <returns>The value indicating whether the product combination is in stock, variation code, variation details</returns>
        public async Task<(bool productIsInStock, string variationCode, Dictionary<string, object> variationDetails)> IsProductCombinationInStockAsync(Product product, string attributeXml)
        {
            var productIsInStock = false;
            var variationCode = "";
            var variationDetails = new Dictionary<string, object>();

            if (product == null)
                return (productIsInStock, variationCode, variationDetails);

            switch (product.ManageInventoryMethod)
            {
                case ManageInventoryMethod.ManageStock:
                    {
                        #region Manage stock

                        if (!product.DisplayStockAvailability)
                            productIsInStock = true;

                        var stockQuantity = await _productService.GetTotalStockQuantityAsync(product);
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

                        var combination = await _productAttributeParser.FindProductAttributeCombinationAsync(product, attributeXml);
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

            var values = await _productAttributeParser.ParseProductAttributeValuesAsync(attributeXml);
            if (values.Count > 0)
            {
                for (var i = 0; i < values.Count; i++)
                {
                    variationCode += values[i].Id;
                    if (i < values.Count - 1)
                        variationCode += "-";

                    var attributeMapping = await _productAttributeService.GetProductAttributeMappingByIdAsync(values[i].ProductAttributeMappingId);
                    var productAttribute = await _productAttributeService.GetProductAttributeByIdAsync(attributeMapping.ProductAttributeId);
                    if (attributeMapping != null && productAttribute != null)
                    {
                        variationDetails.Add(
                            values[i].Id.ToString(),
                            new
                            {
                                category_name = await _localizationService.GetLocalizedAsync(productAttribute, x => x.Name),
                                category = productAttribute.Id,
                                value = values[i].Name
                            });
                    }
                }
            }

            return (productIsInStock, variationCode, variationDetails);
        }

        #endregion

        /// <summary>
        /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
        /// </summary>
        public bool HideInWidgetList => false;
    }
}