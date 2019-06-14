using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Tax;
using Nop.Core.Http.Extensions;
using Nop.Plugin.Widgets.Retargeting.Models;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Orders;
using Nop.Services.Topics;
using Nop.Web.Framework.Components;
using OrderItem = Nop.Plugin.Widgets.Retargeting.Models.OrderItem;

namespace Nop.Plugin.Widgets.Retargeting.Components
{
    [ViewComponent(Name = RetargetingDefaults.RETARGETING_VIEW_COMPONENT_NAME)]
    public class WidgetsRetargetingViewComponent : NopViewComponent
    {
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly ICategoryService _categoryService;
        private readonly ICustomerService _customerService;
        private readonly IDiscountService _discountService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IManufacturerService _manufacturerService;
        private readonly IOrderService _orderService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductService _productService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly ITopicService _topicService;
        private readonly IWorkContext _workContext;
        private readonly MediaSettings _mediaSettings;

        public WidgetsRetargetingViewComponent(
            IActionContextAccessor actionContextAccessor,
            ICategoryService categoryService,
            ICustomerService customerService,
            IDiscountService discountService,
            IGenericAttributeService genericAttributeService,
            IHttpContextAccessor httpContextAccessor,
            IManufacturerService manufacturerService,
            IOrderService orderService,
            IProductService productService,
            IProductAttributeParser productAttributeParser,
            ISettingService settingService,
            IStoreContext storeContext,
            ITopicService topicService,
            IWorkContext workContext,
            MediaSettings mediaSettings
            )
        {
            _actionContextAccessor = actionContextAccessor;
            _categoryService = categoryService;
            _customerService = customerService;
            _discountService = discountService;
            _genericAttributeService = genericAttributeService;
            _httpContextAccessor = httpContextAccessor;
            _manufacturerService = manufacturerService;
            _orderService = orderService;
            _productService = productService;
            _productAttributeParser = productAttributeParser;
            _settingService = settingService;
            _storeContext = storeContext;
            _topicService = topicService;
            _workContext = workContext;
            _mediaSettings = mediaSettings;
        }

        public IViewComponentResult Invoke(string widgetZone, object additionalData)
        {
            var retargetingSettings = _settingService.LoadSetting<RetargetingSettings>(_storeContext.CurrentStore.Id);

            var model = new PublicInfoModel
            {
                TrackingApiKey = retargetingSettings.TrackingApiKey,
                AddToCartButtonIdDetailsPrefix = retargetingSettings.AddToCartButtonIdDetailsPrefix,
                ProductPriceLabelDetailsSelector = retargetingSettings.ProductPriceLabelDetailsSelector,
                AddToWishlistButtonIdDetailsPrefix = retargetingSettings.AddToWishlistButtonIdDetailsPrefix,
                AddToWishlistCatalogButtonSelector = retargetingSettings.AddToWishlistCatalogButtonSelector,
                ProductReviewAddedResultSelector = retargetingSettings.ProductReviewAddedResultSelector,
                AddToCartCatalogButtonSelector = retargetingSettings.AddToCartCatalogButtonSelector,
                ProductBoxSelector = retargetingSettings.ProductBoxSelector,
                ProductMainPictureIdDetailsPrefix = retargetingSettings.ProductMainPictureIdDetailsPrefix,

                RenderAddToCartFunc = true,
                RenderAddToWishlistFunc = true,

                RecommendationHomePage = retargetingSettings.RecommendationHomePage,
                RecommendationCategoryPage = retargetingSettings.RecommendationCategoryPage,
                RecommendationProductPage = retargetingSettings.RecommendationProductPage,
                RecommendationCheckoutPage = retargetingSettings.RecommendationCheckoutPage,
                RecommendationThankYouPage = retargetingSettings.RecommendationThankYouPage,
                RecommendationOutOfStockPage = retargetingSettings.RecommendationOutOfStockPage,
                RecommendationSearchPage = retargetingSettings.RecommendationSearchPage,
                RecommendationPageNotFound = retargetingSettings.RecommendationPageNotFound
            };

            var routeData = _actionContextAccessor.ActionContext.RouteData;
            var controllerName = routeData.Values["controller"];
            var actionName = routeData.Values["action"];

            //user info
            var customerId = _httpContextAccessor.HttpContext?.Session?.GetInt32("ra_CustomerId");

            if (customerId.HasValue)
            {
                var customer = _customerService.GetCustomerById(customerId.Value);
                if (customer != null)
                {
                    model.RenderSetEmailFunc = true;
                    model.CustomerModel = new CustomerModel()
                    {
                        Name = JavaScriptEncoder.Default.Encode(
                               _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.FirstNameAttribute) + " " +
                               _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.LastNameAttribute)),
                        Email = customer.Email,
                        City = JavaScriptEncoder.Default.Encode(_genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.CityAttribute) ?? ""),
                        Phone = JavaScriptEncoder.Default.Encode(_genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.PhoneAttribute) ?? "")
                    };

