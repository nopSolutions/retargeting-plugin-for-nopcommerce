using System;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Core.Plugins;
using Nop.Services.Affiliates;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using Nop.Services.Vendors;

namespace Nop.Plugin.Widgets.Retargeting.Services
{
    public class CustomOrderProcessingService : OrderProcessingService
    {
        private readonly IPluginFinder _pluginFinder;

        public CustomOrderProcessingService(
            IPluginFinder pluginFinder,
            IOrderService orderService,
            IWebHelper webHelper,
            ILocalizationService localizationService,
            ILanguageService languageService,
            IProductService productService,
            IPaymentService paymentService,
            ILogger logger,
            IOrderTotalCalculationService orderTotalCalculationService,
            IPriceCalculationService priceCalculationService,
            IPriceFormatter priceFormatter,
            IProductAttributeParser productAttributeParser,
            IProductAttributeFormatter productAttributeFormatter,
            IGiftCardService giftCardService,
            IShoppingCartService shoppingCartService,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            IShippingService shippingService,
            IShipmentService shipmentService,
            ITaxService taxService,
            ICustomerService customerService,
            IDiscountService discountService,
            IEncryptionService encryptionService,
            IWorkContext workContext,
            IWorkflowMessageService workflowMessageService,
            IVendorService vendorService,
            ICustomerActivityService customerActivityService,
            ICurrencyService currencyService,
            IAffiliateService affiliateService,
            IEventPublisher eventPublisher,
            IPdfService pdfService,
            IRewardPointService rewardPointService,
            IGenericAttributeService genericAttributeService,
            ICountryService countryService,
            IStateProvinceService stateProvinceService,
            ShippingSettings shippingSettings,
            PaymentSettings paymentSettings,
            RewardPointsSettings rewardPointsSettings,
            OrderSettings orderSettings,
            TaxSettings taxSettings,
            LocalizationSettings localizationSettings,
            CurrencySettings currencySettings,
            ICustomNumberFormatter customNumberFormatter) : 
            base(orderService, webHelper, localizationService, languageService, productService, paymentService, logger, 
                orderTotalCalculationService, priceCalculationService, priceFormatter, productAttributeParser, 
                productAttributeFormatter, giftCardService, shoppingCartService, checkoutAttributeFormatter, 
                shippingService, shipmentService, taxService, customerService, discountService, encryptionService, 
                workContext, workflowMessageService, vendorService, customerActivityService, currencyService, 
                affiliateService, eventPublisher, pdfService, rewardPointService, genericAttributeService, countryService,
                stateProvinceService, shippingSettings, paymentSettings, rewardPointsSettings, orderSettings, taxSettings, 
                localizationSettings, currencySettings, customNumberFormatter)
        {
            _pluginFinder = pluginFinder;
        }

        public override PlaceOrderResult PlaceOrder(ProcessPaymentRequest processPaymentRequest)
        {
            var placeOrderResult = base.PlaceOrder(processPaymentRequest);

            if (placeOrderResult.Success)
            {
                var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName("Widgets.Retargeting");
                if (pluginDescriptor == null)
                    throw new Exception("Cannot load the plugin");

                var plugin = pluginDescriptor.Instance() as RetargetingPlugin;
                if (plugin == null)
                    throw new Exception("Cannot load the plugin");

                plugin.SendOrder(placeOrderResult.PlacedOrder.Id);
            }

            return placeOrderResult;
        }
    }
}
