using System;
using System.Collections.Generic;
using System.Linq;
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
using Nop.Services.Shipping.Date;
using Nop.Services.Stores;
using Microsoft.AspNetCore.Http;
using Nop.Core.Http.Extensions;
using Newtonsoft.Json;

namespace Nop.Plugin.Widgets.Retargeting.Services
{
    public class CustomShoppingCartService : ShoppingCartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPluginFinder _pluginFinder;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;

        public CustomShoppingCartService(
            IHttpContextAccessor httpContextAccessor,
            IPluginFinder pluginFinder,
            IOrderService orderService,
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
            OrderSettings orderSettings,
            ShoppingCartSettings shoppingCartSettings,
            IEventPublisher eventPublisher,
            IPermissionService permissionService,
            IAclService aclService,
            IDateRangeService dateRangeService,
            IStoreMappingService storeMappingService,
            IGenericAttributeService genericAttributeService,
            IProductAttributeService productAttributeService,
            IDateTimeHelper dateTimeHelper) : base(sciRepository, workContext, storeContext, currencyService, productService, localizationService,
              productAttributeParser, checkoutAttributeService, checkoutAttributeParser, priceFormatter, customerService, orderSettings, shoppingCartSettings,
              eventPublisher, permissionService, aclService, dateRangeService, storeMappingService, genericAttributeService, 
              productAttributeService, dateTimeHelper)
        {
            _httpContextAccessor = httpContextAccessor;
            _pluginFinder = pluginFinder;
            _productService = productService;
            _orderService = orderService;
            _workContext = workContext;
            _storeContext = storeContext;
        }

        public override IList<string> AddToCart(Customer customer, Product product, ShoppingCartType shoppingCartType, int storeId,
            string attributesXml = null, decimal customerEnteredPrice = 0, DateTime? rentalStartDate = null,
            DateTime? rentalEndDate = null, int quantity = 1, bool automaticallyAddRequiredProductsIfEnabled = true)
        {
            var warnings = base.AddToCart(customer, product, shoppingCartType, storeId, attributesXml,
                customerEnteredPrice, rentalStartDate, rentalEndDate, quantity, automaticallyAddRequiredProductsIfEnabled);

            if (_httpContextAccessor.HttpContext?.Session != null)
            {
                var shoppingMigrationInProcess = _httpContextAccessor.HttpContext?.Session.Get<bool>("ra_shoppingMigrationInProcess");

                if (shoppingMigrationInProcess.HasValue && !shoppingMigrationInProcess.Value && warnings.Count == 0)
                {
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

                        _httpContextAccessor.HttpContext?.Session.Set("ra_addToCartProductInfo", addToCartProductInfo);
                    }
                }
            }

            return warnings;
        }

        public override void DeleteShoppingCartItem(ShoppingCartItem shoppingCartItem, bool resetCheckoutData = true,
            bool ensureOnlyActiveCheckoutAttributes = false)
        {
            base.DeleteShoppingCartItem(shoppingCartItem, resetCheckoutData, ensureOnlyActiveCheckoutAttributes);

            var shoppingMigrationInProcess = _httpContextAccessor.HttpContext?.Session.Get<bool>("ra_shoppingMigrationInProcess");

            if (shoppingMigrationInProcess.HasValue && !shoppingMigrationInProcess.Value && shoppingCartItem.ShoppingCartType == ShoppingCartType.ShoppingCart)
            {
                var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName("Widgets.Retargeting");
                if (pluginDescriptor == null)
                    throw new Exception("Cannot load the plugin");

                var plugin = pluginDescriptor.Instance() as RetargetingPlugin;
                if (plugin == null)
                    throw new Exception("Cannot load the plugin");

                var shoppingCartItemsToDelete = _httpContextAccessor.HttpContext?.Session.Get<Dictionary<int, Dictionary<string, string>>>("ra_shoppingCartItemsToDelete");
                if (shoppingCartItemsToDelete == null)
                    shoppingCartItemsToDelete = new Dictionary<int, Dictionary<string, string>>();

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

                var order = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
                    customerId: _workContext.CurrentCustomer.Id, pageSize: 1)
                    .FirstOrDefault();

                if (!(order != null
                      && order.CreatedOnUtc > DateTime.UtcNow.AddMinutes(-1)
                      && order.OrderItems.Any(item => item.ProductId == shoppingCartItem.ProductId)))
                {
                    if (!shoppingCartItemsToDelete.ContainsKey(shoppingCartItem.Id))
                        shoppingCartItemsToDelete.Add(shoppingCartItem.Id, new Dictionary<string, string>
                                    {
                                        {"productId", shoppingCartItem.ProductId.ToString()},
                                        {"quantity", shoppingCartItem.Quantity.ToString()},
                                        {"variation",  JsonConvert.SerializeObject(variation)}
                                    });
                }

                _httpContextAccessor.HttpContext?.Session?.Set("ra_shoppingCartItemsToDelete", shoppingCartItemsToDelete);
            }
        }

        public override void MigrateShoppingCart(Customer fromCustomer, Customer toCustomer, bool includeCouponCodes)
        {
            _httpContextAccessor.HttpContext?.Session?.Set("ra_shoppingMigrationInProcess", true);

            base.MigrateShoppingCart(fromCustomer, toCustomer, includeCouponCodes);

            _httpContextAccessor.HttpContext?.Session?.Set("ra_shoppingMigrationInProcess", false);
        }
    }
}
