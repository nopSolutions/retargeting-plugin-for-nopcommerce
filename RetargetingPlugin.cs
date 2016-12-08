using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Tax;
using Nop.Core.Plugins;
using Nop.Services.Catalog;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Services.Topics;
using OrderItem = Nop.Plugin.Widgets.Retargeting.Models.OrderItem;

namespace Nop.Plugin.Widgets.Retargeting
{
    public class RetargetingPlugin : BasePlugin, IWidgetPlugin
    {
        private readonly ITaxService _taxService;
        private readonly ITopicService _topicService;
        private readonly IStoreService _storeService;
        private readonly IOrderService _orderService;
        private readonly IPictureService _pictureService;
        private readonly ISettingService _settingService;
        private readonly IProductService _productService;
        private readonly IDiscountService _discountService;
        private readonly ICurrencyService _currencyService;
        private readonly ICategoryService _categoryService;
        private readonly IPermissionService _permissionService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IManufacturerService _manufacturerService;
        private readonly ILocalizationService _localizationService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductAttributeService _productAttributeService;

        private readonly ILogger _logger;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IProductAttributeParser _productAttributeParser;

        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly OrderSettings _orderSettings;

        public RetargetingPlugin(
            ITaxService taxService,
            ITopicService topicService,
            IStoreService storeService,
            IOrderService orderService,
            IPictureService pictureService,
            ISettingService settingService,
            IProductService productService,
            ICurrencyService currencyService,
            ICategoryService categoryService,
            IDiscountService discountService,
            IPermissionService permissionService,
            IShoppingCartService shoppingCartService,
            IManufacturerService manufacturerService,
            ILocalizationService localizationService,
            IPriceCalculationService priceCalculationService,
            IProductAttributeService productAttributeService,

            ILogger logger,
            IWorkContext workContext,
            IStoreContext storeContext,
            IProductAttributeParser productAttributeParser,

            ShoppingCartSettings shoppingCartSettings,
            OrderSettings orderSettings)
        {
            _taxService = taxService;
            _topicService = topicService;
            _storeService = storeService;
            _orderService = orderService;
            _pictureService = pictureService;
            _settingService = settingService;
            _productService = productService;
            _currencyService = currencyService;
            _categoryService = categoryService;
            _discountService = discountService;
            _permissionService = permissionService;
            _shoppingCartService = shoppingCartService;
            _manufacturerService = manufacturerService;
            _localizationService = localizationService;
            _priceCalculationService = priceCalculationService;
            _productAttributeService = productAttributeService;

            _logger = logger;
            _workContext = workContext;
            _storeContext = storeContext;
            _productAttributeParser = productAttributeParser;

            _shoppingCartSettings = shoppingCartSettings;
            _orderSettings = orderSettings;
        }

