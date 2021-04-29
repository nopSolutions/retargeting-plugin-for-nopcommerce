using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
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
using Nop.Services.Security;
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

        private readonly IPermissionService _permissionService;
        private readonly ICategoryService _categoryService;
        private readonly IDiscountService _discountService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IManufacturerService _manufacturerService;
        private readonly INotificationService _notificationService;
        private readonly IPictureService _pictureService;
        private readonly IPluginService _pluginService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductService _productService;
        private readonly ISettingService _settingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IUrlRecordService _urlRecordService;

        private readonly ILogger _logger;
        private readonly IEmailSender _emailSender;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly IProductAttributeParser _productAttributeParser;

        private readonly EmailAccountSettings _emailAccountSettings;

        #endregion

        #region Ctor

        public WidgetsRetargetingController(
            IPermissionService permissionService,
            ICategoryService categoryService,
            IDiscountService discountService,
            IEmailAccountService emailAccountService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IManufacturerService manufacturerService,
            INotificationService notificationService,
            IPictureService pictureService,
            IPluginService pluginService,
            IProductAttributeService productAttributeService,
            IProductService productService,
            ISettingService settingService,
            IShoppingCartService shoppingCartService,
            IStoreContext storeContext,
            IUrlRecordService urlRecordService,

            ILogger logger,
            IEmailSender emailSender,
            IWorkContext workContext,
            IProductAttributeParser productAttributeParser,

            EmailAccountSettings emailAccountSettings
            )
        {
            _permissionService = permissionService;
            _categoryService = categoryService;
            _discountService = discountService;
            _emailAccountService = emailAccountService;
            _genericAttributeService = genericAttributeService;
            _localizationService = localizationService;
            _manufacturerService = manufacturerService;
            _notificationService = notificationService;
            _pictureService = pictureService;
            _pluginService = pluginService;
            _productAttributeService = productAttributeService;
            _productService = productService;
            _settingService = settingService;
            _shoppingCartService = shoppingCartService;
            _urlRecordService = urlRecordService;

            _logger = logger;
            _emailSender = emailSender;
            _workContext = workContext;
            _storeContext = storeContext;
            _productAttributeParser = productAttributeParser;

            _emailAccountSettings = emailAccountSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Sends an email
        /// </summary>
        /// <param name="email">Email</param>
        /// <param name="subscribe">If subscribed</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        private async Task SendEmailAsync(string email, bool subscribe)
        {
            //try to get an email account
            var emailAccount = await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId)
                ?? throw new NopException("Email account could not be loaded");

            var subject = subscribe ? "New subscription" : "New unsubscription";
            var body = subscribe
                ? "nopCommerce user just left the email to receive an information about special offers from Retargeting."
                : "nopCommerce user has canceled subscription to receive Retargeting news.";

            //send email
            await _emailSender.SendEmailAsync(emailAccount: emailAccount,
                subject: subject, body: body,
                fromAddress: email, fromName: RetargetingDefaults.UserAgent,
                toAddress: RetargetingDefaults.SubscriptionEmail, toName: null);
        }

        /// <summary>
        /// Gets a product image url
        /// </summary>
        /// <param name="product">Product</param>
        /// <returns>Product image url</returns>
        private async Task<string> GetProductImageUrlAsync(Product product)
        {
            var pictures = await _pictureService.GetPicturesByProductIdAsync(product.Id);
            var defaultPicture = pictures.FirstOrDefault();

            return await _pictureService.GetPictureUrlAsync(defaultPicture.Id);
        }

        /// <summary>
        /// Parses product attributes
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="form">Form</param>
        /// <returns>Attributes xml</returns>
        private async Task<string> ParseProductAttributesAsync(Product product, IFormCollection form)
        {
            var attributesXml = "";

            var productAttributes = await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(product.Id);
            foreach (var attribute in productAttributes)
            {
                var controlId = $"{NopCatalogDefaults.ProductAttributePrefix}{attribute.Id}";
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
                            var attributeValues = await _productAttributeService.GetProductAttributeValuesAsync(attribute.Id);
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
                var conditionMet = await _productAttributeParser.IsConditionMetAsync(attribute, attributesXml);
                if (conditionMet.HasValue && !conditionMet.Value)
                {
                    attributesXml = _productAttributeParser.RemoveProductAttribute(attributesXml, attribute);
                }
            }

            return attributesXml;
        }

        /// <summary>
        /// Subscribe to Retargeting news
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>True if successfully subscribed/unsubscribed, otherwise false</returns>
        private async Task<bool> SubscribeToRetargetingAsync(string newEmail, string oldEmail)
        {
            try
            {
                //unsubscribe previous email
                if (!string.IsNullOrEmpty(oldEmail))
                    await SendEmailAsync(oldEmail, false);

                //subscribe new email
                if (!string.IsNullOrEmpty(newEmail))
                    await SendEmailAsync(newEmail, true);

                return true;
            }
            catch (Exception exception)
            {
                //log errors
                var errorMessage = $"Retargeting subscription error: {exception.Message}.";
                await _logger.ErrorAsync(errorMessage, exception, (await _workContext.GetCurrentCustomerAsync()));

                return false;
            }
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var retargetingSettings = await _settingService.LoadSettingAsync<RetargetingSettings>(storeScope);

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

                HideConfigurationBlock = await _genericAttributeService.GetAttributeAsync<bool>(await _workContext.GetCurrentCustomerAsync(), RetargetingDefaults.HideConfigurationBlock),
                HidePreconfigureBlock = await _genericAttributeService.GetAttributeAsync<bool>(await _workContext.GetCurrentCustomerAsync(), RetargetingDefaults.HidePreconfigureBlock),

                MerchantEmail = retargetingSettings.MerchantEmail,

                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope > 0)
            {
                model.TrackingApiKey_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.TrackingApiKey, storeScope);
                model.RestApiKey_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.RestApiKey, storeScope);
                model.UseHttpPostInsteadOfAjaxInAddToCart_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.UseHttpPostInsteadOfAjaxInAddToCart, storeScope);
                model.AddToCartButtonDetailsPrefix_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.AddToCartButtonIdDetailsPrefix, storeScope);
                model.PriceLabelSelector_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.ProductPriceLabelDetailsSelector, storeScope);
                model.AddToWishlistButtonIdDetailsPrefix_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.AddToWishlistButtonIdDetailsPrefix, storeScope);
                model.HelpTopicSystemNames_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.HelpTopicSystemNames, storeScope);
                model.AddToWishlistCatalogButtonSelector_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.AddToWishlistCatalogButtonSelector, storeScope);
                model.ProductReviewAddedResultSelector_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.ProductReviewAddedResultSelector, storeScope);
                model.AddToCartCatalogButtonSelector_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.AddToCartCatalogButtonSelector, storeScope);
                model.ProductBoxSelector_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.ProductBoxSelector, storeScope);
                model.ProductMainPictureIdDetailsPrefix_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.ProductMainPictureIdDetailsPrefix, storeScope);

                model.RecommendationHomePage_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.RecommendationHomePage, storeScope);
                model.RecommendationCategoryPage_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.RecommendationCategoryPage, storeScope);
                model.RecommendationProductPage_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.RecommendationProductPage, storeScope);
                model.RecommendationCheckoutPage_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.RecommendationCheckoutPage, storeScope);
                model.RecommendationThankYouPage_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.RecommendationThankYouPage, storeScope);
                model.RecommendationOutOfStockPage_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.RecommendationOutOfStockPage, storeScope);
                model.RecommendationSearchPage_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.RecommendationSearchPage, storeScope);
                model.RecommendationPageNotFound_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.RecommendationPageNotFound, storeScope);

                model.MerchantEmail_OverrideForStore = await _settingService.SettingExistsAsync(retargetingSettings, x => x.MerchantEmail, storeScope);
            }

            return View("~/Plugins/Widgets.Retargeting/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [FormValueRequired("save")]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var retargetingSettings = await _settingService.LoadSettingAsync<RetargetingSettings>(storeScope);

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
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.TrackingApiKey, model.TrackingApiKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.RestApiKey, model.RestApiKey_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.UseHttpPostInsteadOfAjaxInAddToCart, model.UseHttpPostInsteadOfAjaxInAddToCart_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.AddToCartButtonIdDetailsPrefix, model.AddToCartButtonDetailsPrefix_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.ProductPriceLabelDetailsSelector, model.PriceLabelSelector_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.AddToWishlistButtonIdDetailsPrefix, model.AddToWishlistButtonIdDetailsPrefix_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.HelpTopicSystemNames, model.HelpTopicSystemNames_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.AddToWishlistCatalogButtonSelector, model.AddToWishlistCatalogButtonSelector_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.ProductReviewAddedResultSelector, model.ProductReviewAddedResultSelector_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.AddToCartCatalogButtonSelector, model.AddToCartCatalogButtonSelector_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.ProductBoxSelector, model.ProductBoxSelector_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.ProductMainPictureIdDetailsPrefix, model.ProductMainPictureIdDetailsPrefix_OverrideForStore, storeScope, false);

            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.RecommendationHomePage, model.RecommendationHomePage_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.RecommendationCategoryPage, model.RecommendationCategoryPage_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.RecommendationProductPage, model.RecommendationProductPage_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.RecommendationCheckoutPage, model.RecommendationCheckoutPage_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.RecommendationThankYouPage, model.RecommendationThankYouPage_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.RecommendationOutOfStockPage, model.RecommendationOutOfStockPage_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.RecommendationSearchPage, model.RecommendationSearchPage_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.RecommendationPageNotFound, model.RecommendationPageNotFound_OverrideForStore, storeScope, false);

            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.MerchantEmail, model.MerchantEmail_OverrideForStore, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        [Area(AreaNames.Admin)]
        [HttpPost, ActionName("Configure")]
        [FormValueRequired("preconfigure")]
        public async Task<IActionResult> Preconfigure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            var pluginDescriptor = await _pluginService.GetPluginDescriptorBySystemNameAsync<IPlugin>(RetargetingDefaults.SystemName);
            if (pluginDescriptor != null)
            {
                if (pluginDescriptor.Instance<IPlugin>() is RetargetingPlugin plugin)
                {
                    try
                    {
                        await plugin.PreconfigureAsync();
                        var message = await _localizationService.GetResourceAsync("Plugins.Widgets.Retargeting.PreconfigureCompleted");
                        _notificationService.SuccessNotification(message);
                        await _logger.InformationAsync(message);
                    }
                    catch (Exception exception)
                    {
                        var message = await _localizationService.GetResourceAsync("Plugins.Widgets.Retargeting.PreconfigureError") + exception;
                        _notificationService.ErrorNotification(message);
                        await _logger.ErrorAsync(message);
                    }

                    return await Configure();
                }
            }

            throw new Exception(await _localizationService.GetResourceAsync("Plugins.Widgets.Retargeting.ExceptionLoadPlugin"));
        }

        [Area(AreaNames.Admin)]
        [HttpPost, ActionName("Configure")]
        [FormValueRequired("reset-settings")]
        public async Task<IActionResult> ResetSettings()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePlugins))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var retargetingSettings = await _settingService.LoadSettingAsync<RetargetingSettings>(storeScope);

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
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.UseHttpPostInsteadOfAjaxInAddToCart, false, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.AddToCartButtonIdDetailsPrefix, false, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.ProductPriceLabelDetailsSelector, false, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.AddToWishlistButtonIdDetailsPrefix, false, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.HelpTopicSystemNames, false, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.AddToWishlistCatalogButtonSelector, false, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.ProductReviewAddedResultSelector, false, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.AddToCartCatalogButtonSelector, false, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.ProductBoxSelector, false, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(retargetingSettings, x => x.ProductMainPictureIdDetailsPrefix, false, storeScope, false);

            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Plugins.Widgets.Retargeting.SettingsReset"));

            return await Configure();
        }

        public async Task<IActionResult> ProductStockFeed()
        {
            var fileName = string.Format("feed_{0}_{1}.csv", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"), CommonHelper.GenerateRandomDigitCode(4));
            var result = string.Empty;

            try
            {
                var pluginDescriptor = await _pluginService.GetPluginDescriptorBySystemNameAsync<IPlugin>("Widgets.Retargeting");
                if (pluginDescriptor == null || pluginDescriptor.Instance<IPlugin>() is not RetargetingPlugin plugin)
                    throw new Exception(await _localizationService.GetResourceAsync("Plugins.Widgets.Retargeting.ExceptionLoadPlugin"));

                result = await plugin.ExportProductsToCsvAsync();
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc.Message);
                await _logger.ErrorAsync(exc.Message, exc);
            }

            return File(Encoding.UTF8.GetBytes(result), MimeTypes.TextCsv, fileName);
        }

        public async Task<IActionResult> GenerateDiscounts(string key, string value, int type, int count)
        {
            var discountCodes = new List<string>();

            var retargetingSettings = await _settingService.LoadSettingAsync<RetargetingSettings>((await _storeContext.GetCurrentStoreAsync()).Id);
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
                        await _discountService.InsertDiscountAsync(discount);
                        discountCodes.Add(discount.CouponCode);
                    }
                }
            }

            return Json(discountCodes.ToArray());
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GetProductInfo(int productId)
        {
            var productInfo = new Dictionary<string, object>();

            try
            {
                var pluginDescriptor = await _pluginService.GetPluginDescriptorBySystemNameAsync<IPlugin>("Widgets.Retargeting");
                if (pluginDescriptor == null || pluginDescriptor.Instance<IPlugin>() is not RetargetingPlugin plugin)
                    throw new Exception(await _localizationService.GetResourceAsync("Plugins.Widgets.Retargeting.ExceptionLoadPlugin"));

                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null)
                    return new NullJsonResult();

                #region Product details

                productInfo.Add("id", product.Id);
                productInfo.Add("name", JavaScriptEncoder.Default.Encode(await _localizationService.GetLocalizedAsync(product, x => x.Name)) ?? "");
                productInfo.Add("url", string.Format("{0}{1}", (await _storeContext.GetCurrentStoreAsync()).Url, await _urlRecordService.GetSeNameAsync(product)));
                productInfo.Add("img", await GetProductImageUrlAsync(product));

                var (price, priceWithDiscount) = await plugin.GetProductPrice(product);
                productInfo.Add("price", price);
                productInfo.Add("promo", priceWithDiscount);

                #endregion

                #region Categories

                var categories = await (await _categoryService.GetProductCategoriesByProductIdAsync(product.Id))
                    .SelectAwait(async x => await _categoryService.GetCategoryByIdAsync(x.CategoryId))
                    .ToListAsync();

                if (categories.Count == 0)
                    if (product.ParentGroupedProductId > 0)
                        categories = await (await _categoryService.GetProductCategoriesByProductIdAsync(product.ParentGroupedProductId))
                            .SelectAwait(async x => await _categoryService.GetCategoryByIdAsync(x.CategoryId))
                            .ToListAsync();

                //product must have at least one category
                if (categories.Count == 0)
                    categories.Add(new Category { Name = "Default category" });

                var categoriesResult = new List<object>();
                foreach (var category in categories)
                {
                    var categoryObj = new Dictionary<string, object>
                    {
                        {"name", JavaScriptEncoder.Default.Encode(await _localizationService.GetLocalizedAsync(category, x => x.Name))},
                        {"id", category.Id}
                    };

                    var breadcrumb = new List<object>();
                    if (category.ParentCategoryId > 0)
                    {
                        categoryObj.Add("parent", category.ParentCategoryId);

                        var parentCategory = await _categoryService.GetCategoryByIdAsync(category.ParentCategoryId);
                        if (parentCategory != null)
                        {
                            var bc1 = new Dictionary<string, object>
                            {
                                {"id", parentCategory.Id},
                                {"name", JavaScriptEncoder.Default.Encode(await _localizationService.GetLocalizedAsync(parentCategory, x => x.Name))}
                            };

                            if (parentCategory.ParentCategoryId > 0)
                                bc1.Add("parent", parentCategory.ParentCategoryId);
                            else
                                bc1.Add("parent", false);

                            breadcrumb.Add(bc1);

                            var parentParentCategory = await _categoryService.GetCategoryByIdAsync(parentCategory.ParentCategoryId);
                            if (parentParentCategory != null)
                            {
                                breadcrumb.Add(new Dictionary<string, object>
                            {
                                {"id", parentParentCategory.Id},
                                {"name", JavaScriptEncoder.Default.Encode(await _localizationService.GetLocalizedAsync(parentParentCategory, x => x.Name))},
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
                var manufacturers = await (await _manufacturerService.GetProductManufacturersByProductIdAsync(productId))
                        .SelectAwait(async x => await _manufacturerService.GetManufacturerByIdAsync(x.ManufacturerId))
                        .ToListAsync();

                if (manufacturers.Count > 0)
                {
                    manufacturer.Add("id", manufacturers[0].Id);
                    manufacturer.Add("name", JavaScriptEncoder.Default.Encode(await _localizationService.GetLocalizedAsync(manufacturers[0], x => x.Name)));
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

                var allAttributeCombinationsXml = await _productAttributeParser.GenerateAllCombinationsAsync(product, true);
                foreach (var attributeCombinationXml in allAttributeCombinationsXml)
                {
                    var warnings = new List<string>();
                    warnings.AddRange(await _shoppingCartService.GetShoppingCartItemAttributeWarningsAsync((await _workContext.GetCurrentCustomerAsync()),
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

                                var stockQuantity = await _productService.GetTotalStockQuantityAsync(product);
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

                                var combination = await _productAttributeParser.FindProductAttributeCombinationAsync(product, attributeCombinationXml);
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

                    var varCode = await plugin.GetCombinationCodeAsync(attributeCombinationXml);
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
                await _logger.ErrorAsync(exc.Message, exc);
            }

            return Json(new
            {
                result = productInfo
            });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> IsProductCombinationInStock(int productId, IFormCollection form)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null)
                return new NullJsonResult();

            var pluginDescriptor = await _pluginService.GetPluginDescriptorBySystemNameAsync<IPlugin>("Widgets.Retargeting");
            if (pluginDescriptor == null || pluginDescriptor.Instance<IPlugin>() is not RetargetingPlugin plugin)
                throw new Exception(await _localizationService.GetResourceAsync("Plugins.Widgets.Retargeting.ExceptionLoadPlugin"));

            var attributeXml = await ParseProductAttributesAsync(product, form);

            var (productIsInStock, variationCode, variationDetails) = await plugin.IsProductCombinationInStockAsync(product, attributeXml);

            return Json(new
            {
                stock = productIsInStock,
                variationCode = variationCode,
                variationDetails = variationDetails
            });
        }

        [Area(AreaNames.Admin)]
        [HttpPost, ActionName("Configure")]
        [FormValueRequired("subscribe")]
        public async Task<IActionResult> SubscribeAsync(ConfigurationModel model)
        {
            //load settings
            var settings = await _settingService.LoadSettingAsync<RetargetingSettings>();
            if (settings.MerchantEmail == model.MerchantEmail)
                return await Configure();

            //try to subscribe/unsubscribe
            var successfullySubscribed = await SubscribeToRetargetingAsync(model.MerchantEmail, settings.MerchantEmail);
            if (successfullySubscribed)
            {
                //save settings and display success notification
                settings.MerchantEmail = model.MerchantEmail;
                await _settingService.SaveSettingAsync(settings);

                var message = !string.IsNullOrEmpty(model.MerchantEmail)
                    ? await _localizationService.GetResourceAsync("Plugins.Widgets.Retargeting.Subscribe.Success")
                    : await _localizationService.GetResourceAsync("Plugins.Widgets.Retargeting.Unsubscribe.Success");
                _notificationService.SuccessNotification(message);
            }
            else
                _notificationService.ErrorNotification(await _localizationService.GetResourceAsync("Plugins.Widgets.Retargeting.Subscribe.Error"));

            return await Configure();
        }
    }

    #endregion
}