namespace Nop.Plugin.Widgets.Retargeting
{
    public static class RetargetingDefaults
    {
        /// <summary>
        /// Retargeting system name
        /// </summary>
        public static string SystemName => "Widgets.Retargeting";

        /// <summary>
        /// Name of the view component to display widget in public store
        /// </summary>
        public const string RETARGETING_VIEW_COMPONENT_NAME = "WidgetsRetargeting";

        /// <summary>
        /// Generic attribute name to hide configuration settings block on the plugin configuration page
        /// </summary>
        public static string HideConfigurationBlock = "RetargetingPage.HideConfigurationBlock";

        /// <summary>
        /// Generic attribute name to hide preconfiguration system block on the plugin configuration page
        /// </summary>
        public static string HidePreconfigureBlock = "RetargetingPage.HidePreconfigureBlock";

        public static string RecommendationHomePage => "retargeting-recommeng-home-page";

        public static string RecommendationCategoryPage => "retargeting-recommeng-category-page";

        public static string RecommendationProductPage => "retargeting-recommeng-product-page";

        public static string RecommendationCheckoutPage => "retargeting-recommeng-checkout-page";

        public static string RecommendationThankYouPage => "retargeting-recommeng-thank-you-page";

        public static string RecommendationOutOfStockPage => "retargeting-recommeng-out-of-stock-page";

        public static string RecommendationSearchPage => "retargeting-recommeng-search-page";

        public static string RecommendationPageNotFound => "retargeting-recommeng-not-found-page";

        /// <summary>
        /// User agent using for requesting Retargeting services
        /// </summary>
        public static string UserAgent => "nopCommerce-plugin";

        /// <summary>
        /// Subscription email
        /// </summary>
        public static string SubscriptionEmail => "info@retargeting.biz";

        public static string AddToCartButtonIdDetailsPrefix => "add-to-cart-button-";
        public static string ProductPriceLabelDetailsSelector => ".prices";
        public static string AddToWishlistButtonIdDetailsPrefix => "add-to-wishlist-button-";
        public static string HelpTopicSystemNames => "ShippingInfo,PrivacyInfo,ConditionsOfUse,AboutUs";
        public static string AddToWishlistCatalogButtonSelector => ".add-to-wishlist-button";
        public static string ProductReviewAddedResultSelector => "div.result";
        public static string AddToCartCatalogButtonSelector => ".product-box-add-to-cart-button";
        public static string ProductBoxSelector => ".product-item";
        public static string ProductMainPictureIdDetailsPrefix => "main-product-img-lightbox-anchor-";
    }
}