                    var gender = _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.GenderAttribute);
                    switch (gender)
                    {
                        case "M":
                            model.CustomerModel.Sex = "1";
                            break;
                        case "F":
                            model.CustomerModel.Sex = "0";
                            break;
                        default:
                            model.CustomerModel.Sex = "";
                            break;
                    }

                    var dateOfBirth = _genericAttributeService.GetAttribute<DateTime?>(customer, NopCustomerDefaults.DateOfBirthAttribute);
                    if (dateOfBirth.HasValue)
                        model.CustomerModel.Birthday = dateOfBirth.Value.ToString("dd-MM-yyyy");

                    _httpContextAccessor.HttpContext?.Session?.Remove("ra_CustomerId");
                }
            }

            //category page
            if (controllerName.ToString().Equals("catalog", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.ToString().Equals("category", StringComparison.InvariantCultureIgnoreCase))
            {
                model.RenderSendCategoryFunc = true;

                if (routeData.Values.ContainsKey("categoryid") && int.TryParse(routeData.Values["categoryid"].ToString(), out var categoryId) && categoryId > 0)
                {
                    var category = _categoryService.GetCategoryById(categoryId);
                    if (category != null)
                    {
                        var categoryModel = new CategoryModel
                        {
                            CategoryId = category.Id,
                            CategoryName = JavaScriptEncoder.Default.Encode(category.Name ?? "")
                        };

                        var parentCategory = _categoryService.GetCategoryById(category.ParentCategoryId);
                        if (parentCategory != null)
                        {
                            categoryModel.ParentCategoryId = parentCategory.Id;
                            categoryModel.ParentCategoryName = JavaScriptEncoder.Default.Encode(parentCategory.Name ?? "");

                            var parentParentCategory = _categoryService.GetCategoryById(parentCategory.ParentCategoryId);
                            if (parentParentCategory != null)
                            {
                                categoryModel.ParentParentCategoryId = parentParentCategory.Id;
                                categoryModel.ParentParentCategoryName = JavaScriptEncoder.Default.Encode(parentParentCategory.Name ?? "");
                            }
                        }

                        model.CategoryModel = categoryModel;
                    }
                }
            }

            //manufacturer page
            if (controllerName.ToString().Equals("catalog", StringComparison.InvariantCultureIgnoreCase) &&
                    actionName.ToString().Equals("manufacturer", StringComparison.InvariantCultureIgnoreCase))
            {
                model.RenderSendBrandFunc = true;

                if (routeData.Values.ContainsKey("manufacturerid") &&
                    int.TryParse(routeData.Values["manufacturerid"].ToString(), out var manufacturerId) &&
                    manufacturerId > 0)
                {
                    var manufacturer = _manufacturerService.GetManufacturerById(manufacturerId);
                    if (manufacturer != null)
                    {
                        var manufacturerModel = new ManufacturerModel
                        {
                            ManufacturerId = manufacturer.Id,
                            ManufacturerName = JavaScriptEncoder.Default.Encode(manufacturer.Name ?? "")
                        };

                        model.ManufacturerModel = manufacturerModel;
                    }
                }
            }

            //product page
            if (controllerName.ToString().Equals("product", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.ToString().Equals("productdetails", StringComparison.InvariantCultureIgnoreCase))
            {
                model.RenderSendProductFunc = true;

                if (routeData.Values.ContainsKey("productid") &&
                    int.TryParse(routeData.Values["productid"].ToString(), out var productId) &&
                    productId > 0)
                {
                    var product = _productService.GetProductById(productId);
                    if (product != null)
                        model.ProductId = product.Id;
                }

                if (_mediaSettings.DefaultPictureZoomEnabled)
                    model.RenderClickImageFunc = true;
            }

            //order completed page
            if (controllerName.ToString().Equals("checkout", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.ToString().Equals("completed", StringComparison.InvariantCultureIgnoreCase))
            {
                Order order;
                if (routeData.Values.ContainsKey("orderid") &&
                    int.TryParse(routeData.Values["orderid"].ToString(), out var orderId) &&
                    orderId > 0)
                {
                    order = _orderService.GetOrderById(orderId);
                }
                else
                {
                    order = _orderService.SearchOrders(_storeContext.CurrentStore.Id,
                        customerId: _workContext.CurrentCustomer.Id, pageSize: 1)
                        .FirstOrDefault();
                }

                if (order != null && !order.Deleted && _workContext.CurrentCustomer.Id == order.CustomerId)
                {
                    model.RenderSendOrderFunc = true;
                    var orderModel = new OrderModel
                    {
                        Id = order.Id,
                        City = JavaScriptEncoder.Default.Encode(order.BillingAddress.City ?? ""),
                        Discount = order.OrderDiscount.ToString("0.00", CultureInfo.InvariantCulture),
                        Email = order.BillingAddress.Email,
                        FirstName = JavaScriptEncoder.Default.Encode(order.BillingAddress.FirstName ?? ""),
                        LastName = JavaScriptEncoder.Default.Encode(order.BillingAddress.LastName ?? ""),
                        Phone = JavaScriptEncoder.Default.Encode(order.BillingAddress.PhoneNumber ?? ""),
                        State =
                            order.BillingAddress.StateProvince != null
                                ? JavaScriptEncoder.Default.Encode(order.BillingAddress.StateProvince.Name ?? "")
                                : "",
                        Address =
                            JavaScriptEncoder.Default.Encode(order.BillingAddress.Address1 + " " +
                                                               order.BillingAddress.Address2),
                        Shipping =
                            order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax
                                ? order.OrderShippingInclTax.ToString("0.00", CultureInfo.InvariantCulture)
                                : order.OrderShippingExclTax.ToString("0.00", CultureInfo.InvariantCulture),
                        Total = order.OrderTotal.ToString("0.00", CultureInfo.InvariantCulture),
                        Rebates = 0,
                        Fees = 0
                    };

                    //discount codes
                    var discountsWithCouponCodes =
                        _discountService.GetAllDiscountUsageHistory(orderId: order.Id)
                            .Where(x => !string.IsNullOrEmpty(x.Discount.CouponCode))
                            .ToList();
                    for (var i = 0; i < discountsWithCouponCodes.Count; i++)
                    {
                        orderModel.DiscountCode += discountsWithCouponCodes[i].Discount.CouponCode;

                        if (i < discountsWithCouponCodes.Count - 1)
                            orderModel.DiscountCode += ", ";
                    }

                    var dateOfBirth = _genericAttributeService.GetAttribute<DateTime?>(order.Customer, NopCustomerDefaults.DateOfBirthAttribute);
                    if (dateOfBirth.HasValue)
                        orderModel.Birthday = dateOfBirth.Value.ToString("dd-mm-yyyy");

                    //order items
                    foreach (var orderItem in order.OrderItems)
                    {
                        var itemPrice = order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax
                                            ? orderItem.UnitPriceInclTax
                                            : orderItem.UnitPriceExclTax;
                        var item = new OrderItem()
                        {
                            Id = orderItem.ProductId,
                            Quantity = orderItem.Quantity,
                            Price = itemPrice.ToString("0.00", CultureInfo.InvariantCulture)
                        };

                        var values = _productAttributeParser.ParseProductAttributeValues(orderItem.AttributesXml);
                        for (var i = 0; i < values.Count; i++)
                        {
                            item.VariationCode += values[i].Id;
                            if (i < values.Count - 1)
                                item.VariationCode += "-";
                        }

                        orderModel.OrderItems.Add(item);
                    }

                    model.OrderModel = orderModel;
                }
            }

            //product review added page
            if (controllerName.ToString().Equals("product", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.ToString().Equals("productreviews", StringComparison.InvariantCultureIgnoreCase))
            {
                model.RenderCommentOnProductFunc = true;

                if (routeData.Values.ContainsKey("productid") &&
                    int.TryParse(routeData.Values["productid"].ToString(), out var productId))
                {
                    model.ProductId = productId;
                }
            }

            //help pages
            if (controllerName.ToString().Equals("topic", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.ToString().Equals("topicdetails", StringComparison.InvariantCultureIgnoreCase))
            {
                if (routeData.Values.ContainsKey("topicid") &&
                    int.TryParse(routeData.Values["topicid"].ToString(), out var topicId))
                {
                    var systemNames = retargetingSettings.HelpTopicSystemNames.Split(Convert.ToChar(",")).Select(s => s.Trim()).ToList();
                    var topic = _topicService.GetTopicById(topicId);
                    if (topic != null && systemNames.Contains(topic.SystemName))
                    {
                        model.RenderVisitHelpPageFunc = true;
                    }
                }
            }

            //home page
            if (controllerName.ToString().Equals("Home", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.ToString().Equals("index", StringComparison.InvariantCultureIgnoreCase))
            {
                model.RenderVisitHomePageFunc = true;
            }

            //checkout page
            if (controllerName.ToString().Equals("shoppingcart", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.ToString().Equals("cart", StringComparison.InvariantCultureIgnoreCase))
            {
                model.RenderCheckoutIdsFunc = true;

                var cart = _workContext.CurrentCustomer.ShoppingCartItems
                    .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                    .Where(item => item.StoreId == _storeContext.CurrentStore.Id)
                    .ToList();

                foreach (var cartItem in cart)
                {
                    model.CartItemIds += cartItem.ProductId;
                    model.CartItemIds += ",";
                }
            }

            //remove from cart
            var items = _httpContextAccessor.HttpContext?.Session?.Get<Dictionary<int, Dictionary<string, string>>>("ra_shoppingCartItemsToDelete");
            if (items == null)
                items = new Dictionary<int, Dictionary<string, string>>();

            model.CartItemsToDelete = items;
            _httpContextAccessor.HttpContext?.Session?.Remove("ra_shoppingCartItemsToDelete");

            //add to cart
            if (retargetingSettings.UseHttpPostInsteadOfAjaxInAddToCart)
            {
                model.AddToCartProductInfo = _httpContextAccessor.HttpContext?.Session?.Get("ra_addToCartProductInfo");
                _httpContextAccessor.HttpContext?.Session?.Remove("ra_addToCartProductInfo");
            }

            //page not found
            if (controllerName.ToString().Equals("Common", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.ToString().Equals("PageNotFound", StringComparison.InvariantCultureIgnoreCase))
            {
                model.RenderPageNotFoundFunc = true;
            }

            //send search term
            if (controllerName.ToString().Equals("Catalog", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.ToString().Equals("Search", StringComparison.InvariantCultureIgnoreCase))
            {
                model.RenderSendSearchTermFunc = true;
            }

            //thank you page
            if (controllerName.ToString().Equals("Checkout", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.ToString().Equals("Completed", StringComparison.InvariantCultureIgnoreCase))
            {
                model.RenderThankYouFunc = true;
            }

            return View("~/Plugins/Widgets.Retargeting/Views/PublicInfo.cshtml", model);
        }
    }
}