        /// <summary>
        /// Gets widget zones where this widget should be rendered
        /// </summary>
        /// <returns>Widget zones</returns>
        public IList<string> GetWidgetZones()
        {
            return new List<string>() { "content_before" };
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "WidgetsRetargeting";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Widgets.Retargeting.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for displaying widget
        /// </summary>
        /// <param name="widgetZone">Widget zone where it's displayed</param>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetDisplayWidgetRoute(string widgetZone, out string actionName, out string controllerName,
            out RouteValueDictionary routeValues)
        {
            actionName = "PublicInfo";
            controllerName = "WidgetsRetargeting";
            routeValues = new RouteValueDictionary
            {
                {"Namespaces", "Nop.Plugin.Widgets.Retargeting.Controllers"},
                {"area", null},
                {"widgetZone", widgetZone}
            };
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new RetargetingSettings()
            {
                ProductBoxSelector = ".product-item",
                AddToCartCatalogButtonSelector = ".product-box-add-to-cart-button",
                AddToCartButtonIdDetailsPrefix = "add-to-cart-button-",
                AddToWishlistCatalogButtonSelector = ".add-to-wishlist-button",
                AddToWishlistButtonIdDetailsPrefix = "add-to-wishlist-button-",
                ProductPriceLabelDetailsSelector = ".prices",
                ProductMainPictureIdDetailsPrefix = "main-product-img-lightbox-anchor-",
                HelpTopicSystemNames = "ShippingInfo,PrivacyInfo,ConditionsOfUse,AboutUs",
                ProductReviewAddedResultSelector = "div.result",
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.Configuration", "Configuration");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.PreconfigureSystem", "Preconfigure system");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.PreconfigureButton", "Preconfigure");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.PreconfigureCompleted", "Preconfigure completed");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.PreconfigureError", "Preconfigure error");

            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.TrackingApiKey", "Tracking API KEY");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.TrackingApiKey.Hint", "To use Retargeting you need the Tracking API KEY from your Retargeting account.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RestApiKey", "REST API KEY");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RestApiKey.Hint", "To use the REST API you need the REST API KEY from your Retargeting account.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ProductBoxSelector", "Product box selector");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ProductBoxSelector.Hint", "Product box selector.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.AddToCartCatalogButtonSelector", "Add to cart button selector (catalog)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.AddToCartCatalogButtonSelector.Hint", "Add to cart button selector (catalog).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.AddToCartButtonIdDetailsPrefix", "Add to cart button id prefix (product details)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.AddToCartButtonIdDetailsPrefix.Hint", "Add to cart button id prefix (product details).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.AddToWishlistCatalogButtonSelector", "Add to wishlist button selector (catalog)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.AddToWishlistCatalogButtonSelector.Hint", "Add to wishlist button selector (catalog).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.AddToWishlistButtonIdDetailsPrefix", "Add to wishlist button id prefix (product details)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.AddToWishlistButtonIdDetailsPrefix.Hint", "Add to wishlist button id  (product details).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ProductPriceLabelDetailsSelector", "Price label selector (product details)");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ProductPriceLabelDetailsSelector.Hint", "Price label selector (product details).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ProductMainPictureIdDetailsPrefix", "Product main picture id prefix");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ProductMainPictureIdDetailsPrefix.Hint", "Product main picture id prefix (required only if main picture zoom is enabled).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.HelpTopicSystemNames", "Help topic system names");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.HelpTopicSystemNames.Hint", "Comma separated help topic system names.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ProductReviewAddedResultSelector", "Product review added result selector");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ProductReviewAddedResultSelector.Hint", "Product review added result selector.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.DiscountTypeNote", "Note: Retargeting can generate discounts through it's API. One of the generated discount types is Custom. We allow Retargeting to generate only Free Shipping discount as a Custom discount type.");

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
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.Configuration");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.PreconfigureSystem");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.PreconfigureButton");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.PreconfigureCompleted");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.PreconfigureError");

            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.TrackingApiKey");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.TrackingApiKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RestApiKey");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RestApiKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ProductBoxSelector");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ProductBoxSelector.Hint");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.AddToCartCatalogButtonSelector");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.AddToCartCatalogButtonSelector.Hint");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.AddToCartButtonIdDetailsPrefix");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.AddToCartButtonIdDetailsPrefix.Hint");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.AddToWishlistCatalogButtonSelector");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.AddToWishlistCatalogButtonSelector.Hint");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.AddToWishlistButtonIdDetailsPrefix");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.AddToWishlistButtonIdDetailsPrefix.Hint");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ProductPriceLabelDetailsSelector");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ProductPriceLabelDetailsSelector.Hint");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ProductMainPictureIdDetailsPrefix");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ProductMainPictureIdDetailsPrefix.Hint");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.HelpTopicSystemNames");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.HelpTopicSystemNames.Hint");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ProductReviewAddedResultSelector");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ProductReviewAddedResultSelector.Hint");
            this.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.DiscountTypeNote");

            base.Uninstall();
        }

        public void Preconfigure()
        {
            _shoppingCartSettings.DisplayCartAfterAddingProduct = false;
            _shoppingCartSettings.DisplayWishlistAfterAddingProduct = false;
            _settingService.SaveSetting(_shoppingCartSettings);

            _orderSettings.DisableOrderCompletedPage = false;
            _settingService.SaveSetting(_orderSettings);
        }

        public List<Dictionary<string, object>> GenerateProductStockFeed()
        {
            var productFeed = new List<Dictionary<string, object>>();

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
                            productsToProcess.AddRange(associatedProducts);
                        }
                        break;
                    default:
                        continue;
                }

                foreach (var productToProcess in productsToProcess)
                {
                    var productInfo = new Dictionary<string, object>
                    {
                        {"id", productToProcess.Id.ToString()},
                        {
                            "product_availability",
                            product.AvailableEndDateTimeUtc != null
                                ? product.AvailableEndDateTimeUtc.Value.ToString("yy-MM-dd hh:mm:ss")
                                : null
                        }
                    };

                    decimal price;
                    decimal priceWithDiscount;
                    GetProductPrice(product, out price, out priceWithDiscount);

                    productInfo.Add("price", price.ToString(new CultureInfo("en-US", false).NumberFormat));
                    productInfo.Add("promo", priceWithDiscount.ToString(new CultureInfo("en-US", false).NumberFormat));
                    productInfo.Add("promo_price_end_date", null);

                    var inventory = new Dictionary<string, object>();
                    var attributes = new Dictionary<string, object>();

                    var allAttributesXml = _productAttributeParser.GenerateAllCombinations(product, true);
                    foreach (var attributesXml in allAttributesXml)
                    {
                        var warnings = new List<string>();
                        warnings.AddRange(_shoppingCartService.GetShoppingCartItemAttributeWarnings(_workContext.CurrentCustomer,
                            ShoppingCartType.ShoppingCart, product, 1, attributesXml, true));
                        if (warnings.Count != 0)
                            continue;

                        var inStock = true;
                        var existingCombination = _productAttributeParser.FindProductAttributeCombination(product, attributesXml);
                        if (existingCombination != null)
                            inStock = existingCombination.StockQuantity > 0;

                        var varCode = GetCombinationCode(attributesXml);
                        if (!attributes.ContainsKey(varCode))
                            attributes.Add(varCode, inStock);
                    }

                    var variation = new Dictionary<string, object>
                    {
                        {"variation", attributes}
                    };

                    if (attributes.Count > 0)
                    {
                        inventory.Add("variations", true);
                        inventory.Add("stock", variation);
                    }
                    else
                    {
                        inventory.Add("variations", false);
                        inventory.Add("stock", product.StockQuantity > 0);
                    }

                    productInfo.Add("inventory", inventory);
                    productInfo.Add("user_groups", false);

                    productFeed.Add(productInfo);
                }
            }

            return productFeed;
        }

        public void GetProductPrice(Product product, out decimal price, out decimal priceWithDiscount)
        {
            price = decimal.Zero;
            priceWithDiscount = decimal.Zero;

            if (_permissionService.Authorize(StandardPermissionProvider.DisplayPrices))
            {
                if (!product.CustomerEntersPrice)
                {
                    if (!product.CallForPrice)
                    {
                        decimal taxRate;

                        var oldPriceBase = _taxService.GetProductPrice(product,
                            _priceCalculationService.GetFinalPrice(product, _workContext.CurrentCustomer,
                                includeDiscounts: false), out taxRate);
                        var oldPrice = _currencyService.ConvertFromPrimaryStoreCurrency(oldPriceBase,
                            _workContext.WorkingCurrency);

                        var priceWithDiscountBase = _taxService.GetProductPrice(product,
                            _priceCalculationService.GetFinalPrice(product, _workContext.CurrentCustomer), out taxRate);
                        var finalPriceWithDiscount =
                            _currencyService.ConvertFromPrimaryStoreCurrency(priceWithDiscountBase,
                                _workContext.WorkingCurrency);

                        price = oldPrice;
                        if (oldPrice == 0)
                            price = finalPriceWithDiscount;

                        if (price != finalPriceWithDiscount)
                            priceWithDiscount = finalPriceWithDiscount;
                    }
                }
            }
        }

        public string GetCombinationCode(string attributesXml)
        {
            var result = "";

            var attributes = _productAttributeParser.ParseProductAttributeMappings(attributesXml);
            for (var i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];
                var valuesStr = _productAttributeParser.ParseValues(attributesXml, attribute.Id);

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

        public void SendOrder(int orderId)
        {
            var retargetingSettings = _settingService.LoadSetting<RetargetingSettings>(_storeContext.CurrentStore.Id);

            if (!string.IsNullOrEmpty(retargetingSettings.RestApiKey))
            {
                var order = _orderService.GetOrderById(orderId);
                if (order != null && !order.Deleted && _workContext.CurrentCustomer.Id == order.CustomerId)
                {
                    var sb = new StringBuilder();

                    sb.AppendFormat("api_key={0}&", retargetingSettings.RestApiKey);
                    sb.AppendFormat("0[order_no]={0}&", order.Id);
                    sb.AppendFormat("0[lastname]={0}&", HttpUtility.UrlEncode(order.BillingAddress.LastName));
                    sb.AppendFormat("0[firstname]={0}&", HttpUtility.UrlEncode(order.BillingAddress.FirstName));
                    sb.AppendFormat("0[email]={0}&", HttpUtility.UrlEncode(order.BillingAddress.Email));
                    sb.AppendFormat("0[phone]={0}&", HttpUtility.UrlEncode(order.BillingAddress.PhoneNumber));
                    sb.AppendFormat("0[state]={0}&", order.BillingAddress.StateProvince != null
                                                    ? HttpUtility.UrlEncode(order.BillingAddress.StateProvince.Name)
                                                    : "");
                    sb.AppendFormat("0[city]={0}&", HttpUtility.UrlEncode(order.BillingAddress.City));
                    sb.AppendFormat("0[adress]={0}&", HttpUtility.UrlEncode(order.BillingAddress.Address1 + " " +
                                                               order.BillingAddress.Address2));
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
                                .Where(x => !string.IsNullOrEmpty(x.Discount.CouponCode))
                                .ToList();
                    for (var i = 0; i < discountsWithCouponCodes.Count; i++)
                    {
                        discountCode += discountsWithCouponCodes[i].Discount.CouponCode;

                        if (i < discountsWithCouponCodes.Count - 1)
                            discountCode += ",";
                    }
                    sb.AppendFormat("0[discount_code]={0}&", discountCode);

                    //order items
                    for (var i = 0; i < order.OrderItems.Count; i++)
                    {
                        sb.AppendFormat("1[{0}][id]={1}&", i, order.OrderItems.ElementAt(i).Id);
                        sb.AppendFormat("1[{0}][quantity]={1}&", i, order.OrderItems.ElementAt(i).Quantity);
                        sb.AppendFormat("1[{0}][price]={1}&", i, order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax
                                                                                    ? order.OrderItems.ElementAt(i).UnitPriceInclTax
                                                                                    : order.OrderItems.ElementAt(i).UnitPriceExclTax);
                        var variationCode = "";
                        var values = _productAttributeParser.ParseProductAttributeValues(order.OrderItems.ElementAt(i).AttributesXml);
                        for (var j = 0; j < values.Count; i++)
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
                    order.OrderNotes.Add(new OrderNote
                    {
                        Note = string.Format("Retargeting REST API. Saving the order data result: {0}", response),
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    _orderService.UpdateOrder(order);
                }
            }
        }
    }
}
