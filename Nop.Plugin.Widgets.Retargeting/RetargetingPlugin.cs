﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Net;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Tax;
using Nop.Services.Catalog;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Tax;
using Nop.Services.Logging;
using Nop.Services.Plugins;
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
        private readonly ICurrencyService _currencyService;
        private readonly IPermissionService _permissionService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IPriceCalculationService _priceCalculationService;

        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IProductAttributeParser _productAttributeParser;

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
            ICurrencyService currencyService,
            IDiscountService discountService,
            IPermissionService permissionService,
            IShoppingCartService shoppingCartService,
            IProductAttributeService productAttributeService,
            IPriceCalculationService priceCalculationService,

            ILocalizationService localizationService,
            ILogger logger,
            IWebHelper webHelper,
            IWorkContext workContext,
            IStoreContext storeContext,
            IProductAttributeParser productAttributeParser,

            ShoppingCartSettings shoppingCartSettings,
            OrderSettings orderSettings,
            MediaSettings mediaSettings)
        {
            _taxService = taxService;
            _orderService = orderService;
            _settingService = settingService;
            _productService = productService;
            _currencyService = currencyService;
            _discountService = discountService;
            _permissionService = permissionService;
            _shoppingCartService = shoppingCartService;
            _productAttributeService = productAttributeService;
            _priceCalculationService = priceCalculationService;

            _localizationService = localizationService;
            _logger = logger;
            _webHelper = webHelper;
            _workContext = workContext;
            _storeContext = storeContext;
            _productAttributeParser = productAttributeParser;

            _shoppingCartSettings = shoppingCartSettings;
            _orderSettings = orderSettings;
            _mediaSettings = mediaSettings;
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
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.Configuration", "Configuration");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.PreconfigureSystem", "Preconfigure system");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.PreconfigureButton", "Preconfigure");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.PreconfigureCompleted", "Preconfigure completed");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.PreconfigureError", "Preconfigure error");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ResetSettings", "Reset settings");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.SettingsReset", "Settings have been reset");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ExceptionLoadPlugin", "Cannot load the plugin");

            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.TrackingApiKey", "Tracking API KEY");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.TrackingApiKey.Hint", "To use Retargeting you need the Tracking API KEY from your Retargeting account.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RestApiKey", "REST API KEY");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RestApiKey.Hint", "To use the REST API you need the REST API KEY from your Retargeting account.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.UseHttpPostInsteadOfAjaxInAddToCart", "Use HTTP POST method (add to cart)");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.UseHttpPostInsteadOfAjaxInAddToCart.Hint", "Check to use the HTTP POST method for adding to cart(wishlist) instead of ajax.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ProductBoxSelector", "Product box selector");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ProductBoxSelector.Hint", "Product box selector.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.AddToCartCatalogButtonSelector", "Add to cart button selector (catalog)");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.AddToCartCatalogButtonSelector.Hint", "Add to cart button selector (catalog).");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.AddToCartButtonIdDetailsPrefix", "Add to cart button id prefix (product details)");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.AddToCartButtonIdDetailsPrefix.Hint", "Add to cart button id prefix (product details).");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.AddToWishlistCatalogButtonSelector", "Add to wishlist button selector (catalog)");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.AddToWishlistCatalogButtonSelector.Hint", "Add to wishlist button selector (catalog).");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.AddToWishlistButtonIdDetailsPrefix", "Add to wishlist button id prefix (product details)");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.AddToWishlistButtonIdDetailsPrefix.Hint", "Add to wishlist button id  (product details).");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ProductPriceLabelDetailsSelector", "Price label selector (product details)");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ProductPriceLabelDetailsSelector.Hint", "Price label selector (product details).");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ProductMainPictureIdDetailsPrefix", "Product main picture id prefix");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ProductMainPictureIdDetailsPrefix.Hint", "Product main picture id prefix (required only if main picture zoom is enabled).");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.HelpTopicSystemNames", "Help topic system names");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.HelpTopicSystemNames.Hint", "Comma separated help topic system names.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ProductReviewAddedResultSelector", "Product review added result selector");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.ProductReviewAddedResultSelector.Hint", "Product review added result selector.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.DiscountTypeNote", "Note: Retargeting can generate discounts through it's API. One of the generated discount types is Custom. We allow Retargeting to generate only Free Shipping discount as a Custom discount type.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.CustomizationNote", "Note: this plugin may work incorrectly in case you made some customization of your website.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.SubscribeRetargeting", "Please enter your email to receive an information about special offers from Retargeting.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.Subscribe", "Subscribe");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.MerchantEmail", "Email");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.MerchantEmail.Hint", "Enter your email to subscribe to Retargeting news.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.Subscribe.Error", "An error has occurred, details in the log");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.Subscribe.Success", "You have subscribed to Retargeting news");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.Unsubscribe.Success", "You have unsubscribed from Retargeting news");

            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationHomePage", "Recommendation for Home page");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationHomePage.Hint", "To use Retargeting Recommendation Engine for Home page.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationCategoryPage", "Recommendation for Category page");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationCategoryPage.Hint", "To use Retargeting Recommendation Engine for Category page.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationProductPage", "Recommendation for Product page");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationProductPage.Hint", "To use Retargeting Recommendation Engine for Product page.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationCheckoutPage", "Recommendation for Checkout page");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationCheckoutPage.Hint", "To use Retargeting Recommendation Engine for Checkout page.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationThankYouPage", "Recommendation for Thank You Page");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationThankYouPage.Hint", "To use Retargeting Recommendation Engine for Thank You page.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationOutOfStockPage", "Recommendation for Out Of Stock page");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationOutOfStockPage.Hint", "To use Retargeting Recommendation Engine for Out Of Stock page.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationSearchPage", "Recommendation for Search page");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationSearchPage.Hint", "To use Retargeting Recommendation Engine for Search page.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationPageNotFound", "Recommendation for Page Not Found");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationPageNotFound.Hint", "To use Retargeting Recommendation Engine for Page Not Found.");


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
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.Configuration");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.PreconfigureSystem");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.PreconfigureButton");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.PreconfigureCompleted");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.PreconfigureError");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ResetSettings");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.SettingsReset");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ExceptionLoadPlugin");

            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.TrackingApiKey");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.TrackingApiKey.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.UseHttpPostInsteadOfAjaxInAddToCart");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.UseHttpPostInsteadOfAjaxInAddToCart.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RestApiKey");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RestApiKey.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ProductBoxSelector");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ProductBoxSelector.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.AddToCartCatalogButtonSelector");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.AddToCartCatalogButtonSelector.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.AddToCartButtonIdDetailsPrefix");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.AddToCartButtonIdDetailsPrefix.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.AddToWishlistCatalogButtonSelector");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.AddToWishlistCatalogButtonSelector.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.AddToWishlistButtonIdDetailsPrefix");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.AddToWishlistButtonIdDetailsPrefix.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ProductPriceLabelDetailsSelector");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ProductPriceLabelDetailsSelector.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ProductMainPictureIdDetailsPrefix");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ProductMainPictureIdDetailsPrefix.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.HelpTopicSystemNames");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.HelpTopicSystemNames.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ProductReviewAddedResultSelector");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.ProductReviewAddedResultSelector.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.DiscountTypeNote");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.CustomizationNote");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.SubscribeRetargeting");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.Subscribe");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.MerchantEmail");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.MerchantEmail.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.Subscribe.Error");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.Subscribe.Success");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.Unsubscribe.Success");

            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationHomePage");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationHomePage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationCategoryPage");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationCategoryPage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationProductPage");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationProductPage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationCheckoutPage");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationCheckoutPage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationThankYouPage");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationThankYouPage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationOutOfStockPage");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationOutOfStockPage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationSearchPage");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationSearchPage.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationPageNotFound");
            _localizationService.DeletePluginLocaleResource("Plugins.Widgets.Retargeting.RecommendationPageNotFound.Hint");

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
                            product.AvailableEndDateTimeUtc?.ToString("yy-MM-dd hh:mm:ss")
                        }
                    };

                    GetProductPrice(product, out var price, out var priceWithDiscount);

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
                        var oldPriceBase = _taxService.GetProductPrice(product, product.OldPrice, out _);
                        var finalPriceWithDiscountBase = _taxService.GetProductPrice(product, _priceCalculationService.GetFinalPrice(product, _workContext.CurrentCustomer, includeDiscounts: true), out _);

                        price = _currencyService.ConvertFromPrimaryStoreCurrency(oldPriceBase, _workContext.WorkingCurrency);
                        priceWithDiscount = _currencyService.ConvertFromPrimaryStoreCurrency(finalPriceWithDiscountBase, _workContext.WorkingCurrency);

                        if (price == 0)
                        {
                            price = priceWithDiscount;
                            priceWithDiscount = 0;
                        }
                    }
                }
            }
        }

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
                    sb.AppendFormat("0[lastname]={0}&", WebUtility.UrlEncode(order.BillingAddress.LastName));
                    sb.AppendFormat("0[firstname]={0}&", WebUtility.UrlEncode(order.BillingAddress.FirstName));
                    sb.AppendFormat("0[email]={0}&", WebUtility.UrlEncode(order.BillingAddress.Email));
                    sb.AppendFormat("0[phone]={0}&", WebUtility.UrlEncode(order.BillingAddress.PhoneNumber));
                    sb.AppendFormat("0[state]={0}&", order.BillingAddress.StateProvince != null
                                                    ? WebUtility.UrlEncode(order.BillingAddress.StateProvince.Name)
                                                    : "");
                    sb.AppendFormat("0[city]={0}&", WebUtility.UrlEncode(order.BillingAddress.City));
                    sb.AppendFormat("0[adress]={0}&", WebUtility.UrlEncode(order.BillingAddress.Address1 + " " +
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

                    var attributeMapping =
                            _productAttributeService.GetProductAttributeMappingById(values[i].ProductAttributeMappingId);
                    if (attributeMapping != null && attributeMapping.ProductAttribute != null)
                    {
                        variationDetails.Add(
                            values[i].Id.ToString(),
                            new
                            {
                                category_name = _localizationService.GetLocalized(attributeMapping.ProductAttribute, x => x.Name),
                                category = attributeMapping.ProductAttribute.Id,
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