using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Widgets.Retargeting.Models;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Plugins;
using Nop.Services.Seo;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Widgets.Retargeting.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class WidgetsRetargetingController : BasePluginController
    {
        #region Fields

        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly ICategoryService _categoryService;
        private readonly IDiscountService _discountService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IEmailSender _emailSender;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IManufacturerService _manufacturerService;
        private readonly INotificationService _notificationService;
        private readonly IPictureService _pictureService;
        private readonly IPluginService _pluginService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductService _productService;
        private readonly ISettingService _settingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor    

        public WidgetsRetargetingController(
                EmailAccountSettings emailAccountSettings,
                ICategoryService categoryService,
                IDiscountService discountService,
                IEmailAccountService emailAccountService,
                IEmailSender emailSender,
                IGenericAttributeService genericAttributeService,
                ILocalizationService localizationService,
                ILogger logger,
                IManufacturerService manufacturerService,
                INotificationService notificationService,
                IPictureService pictureService,
                IPluginService pluginService,
                IProductAttributeParser productAttributeParser,
                IProductAttributeService productAttributeService,
                IProductService productService,
                ISettingService settingService,
                IShoppingCartService shoppingCartService,
                IStoreContext storeContext,
                IUrlRecordService urlRecordService,
                IWorkContext workContext
                )
        {
            _categoryService = categoryService;
            _discountService = discountService;
            _emailAccountService = emailAccountService;
            _emailAccountSettings = emailAccountSettings;
            _emailSender = emailSender;
            _genericAttributeService = genericAttributeService;
            _localizationService = localizationService;
            _logger = logger;
            _manufacturerService = manufacturerService;
            _notificationService = notificationService;
            _pictureService = pictureService;
            _pluginService = pluginService;
            _productAttributeParser = productAttributeParser;
            _productAttributeService = productAttributeService;
            _productService = productService;
            _settingService = settingService;
            _shoppingCartService = shoppingCartService;
            _storeContext = storeContext;
            _urlRecordService = urlRecordService;
            _workContext = workContext;
        }

        #endregion

        #region Utilities

        [NonAction]
        private string GetProductImageUrl(Product product)
        {
            var pictures = _pictureService.GetPicturesByProductId(product.Id);
            var defaultPicture = pictures.FirstOrDefault();

            return _pictureService.GetPictureUrl(defaultPicture.Id);
        }

        private string ParseProductAttributes(Product product, IFormCollection form)
        {
            var attributesXml = "";

            #region Product attributes

            var productAttributes = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id);
            foreach (var attribute in productAttributes)
            {
                var controlId = string.Format("product_attribute_{0}", attribute.Id);
                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                        {
                            var ctrlAttributes = form[controlId];
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                var selectedAttributeId = int.Parse(ctrlAttributes);
                                if (selectedAttributeId > 0)
                                    attributesXml = _productAttributeParser.AddProductAttribute(attributesXml,
                                        attribute, selectedAttributeId.ToString());
                            }
                        }
                        break;
                    case AttributeControlType.Checkboxes:
                        {
                            var ctrlAttributes = form[controlId].ToString();
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                foreach (var item in ctrlAttributes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    var selectedAttributeId = int.Parse(item);
                                    if (selectedAttributeId > 0)
                                        attributesXml = _productAttributeParser.AddProductAttribute(attributesXml,
                                            attribute, selectedAttributeId.ToString());
                                }
                            }
                        }
                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                        {
                            //load read-only (already server-side selected) values
                            var attributeValues = _productAttributeService.GetProductAttributeValues(attribute.Id);
                            foreach (var selectedAttributeId in attributeValues
                                .Where(v => v.IsPreSelected)
                                .Select(v => v.Id)
                                .ToList())
                            {
                                attributesXml = _productAttributeParser.AddProductAttribute(attributesXml,
                                    attribute, selectedAttributeId.ToString());
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            //validate conditional attributes (if specified)
            foreach (var attribute in productAttributes)
            {
                var conditionMet = _productAttributeParser.IsConditionMet(attribute, attributesXml);
                if (conditionMet.HasValue && !conditionMet.Value)
                {
                    attributesXml = _productAttributeParser.RemoveProductAttribute(attributesXml, attribute);
                }
            }

            #endregion

            return attributesXml;
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var retargetingSettings = _settingService.LoadSetting<RetargetingSettings>(storeScope);

            var model = new ConfigurationModel
            {
                TrackingApiKey = retargetingSettings.TrackingApiKey,
                RestApiKey = retargetingSettings.RestApiKey,
                UseHttpPostInsteadOfAjaxInAddToCart = retargetingSettings.UseHttpPostInsteadOfAjaxInAddToCart,
                AddToCartButtonIdDetailsPrefix = retargetingSettings.AddToCartButtonIdDetailsPrefix,
                ProductPriceLabelDetailsSelector = retargetingSettings.ProductPriceLabelDetailsSelector,
                AddToWishlistButtonIdDetailsPrefix = retargetingSettings.AddToWishlistButtonIdDetailsPrefix,
                HelpTopicSystemNames = retargetingSettings.HelpTopicSystemNames,
                AddToWishlistCatalogButtonSelector = retargetingSettings.AddToWishlistCatalogButtonSelector,
                ProductReviewAddedResultSelector = retargetingSettings.ProductReviewAddedResultSelector,
                AddToCartCatalogButtonSelector = retargetingSettings.AddToCartCatalogButtonSelector,
                ProductBoxSelector = retargetingSettings.ProductBoxSelector,
                ProductMainPictureIdDetailsPrefix = retargetingSettings.ProductMainPictureIdDetailsPrefix,

                RecommendationHomePage = retargetingSettings.RecommendationHomePage,
                RecommendationCategoryPage = retargetingSettings.RecommendationCategoryPage,
                RecommendationProductPage = retargetingSettings.RecommendationProductPage,
                RecommendationCheckoutPage = retargetingSettings.RecommendationCheckoutPage,
                RecommendationThankYouPage = retargetingSettings.RecommendationThankYouPage,
                RecommendationOutOfStockPage = retargetingSettings.RecommendationOutOfStockPage,
                RecommendationSearchPage = retargetingSettings.RecommendationSearchPage,
                RecommendationPageNotFound = retargetingSettings.RecommendationPageNotFound,

                HideConfigurationBlock = _genericAttributeService.GetAttribute<bool>(_workContext.CurrentCustomer, RetargetingDefaults.HideConfigurationBlock),
                HidePreconfigureBlock = _genericAttributeService.GetAttribute<bool>(_workContext.CurrentCustomer, RetargetingDefaults.HidePreconfigureBlock),

                MerchantEmail = retargetingSettings.MerchantEmail,

                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope > 0)
            {
                model.TrackingApiKey_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.TrackingApiKey, storeScope);
                model.RestApiKey_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.RestApiKey, storeScope);
                model.UseHttpPostInsteadOfAjaxInAddToCart_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.UseHttpPostInsteadOfAjaxInAddToCart, storeScope);
                model.AddToCartButtonDetailsPrefix_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.AddToCartButtonIdDetailsPrefix, storeScope);
                model.PriceLabelSelector_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.ProductPriceLabelDetailsSelector, storeScope);
                model.AddToWishlistButtonIdDetailsPrefix_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.AddToWishlistButtonIdDetailsPrefix, storeScope);
                model.HelpTopicSystemNames_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.HelpTopicSystemNames, storeScope);
                model.AddToWishlistCatalogButtonSelector_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.AddToWishlistCatalogButtonSelector, storeScope);
                model.ProductReviewAddedResultSelector_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.ProductReviewAddedResultSelector, storeScope);
                model.AddToCartCatalogButtonSelector_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.AddToCartCatalogButtonSelector, storeScope);
                model.ProductBoxSelector_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.ProductBoxSelector, storeScope);
                model.ProductMainPictureIdDetailsPrefix_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.ProductMainPictureIdDetailsPrefix, storeScope);

                model.RecommendationHomePage_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.RecommendationHomePage, storeScope);
                model.RecommendationCategoryPage_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.RecommendationCategoryPage, storeScope);
                model.RecommendationProductPage_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.RecommendationProductPage, storeScope);
                model.RecommendationCheckoutPage_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.RecommendationCheckoutPage, storeScope);
                model.RecommendationThankYouPage_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.RecommendationThankYouPage, storeScope);
                model.RecommendationOutOfStockPage_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.RecommendationOutOfStockPage, storeScope);
                model.RecommendationSearchPage_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.RecommendationSearchPage, storeScope);
                model.RecommendationPageNotFound_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.RecommendationPageNotFound, storeScope);

                model.MerchantEmail_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.MerchantEmail, storeScope);
            }

            return View("~/Plugins/Widgets.Retargeting/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [FormValueRequired("save")]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var retargetingSettings = _settingService.LoadSetting<RetargetingSettings>(storeScope);

            //save settings
            retargetingSettings.TrackingApiKey = model.TrackingApiKey;
            retargetingSettings.RestApiKey = model.RestApiKey;
            retargetingSettings.UseHttpPostInsteadOfAjaxInAddToCart = model.UseHttpPostInsteadOfAjaxInAddToCart;
            retargetingSettings.AddToCartButtonIdDetailsPrefix = model.AddToCartButtonIdDetailsPrefix;
            retargetingSettings.ProductPriceLabelDetailsSelector = model.ProductPriceLabelDetailsSelector;
            retargetingSettings.AddToWishlistButtonIdDetailsPrefix = model.AddToWishlistButtonIdDetailsPrefix;
            retargetingSettings.HelpTopicSystemNames = model.HelpTopicSystemNames;
            retargetingSettings.AddToWishlistCatalogButtonSelector = model.AddToWishlistCatalogButtonSelector;
            retargetingSettings.ProductReviewAddedResultSelector = model.ProductReviewAddedResultSelector;
            retargetingSettings.AddToCartCatalogButtonSelector = model.AddToCartCatalogButtonSelector;
            retargetingSettings.ProductBoxSelector = model.ProductBoxSelector;
            retargetingSettings.ProductMainPictureIdDetailsPrefix = model.ProductMainPictureIdDetailsPrefix;

            retargetingSettings.RecommendationHomePage = model.RecommendationHomePage;
            retargetingSettings.RecommendationCategoryPage = model.RecommendationCategoryPage;
            retargetingSettings.RecommendationProductPage = model.RecommendationProductPage;
            retargetingSettings.RecommendationCheckoutPage = model.RecommendationCheckoutPage;
            retargetingSettings.RecommendationThankYouPage = model.RecommendationThankYouPage;
            retargetingSettings.RecommendationOutOfStockPage = model.RecommendationOutOfStockPage;
            retargetingSettings.RecommendationSearchPage = model.RecommendationSearchPage;
            retargetingSettings.RecommendationPageNotFound = model.RecommendationPageNotFound;

            retargetingSettings.MerchantEmail = model.MerchantEmail;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.TrackingApiKey, model.TrackingApiKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.RestApiKey, model.RestApiKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.UseHttpPostInsteadOfAjaxInAddToCart, model.UseHttpPostInsteadOfAjaxInAddToCart_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.AddToCartButtonIdDetailsPrefix, model.AddToCartButtonDetailsPrefix_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.ProductPriceLabelDetailsSelector, model.PriceLabelSelector_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.AddToWishlistButtonIdDetailsPrefix, model.AddToWishlistButtonIdDetailsPrefix_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.HelpTopicSystemNames, model.HelpTopicSystemNames_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.AddToWishlistCatalogButtonSelector, model.AddToWishlistCatalogButtonSelector_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.ProductReviewAddedResultSelector, model.ProductReviewAddedResultSelector_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.AddToCartCatalogButtonSelector, model.AddToCartCatalogButtonSelector_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.ProductBoxSelector, model.ProductBoxSelector_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.ProductMainPictureIdDetailsPrefix, model.ProductMainPictureIdDetailsPrefix_OverrideForStore, storeScope, false);

            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.RecommendationHomePage, model.RecommendationHomePage_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.RecommendationCategoryPage, model.RecommendationCategoryPage_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.RecommendationProductPage, model.RecommendationProductPage_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.RecommendationCheckoutPage, model.RecommendationCheckoutPage_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.RecommendationThankYouPage, model.RecommendationThankYouPage_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.RecommendationOutOfStockPage, model.RecommendationOutOfStockPage_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.RecommendationSearchPage, model.RecommendationSearchPage_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.RecommendationPageNotFound, model.RecommendationPageNotFound_OverrideForStore, storeScope, false);

            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.MerchantEmail, model.MerchantEmail_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [Area(AreaNames.Admin)]
        [HttpPost, ActionName("Configure")]
        [FormValueRequired("preconfigure")]
        public IActionResult Preconfigure()
        {
            var pluginDescriptor = _pluginService.GetPluginDescriptorBySystemName<IPlugin>(RetargetingDefaults.SystemName);
            if (pluginDescriptor != null)
            {
                if (pluginDescriptor.Instance<IPlugin>() is RetargetingPlugin plugin)
                {
                    try
                    {
                        plugin.Preconfigure();
                        var message = _localizationService.GetResource("Plugins.Widgets.Retargeting.PreconfigureCompleted");
                        _notificationService.SuccessNotification(message);
                        _logger.Information(message);
                    }
                    catch (Exception exception)
                    {
                        var message = _localizationService.GetResource("Plugins.Widgets.Retargeting.PreconfigureError") + exception;
                        _notificationService.ErrorNotification(message);
                        _logger.Error(message);
                    }

                    return Configure();
                }
            }

            throw new Exception(_localizationService.GetResource("Plugins.Widgets.Retargeting.ExceptionLoadPlugin"));
        }

        [Area(AreaNames.Admin)]
        [HttpPost, ActionName("Configure")]
        [FormValueRequired("reset-settings")]
        public IActionResult ResetSettings()
        {
            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var retargetingSettings = _settingService.LoadSetting<RetargetingSettings>(storeScope);

            //save settings
            retargetingSettings.UseHttpPostInsteadOfAjaxInAddToCart = false;
            retargetingSettings.AddToCartButtonIdDetailsPrefix = RetargetingDefaults.AddToCartButtonIdDetailsPrefix;
            retargetingSettings.ProductPriceLabelDetailsSelector = RetargetingDefaults.ProductPriceLabelDetailsSelector;
            retargetingSettings.AddToWishlistButtonIdDetailsPrefix = RetargetingDefaults.AddToWishlistButtonIdDetailsPrefix;
            retargetingSettings.HelpTopicSystemNames = RetargetingDefaults.HelpTopicSystemNames;
            retargetingSettings.AddToWishlistCatalogButtonSelector = RetargetingDefaults.AddToWishlistCatalogButtonSelector;
            retargetingSettings.ProductReviewAddedResultSelector = RetargetingDefaults.ProductReviewAddedResultSelector;
            retargetingSettings.AddToCartCatalogButtonSelector = RetargetingDefaults.AddToCartCatalogButtonSelector;
            retargetingSettings.ProductBoxSelector = RetargetingDefaults.ProductBoxSelector;
            retargetingSettings.ProductMainPictureIdDetailsPrefix = RetargetingDefaults.ProductMainPictureIdDetailsPrefix;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.UseHttpPostInsteadOfAjaxInAddToCart, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.AddToCartButtonIdDetailsPrefix, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.ProductPriceLabelDetailsSelector, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.AddToWishlistButtonIdDetailsPrefix, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.HelpTopicSystemNames, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.AddToWishlistCatalogButtonSelector, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.ProductReviewAddedResultSelector, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.AddToCartCatalogButtonSelector, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.ProductBoxSelector, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.ProductMainPictureIdDetailsPrefix, false, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            _notificationService.SuccessNotification(_localizationService.GetResource("Plugins.Widgets.Retargeting.SettingsReset"));

            return Configure();
        }

        public IActionResult ProductStockFeed()
        {
            var fileName = string.Format("feed_{0}_{1}.csv", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"), CommonHelper.GenerateRandomDigitCode(4));
            var result = string.Empty;

            try
            {
                var pluginDescriptor = _pluginService.GetPluginDescriptorBySystemName<IPlugin>(RetargetingDefaults.SystemName);
                if (pluginDescriptor == null)
                    throw new Exception(_localizationService.GetResource("Plugins.Widgets.Retargeting.ExceptionLoadPlugin"));

                if (!(pluginDescriptor.Instance<IPlugin>() is RetargetingPlugin plugin))
                    throw new Exception(_localizationService.GetResource("Plugins.Widgets.Retargeting.ExceptionLoadPlugin"));

                result = plugin.ExportProductsToCsv();
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc.Message);
                _logger.Error(exc.Message, exc);
            }

            return File(Encoding.UTF8.GetBytes(result), MimeTypes.TextCsv, fileName);
        }

        public IActionResult GenerateDiscounts(string key, string value, int type, int count)
        {
            var discountCodes = new List<string>();

            var retargetingSettings = _settingService.LoadSetting<RetargetingSettings>(_storeContext.CurrentStore.Id);
            if (retargetingSettings.RestApiKey.Equals(key))
            {
                decimal.TryParse(value, out var discountValue);

                if (discountValue > 0)
                {
                    for (var i = 0; i < count; i++)
                    {
                        var discount = new Discount
                        {
                            RequiresCouponCode = true,
                            CouponCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper()
                        };

                        switch (type)
                        {
                            case 0:
                                discount.Name = string.Format("Retargeting_{0}_{1}_discount", value, "AMOUNT");
                                discount.DiscountType = DiscountType.AssignedToOrderTotal;
                                discount.DiscountAmount = discountValue;
                                break;

                            case 1:
                                discount.Name = string.Format("Retargeting_{0}_{1}_discount", value, "PERCENTAGE");
                                discount.DiscountType = DiscountType.AssignedToOrderTotal;
                                discount.UsePercentage = true;
                                discount.DiscountPercentage = discountValue;
                                break;

                            case 2:
                            default:
                                discount.Name = string.Format("Retargeting_{0}_discount", "FREE_SHIPPING");
                                discount.DiscountType = DiscountType.AssignedToShipping;
                                discount.UsePercentage = true;
                                discount.DiscountPercentage = 100;
                                break;
                        }
                        _discountService.InsertDiscount(discount);
                        discountCodes.Add(discount.CouponCode);
                    }
                }
            }

            return Json(discountCodes.ToArray());
        }

        [HttpPost]
        public IActionResult GetProductInfo(int productId)
        {
            var productInfo = new Dictionary<string, object>();

            try
            {
                var pluginDescriptor = _pluginService.GetPluginDescriptorBySystemName<IPlugin>(RetargetingDefaults.SystemName);
                if (pluginDescriptor == null)
                    throw new Exception(_localizationService.GetResource("Plugins.Widgets.Retargeting.ExceptionLoadPlugin"));

                if (!(pluginDescriptor.Instance<IPlugin>() is RetargetingPlugin plugin))
                    throw new Exception(_localizationService.GetResource("Plugins.Widgets.Retargeting.ExceptionLoadPlugin"));

                var product = _productService.GetProductById(productId);
                if (product == null)
                    return new NullJsonResult();

                #region Product details

                productInfo.Add("id", product.Id);
                productInfo.Add("name", JavaScriptEncoder.Default.Encode(_localizationService.GetLocalized(product, x => x.Name)) ?? "");
                productInfo.Add("url", string.Format("{0}{1}", _storeContext.CurrentStore.Url, _urlRecordService.GetSeName(product)));
                productInfo.Add("img", GetProductImageUrl(product));

                plugin.GetProductPrice(product, out var price, out var priceWithDiscount);
                productInfo.Add("price", price);
                productInfo.Add("promo", priceWithDiscount);

                #endregion

                #region Categories

                var categories = _categoryService.GetProductCategoriesByProductId(product.Id)
                    .Select(x => _categoryService.GetCategoryById(x.CategoryId))
                    .ToList();

                if (categories.Count == 0)
                    if (product.ParentGroupedProductId > 0)
                        categories = _categoryService.GetProductCategoriesByProductId(product.ParentGroupedProductId)
                                .Select(x => _categoryService.GetCategoryById(x.CategoryId))
                                .ToList();

                //product must have at least one category
                if (categories.Count == 0)
                    categories.Add(new Category { Name = "Default category" });

                var categoriesResult = new List<object>();
                foreach (var category in categories)
                {
                    var categoryObj = new Dictionary<string, object>
                    {
                        {"name", JavaScriptEncoder.Default.Encode(_localizationService.GetLocalized(category, x => x.Name) ?? "")},
                        {"id", category}
                    };

                    var breadcrumb = new List<object>();
                    if (category.ParentCategoryId > 0)
                    {
                        categoryObj.Add("parent", category.ParentCategoryId);

                        var parentCategory = _categoryService.GetCategoryById(category.ParentCategoryId);
                        if (parentCategory != null)
                        {
                            var bc1 = new Dictionary<string, object>
                            {
                                {"id", parentCategory.Id},
                                {"name", JavaScriptEncoder.Default.Encode(_localizationService.GetLocalized(parentCategory, x => x.Name) ?? "")}
                            };

                            if (parentCategory.ParentCategoryId > 0)
                                bc1.Add("parent", parentCategory.ParentCategoryId);
                            else
                                bc1.Add("parent", false);

                            breadcrumb.Add(bc1);

                            var parentParentCategory = _categoryService.GetCategoryById(parentCategory.ParentCategoryId);
                            if (parentParentCategory != null)
                            {
                                breadcrumb.Add(new Dictionary<string, object>
                                {
                                    {"id", parentParentCategory.Id},
                                    {"name", JavaScriptEncoder.Default.Encode(_localizationService.GetLocalized(parentParentCategory, x => x.Name) ?? "")},
                                    {"parent", false}
                                });
                            }
                        }
                    }
                    else
                    {
                        categoryObj.Add("parent", false);
                    }

                    categoryObj.Add("breadcrumb", breadcrumb);
                    categoriesResult.Add(categoryObj);
                }
                productInfo.Add("category", categoriesResult);

                #endregion

                #region Manufacturer

                var manufacturer = new Dictionary<string, object>();
                var manufacturers = _manufacturerService.GetProductManufacturersByProductId(productId)
                        .Select(x => _manufacturerService.GetManufacturerById(x.ManufacturerId))
                        .ToList();

                if (manufacturers.Count > 0)
                {
                    manufacturer.Add("id", manufacturers[0].Id);
                    manufacturer.Add("name", JavaScriptEncoder.Default.Encode(_localizationService.GetLocalized(manufacturers[0], m => m.Name) ?? ""));

                    productInfo.Add("brand", manufacturer);
                }
                else
                {
                    productInfo.Add("brand", false);
                }

                #endregion

                #region Inventory

                var inventory = new Dictionary<string, object>();
                var attributes = new Dictionary<string, object>();

                var allAttributeCombinationsXml = _productAttributeParser.GenerateAllCombinations(product, true);
                foreach (var attributeCombinationXml in allAttributeCombinationsXml)
                {
                    var warnings = new List<string>();
                    warnings.AddRange(_shoppingCartService.GetShoppingCartItemAttributeWarnings(_workContext.CurrentCustomer,
                        ShoppingCartType.ShoppingCart, product, 1, attributeCombinationXml, true));
                    if (warnings.Count != 0)
                        continue;

                    var inStock = false;

                    switch (product.ManageInventoryMethod)
                    {
                        case ManageInventoryMethod.ManageStock:
                            {
                                #region Manage stock

                                if (!product.DisplayStockAvailability)
                                    inStock = true;

                                var stockQuantity = _productService.GetTotalStockQuantity(product);
                                if (stockQuantity > 0)
                                    inStock = true;
                                else
                                    //out of stock
                                    switch (product.BackorderMode)
                                    {
                                        case BackorderMode.AllowQtyBelow0:
                                            inStock = true;
                                            break;
                                        case BackorderMode.AllowQtyBelow0AndNotifyCustomer:
                                            inStock = true;
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
                                    inStock = true;

                                var combination = _productAttributeParser.FindProductAttributeCombination(product, attributeCombinationXml);
                                if (combination != null)
                                {
                                    //combination exists
                                    var stockQuantity = combination.StockQuantity;
                                    if (stockQuantity > 0)
                                        inStock = true;
                                    else if (combination.AllowOutOfStockOrders)
                                        inStock = true;
                                }
                                else
                                {
                                    //no combination configured
                                    if (!product.AllowAddingOnlyExistingAttributeCombinations)
                                        inStock = true;
                                }

                                #endregion
                            }
                            break;
                        case ManageInventoryMethod.DontManageStock:
                        default:
                            inStock = true;
                            break;
                    }

                    var varCode = plugin.GetCombinationCode(attributeCombinationXml);
                    if (!attributes.ContainsKey(varCode))
                        attributes.Add(varCode, inStock);
                }
                if (attributes.Count > 0)
                {
                    inventory.Add("variations", true);
                    inventory.Add("stock", attributes);
                }
                else
                {
                    inventory.Add("variations", false);
                    inventory.Add("stock", product.StockQuantity > 0);
                }

                productInfo.Add("inventory", inventory);

                #endregion
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc.Message);
                _logger.Error(exc.Message, exc);
            }

            return Json(new
            {
                result = productInfo
            });
        }

        [HttpPost]
        public IActionResult IsProductCombinationInStock(int productId, IFormCollection form)
        {
            var product = _productService.GetProductById(productId);
            if (product == null)
                return new NullJsonResult();

            var pluginDescriptor = _pluginService.GetPluginDescriptorBySystemName<IPlugin>(RetargetingDefaults.SystemName);
            if (pluginDescriptor == null || !(pluginDescriptor.Instance<IPlugin>() is RetargetingPlugin plugin))
                throw new Exception(_localizationService.GetResource("Plugins.Widgets.Retargeting.ExceptionLoadPlugin"));

            var attributeXml = ParseProductAttributes(product, form);

            var productIsInStock = plugin.IsProductCombinationInStock(product, attributeXml, out var variationCode, out var variationDetails);

            return Json(new
            {
                stock = productIsInStock,
                variationCode = variationCode,
                variationDetails = variationDetails
            });
        }

        /// <summary>
        /// Subscribe to Retargeting news
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>True if successfully subscribed/unsubscribed, otherwise false</returns>
        public bool SubscribeToRetargeting(string newEmail, string oldEmail)
        {
            try
            {
                //unsubscribe previous email
                if (!string.IsNullOrEmpty(oldEmail))
                    SendEmail(oldEmail, false);

                //subscribe new email
                if (!string.IsNullOrEmpty(newEmail))
                    SendEmail(newEmail, true);

                return true;
            }
            catch (Exception exception)
            {
                //log errors
                var errorMessage = $"Retargeting subscription error: {exception.Message}.";
                _logger.Error(errorMessage, exception, _workContext.CurrentCustomer);

                return false;
            }
        }


        [Area(AreaNames.Admin)]
        [HttpPost, ActionName("Configure")]
        [FormValueRequired("subscribe")]
        public IActionResult Subscribe(ConfigurationModel model)
        {
            //if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
            //    return AccessDeniedView();

            //load settings
            var settings = _settingService.LoadSetting<RetargetingSettings>();
            if (settings.MerchantEmail == model.MerchantEmail)
                return Configure();

            //try to subscribe/unsubscribe
            var successfullySubscribed = SubscribeToRetargeting(model.MerchantEmail, settings.MerchantEmail);
            if (successfullySubscribed)
            {
                //save settings and display success notification
                settings.MerchantEmail = model.MerchantEmail;
                _settingService.SaveSetting(settings);

                var message = !string.IsNullOrEmpty(model.MerchantEmail)
                    ? _localizationService.GetResource("Plugins.Widgets.Retargeting.Subscribe.Success")
                    : _localizationService.GetResource("Plugins.Widgets.Retargeting.Unsubscribe.Success");
                _notificationService.SuccessNotification(message);
            }
            else
                _notificationService.ErrorNotification(_localizationService.GetResource("Plugins.Widgets.Retargeting.Subscribe.Error"));

            return Configure();
        }

        private void SendEmail(string email, bool subscribe)
        {
            //try to get an email account
            var emailAccount = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId)
                ?? throw new NopException("Email account could not be loaded");

            var subject = subscribe ? "New subscription" : "New unsubscription";
            var body = subscribe
                ? "nopCommerce user just left the email to receive an information about special offers from Retargeting."
                : "nopCommerce user has canceled subscription to receive Retargeting news.";

            //send email
            _emailSender.SendEmail(emailAccount: emailAccount,
                subject: subject, body: body,
                fromAddress: email, fromName: RetargetingDefaults.UserAgent,
                toAddress: RetargetingDefaults.SubscriptionEmail, toName: null);
        }

        #endregion
    }
}