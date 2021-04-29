using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Events;
using Nop.Core.Http.Extensions;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Plugins;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Date;
using Nop.Services.Stores;

namespace Nop.Plugin.Widgets.Retargeting.Services
{
    public class CustomShoppingCartService : ShoppingCartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderService _orderService;
        private readonly IPluginService _pluginService;
        private readonly IProductService _productService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;

        public CustomShoppingCartService(
            IHttpContextAccessor httpContextAccessor,
            IOrderService orderService,
            IPluginService pluginService,
            CatalogSettings catalogSettings,
            IAclService aclService,
            IActionContextAccessor actionContextAccessor,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICheckoutAttributeService checkoutAttributeService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IDateRangeService dateRangeService,
            IDateTimeHelper dateTimeHelper,
            IEventPublisher eventPublisher,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IPermissionService permissionService,
            IPriceCalculationService priceCalculationService,
            IPriceFormatter priceFormatter,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IProductService productService,
            IRepository<ShoppingCartItem> sciRepository,
            IShippingService shippingService,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            IUrlHelperFactory urlHelperFactory,
            IUrlRecordService urlRecordService,
            IWorkContext workContext,
            OrderSettings orderSettings,
            ShoppingCartSettings shoppingCartSettings) :
            base(catalogSettings, aclService, actionContextAccessor, checkoutAttributeParser, checkoutAttributeService, currencyService,
              customerService, dateRangeService, dateTimeHelper, genericAttributeService, localizationService, permissionService,
              priceCalculationService, priceFormatter, productAttributeParser, productAttributeService, productService, sciRepository, shippingService,
              staticCacheManager, storeContext, storeMappingService, urlHelperFactory, urlRecordService, workContext, orderSettings, shoppingCartSettings)
        {
            _httpContextAccessor = httpContextAccessor;
            _localizationService = localizationService;
            _pluginService = pluginService;
            _productService = productService;
            _orderService = orderService;
            _workContext = workContext;
            _storeContext = storeContext;
        }

        public override async Task<IList<string>> AddToCartAsync(Customer customer, Product product, ShoppingCartType shoppingCartType, int storeId,
            string attributesXml = null, decimal customerEnteredPrice = 0, DateTime? rentalStartDate = null,
            DateTime? rentalEndDate = null, int quantity = 1, bool automaticallyAddRequiredProductsIfEnabled = true)
        {
            var warnings = await base.AddToCartAsync(customer, product, shoppingCartType, storeId, attributesXml,
                customerEnteredPrice, rentalStartDate, rentalEndDate, quantity, automaticallyAddRequiredProductsIfEnabled);

            if (_httpContextAccessor.HttpContext?.Session != null)
            {
                var shoppingMigrationInProcess = _httpContextAccessor.HttpContext?.Session.Get<bool>("ra_shoppingMigrationInProcess");

                if (shoppingMigrationInProcess.HasValue && !shoppingMigrationInProcess.Value && warnings.Count == 0)
                {
                    if (warnings.Count == 0)
                    {
                        var pluginDescriptor = await _pluginService.GetPluginDescriptorBySystemNameAsync<IPlugin>("Widgets.Retargeting");
                        if (pluginDescriptor == null || pluginDescriptor.Instance<IPlugin>() is not RetargetingPlugin plugin)
                            throw new Exception(await _localizationService.GetResourceAsync("Plugins.Widgets.Retargeting.ExceptionLoadPlugin"));

                        object variation = false;

                        var (stock, variationCode, variationDetails) = await plugin.IsProductCombinationInStockAsync(product, attributesXml);
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

        public override async Task DeleteShoppingCartItemAsync(ShoppingCartItem shoppingCartItem, bool resetCheckoutData = true,
            bool ensureOnlyActiveCheckoutAttributes = false)
        {
            await base.DeleteShoppingCartItemAsync(shoppingCartItem, resetCheckoutData, ensureOnlyActiveCheckoutAttributes);

            var shoppingMigrationInProcess = _httpContextAccessor.HttpContext?.Session.Get<bool>("ra_shoppingMigrationInProcess");

            if (shoppingMigrationInProcess.HasValue && !shoppingMigrationInProcess.Value && shoppingCartItem.ShoppingCartType == ShoppingCartType.ShoppingCart)
            {
                var pluginDescriptor = await _pluginService.GetPluginDescriptorBySystemNameAsync<IPlugin>("Widgets.Retargeting");
                if (pluginDescriptor == null || pluginDescriptor.Instance<IPlugin>() is not RetargetingPlugin plugin)
                    throw new Exception(await _localizationService.GetResourceAsync("Plugins.Widgets.Retargeting.ExceptionLoadPlugin"));

                var shoppingCartItemsToDelete = _httpContextAccessor.HttpContext?.Session.Get<Dictionary<int, Dictionary<string, string>>>("ra_shoppingCartItemsToDelete");
                if (shoppingCartItemsToDelete == null)
                    shoppingCartItemsToDelete = new Dictionary<int, Dictionary<string, string>>();

                object variation = false;
                var product = await _productService.GetProductByIdAsync(shoppingCartItem.ProductId);

                var (stock, variationCode, variationDetails) = await plugin.IsProductCombinationInStockAsync(product, shoppingCartItem.AttributesXml);

                if (!string.IsNullOrEmpty(variationCode))
                    variation = new
                    {
                        code = variationCode,
                        stock = stock,
                        details = variationDetails
                    };

                var order = (await _orderService.SearchOrdersAsync(storeId: (await _storeContext.GetCurrentStoreAsync()).Id, customerId: (await _workContext.GetCurrentCustomerAsync()).Id, pageSize: 1))
                    .FirstOrDefault();

                if (!(order != null && 
                    order.CreatedOnUtc > DateTime.UtcNow.AddMinutes(-1) &&
                    (await _orderService.GetOrderItemsAsync(order.Id)).Any(item => item.ProductId == shoppingCartItem.ProductId)))
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

        public override async Task MigrateShoppingCartAsync(Customer fromCustomer, Customer toCustomer, bool includeCouponCodes)
        {
            _httpContextAccessor.HttpContext?.Session?.Set("ra_shoppingMigrationInProcess", true);

            await base.MigrateShoppingCartAsync(fromCustomer, toCustomer, includeCouponCodes);

            _httpContextAccessor.HttpContext?.Session?.Set("ra_shoppingMigrationInProcess", false);
        }
    }
}
