using Nop.Core.Configuration;

namespace Nop.Plugin.Widgets.Retargeting
{
    public class RetargetingSettings : ISettings
    {
        public string TrackingApiKey { get; set; }

        public string RestApiKey { get; set; }

        public bool UseHttpPostInsteadOfAjaxInAddToCart { get; set; }

        public string ProductBoxSelector { get; set; }

        public string AddToCartCatalogButtonSelector { get; set; }

        public string AddToCartButtonIdDetailsPrefix { get; set; }

        public string AddToWishlistCatalogButtonSelector { get; set; }

        public string AddToWishlistButtonIdDetailsPrefix { get; set; }

        public string ProductPriceLabelDetailsSelector { get; set; }

        public string ProductMainPictureIdDetailsPrefix { get; set; }

        public string HelpTopicSystemNames { get; set; }

        public string ProductReviewAddedResultSelector { get; set; }

        public string MerchantEmail { get; set; }

        public bool RecommendationHomePage { get; set; }

        public bool RecommendationCategoryPage { get; set; }

        public bool RecommendationProductPage { get; set; }

        public bool RecommendationCheckoutPage { get; set; }

        public bool RecommendationThankYouPage { get; set; }

        public bool RecommendationOutOfStockPage { get; set; }

        public bool RecommendationSearchPage { get; set; }

        public bool RecommendationPageNotFound { get; set; }

    }
}