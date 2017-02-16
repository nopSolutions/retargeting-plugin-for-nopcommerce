using System.Collections.Generic;

namespace Nop.Plugin.Widgets.Retargeting.Models
{
    public class PublicInfoModel
    {
        public PublicInfoModel()
        {
            CategoryModel = new CategoryModel();
            ManufacturerModel = new ManufacturerModel();
            OrderModel = new OrderModel();
            CustomerModel = new CustomerModel();
            CartItemsToDelete = new Dictionary<int, Dictionary<string, string>>();
        }
        
        public string TrackingApiKey { get; set; }

        public int ProductId { get; set; }

        public string CartItemIds { get; set; }

        public Dictionary<int, Dictionary<string, string>> CartItemsToDelete { get; set; }

        public object AddToCartProductInfo { get; set; }

        public CustomerModel CustomerModel { get; set; }

        public CategoryModel CategoryModel { get; set; }

        public ManufacturerModel ManufacturerModel { get; set; }

        public OrderModel OrderModel { get; set; }

        #region Selectors

        public string AddToCartButtonIdDetailsPrefix { get; set; }

        public string ProductPriceLabelDetailsSelector { get; set; }

        public string AddToWishlistButtonIdDetailsPrefix { get; set; }

        public string AddToWishlistCatalogButtonSelector { get; set; }

        public string ProductReviewAddedResultSelector { get; set; }

        public string AddToCartCatalogButtonSelector { get; set; }

        public string ProductBoxSelector { get; set; }

        public string ProductMainPictureIdDetailsPrefix { get; set; }

        #endregion

        #region Render functions

        public bool RenderSetEmailFunc { get; set; }

        public bool RenderSendCategoryFunc { get; set; }

        public bool RenderSendBrandFunc { get; set; }

        public bool RenderSendProductFunc { get; set; }

        public bool RenderAddToCartFunc { get; set; }

        public bool RenderAddToWishlistFunc { get; set; }

        public bool RenderSendOrderFunc { get; set; }

        public bool RenderClickImageFunc { get; set; }

        public bool RenderCommentOnProductFunc { get; set; }

        public bool RenderVisitHelpPageFunc { get; set; }

        public bool RenderCheckoutIdsFunc { get; set; }

        #endregion
    }

    public class CustomerModel
    {
        public string Email { get; set; }

        public string Name { get; set; }

        public string Phone { get; set; }

        public string City { get; set; }

        public string Sex { get; set; }

        public string Birthday { get; set; }
    }

    public class CategoryModel
    {
        public int CategoryId { get; set; }

        public string CategoryName { get; set; }

        public int ParentCategoryId { get; set; }

        public string ParentCategoryName { get; set; }

        public int ParentParentCategoryId { get; set; }

        public string ParentParentCategoryName { get; set; }
    }

    public class ManufacturerModel
    {
        public int ManufacturerId { get; set; }

        public string ManufacturerName { get; set; }
    }

    public class OrderModel
    {
        public OrderModel()
        {
            OrderItems = new List<OrderItem>();
        }

        public int Id { get; set; }

        public string LastName { get; set; }

        public string FirstName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string State { get; set; }

        public string City { get; set; }

        public string Address { get; set; }

        public string Birthday { get; set; }

        public string DiscountCode { get; set; }

        public decimal Discount { get; set; }

        public decimal Shipping { get; set; }

        public int Rebates { get; set; }

        public int Fees { get; set; }

        public decimal Total { get; set; }

        public List<OrderItem> OrderItems { get; set; }
    }

    public class OrderItem
    {
        public int Id { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public string VariationCode { get; set; }
    }
}