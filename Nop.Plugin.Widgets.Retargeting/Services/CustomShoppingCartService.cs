using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Plugins;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Events;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Stores;

namespace Nop.Plugin.Widgets.Retargeting.Services
{
    public class CustomShoppingCartService : ShoppingCartService
    {
        private readonly HttpContextBase _httpContext;
        private readonly IPluginFinder _pluginFinder;
        private readonly IProductService _productService;

        public CustomShoppingCartService(
            HttpContextBase httpContext,
            IPluginFinder pluginFinder,
            IRepository<ShoppingCartItem> sciRepository,
            IWorkContext workContext,
            IStoreContext storeContext,
            ICurrencyService currencyService,
            IProductService productService,
            ILocalizationService localizationService,
            IProductAttributeParser productAttributeParser,
            ICheckoutAttributeService checkoutAttributeService,
            ICheckoutAttributeParser checkoutAttributeParser,
            IPriceFormatter priceFormatter,
            ICustomerService customerService,
            ShoppingCartSettings shoppingCartSettings,
            IEventPublisher eventPublisher,
            IPermissionService permissionService,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            IGenericAttributeService genericAttributeService,
            IProductAttributeService productAttributeService,
            IDateTimeHelper dateTimeHelper) : base(sciRepository, workContext, storeContext, currencyService, productService, localizationService,
              productAttributeParser, checkoutAttributeService, checkoutAttributeParser, priceFormatter, customerService, shoppingCartSettings,
              eventPublisher, permissionService, aclService, storeMappingService, genericAttributeService, productAttributeService, dateTimeHelper)
        {
            _httpContext = httpContext;
            _pluginFinder = pluginFinder;
            _productService = productService;
        }

        public override IList<string> AddToCart(Customer customer, Product product, ShoppingCartType shoppingCartType, int storeId,
            string attributesXml = null, decimal customerEnteredPrice = 0, DateTime? rentalStartDate = null,
            DateTime? rentalEndDate = null, int quantity = 1, bool automaticallyAddRequiredProductsIfEnabled = true)
        {
            var warnings = base.AddToCart(customer, product, shoppingCartType, storeId, attributesXml,
                customerEnteredPrice, rentalStartDate, rentalEndDate, quantity, automaticallyAddRequiredProductsIfEnabled);

            if (warnings.Count == 0)
            {
                var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName("Widgets.Retargeting");
                if (pluginDescriptor == null)
                    throw new Exception("Cannot load the plugin");

                var plugin = pluginDescriptor.Instance() as RetargetingPlugin;
                if (plugin == null)
                    throw new Exception("Cannot load the plugin");

                object variation = false;
                string variationCode;
                Dictionary<string, object> variationDetails;

                var stock = plugin.IsProductCombinationInStock(product, attributesXml, out variationCode, out variationDetails);
                if (!string.IsNullOrEmpty(variationCode))
                    variation = new
                    {
                        code = variationCode,
                        stock = stock,
                        details = variationDetails
                    };

                var addToCartProductInfo = new
                {
                    shoppingCartType = shoppingCartType,
                    product_id = product.Id,
                    quantity = quantity,
                    variation = variation
                };

                if (_httpContext.Session != null)
                    _httpContext.Session["ra_addToCartProductInfo"] = addToCartProductInfo;
            }

            return warnings;
        }

        public override void DeleteShoppingCartItem(ShoppingCartItem shoppingCartItem, bool resetCheckoutData = true,
            bool ensureOnlyActiveCheckoutAttributes = false)
        {
            base.DeleteShoppingCartItem(shoppingCartItem, resetCheckoutData, ensureOnlyActiveCheckoutAttributes);

            if (shoppingCartItem.ShoppingCartType == ShoppingCartType.ShoppingCart && _httpContext.Session != null)
            {
                var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName("Widgets.Retargeting");
                if (pluginDescriptor == null)
                    throw new Exception("Cannot load the plugin");

                var plugin = pluginDescriptor.Instance() as RetargetingPlugin;
                if (plugin == null)
                    throw new Exception("Cannot load the plugin");

                var shoppingCartItemsToDelete =
                    _httpContext.Session["ra_shoppingCartItemsToDelete"] as Dictionary<int, Dictionary<string, string>> ??
                    new Dictionary<int, Dictionary<string, string>>();

                object variation = false;
                string variationCode;
                Dictionary<string, object> variationDetails;

                var product = _productService.GetProductById(shoppingCartItem.ProductId);

                var stock = plugin.IsProductCombinationInStock(product, shoppingCartItem.AttributesXml,
                    out variationCode, out variationDetails);
                if (!string.IsNullOrEmpty(variationCode))
                    variation = new
                    {
                        code = variationCode,
                        stock = stock,
                        details = variationDetails
                    };

                shoppingCartItemsToDelete.Add(shoppingCartItem.ProductId, new Dictionary<string, string>()
                {
                    {"quantity", shoppingCartItem.Quantity.ToString()},
                    {"variation",  new JavaScriptSerializer().Serialize(variation)}
                });

                _httpContext.Session["ra_shoppingCartItemsToDelete"] = shoppingCartItemsToDelete;
            }
        }
    }
}
