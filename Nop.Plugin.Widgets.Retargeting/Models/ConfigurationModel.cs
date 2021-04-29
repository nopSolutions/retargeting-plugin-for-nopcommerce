using Nop.Web.Framework.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Widgets.Retargeting.Models
{
    /// <summary>
    /// Represents plugin configuration model
    /// </summary>
    public record ConfigurationModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.MerchantEmail")]
        [DataType(DataType.EmailAddress)]
        public string MerchantEmail { get; set; }
        public bool MerchantEmail_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.TrackingApiKey")]
        public string TrackingApiKey { get; set; }
        public bool TrackingApiKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.RestApiKey")]
        public string RestApiKey { get; set; }
        public bool RestApiKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.UseHttpPostInsteadOfAjaxInAddToCart")]
        public bool UseHttpPostInsteadOfAjaxInAddToCart { get; set; }
        public bool UseHttpPostInsteadOfAjaxInAddToCart_OverrideForStore { get; set; }

        public bool HideConfigurationBlock { get; set; }
        public bool HidePreconfigureBlock { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.AddToCartButtonIdDetailsPrefix")]
        public string AddToCartButtonIdDetailsPrefix { get; set; }
        public bool AddToCartButtonDetailsPrefix_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.ProductPriceLabelDetailsSelector")]
        public string ProductPriceLabelDetailsSelector { get; set; }
        public bool PriceLabelSelector_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.AddToWishlistButtonIdDetailsPrefix")]
        public string AddToWishlistButtonIdDetailsPrefix { get; set; }
        public bool AddToWishlistButtonIdDetailsPrefix_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.HelpTopicSystemNames")]
        public string HelpTopicSystemNames { get; set; }
        public bool HelpTopicSystemNames_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.AddToWishlistCatalogButtonSelector")]
        public string AddToWishlistCatalogButtonSelector { get; set; }
        public bool AddToWishlistCatalogButtonSelector_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.ProductReviewAddedResultSelector")]
        public string ProductReviewAddedResultSelector { get; set; }
        public bool ProductReviewAddedResultSelector_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.AddToCartCatalogButtonSelector")]
        public string AddToCartCatalogButtonSelector { get; set; }
        public bool AddToCartCatalogButtonSelector_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.ProductBoxSelector")]
        public string ProductBoxSelector { get; set; }
        public bool ProductBoxSelector_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.ProductMainPictureIdDetailsPrefix")]
        public string ProductMainPictureIdDetailsPrefix { get; set; }
        public bool ProductMainPictureIdDetailsPrefix_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.RecommendationHomePage")]
        public bool RecommendationHomePage { get; set; }
        public bool RecommendationHomePage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.RecommendationCategoryPage")]
        public bool RecommendationCategoryPage { get; set; }
        public bool RecommendationCategoryPage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.RecommendationProductPage")]
        public bool RecommendationProductPage { get; set; }
        public bool RecommendationProductPage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.RecommendationCheckoutPage")]
        public bool RecommendationCheckoutPage { get; set; }
        public bool RecommendationCheckoutPage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.RecommendationThankYouPage")]
        public bool RecommendationThankYouPage { get; set; }
        public bool RecommendationThankYouPage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.RecommendationOutOfStockPage")]
        public bool RecommendationOutOfStockPage { get; set; }
        public bool RecommendationOutOfStockPage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.RecommendationSearchPage")]
        public bool RecommendationSearchPage { get; set; }
        public bool RecommendationSearchPage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.RecommendationPageNotFound")]
        public bool RecommendationPageNotFound { get; set; }
        public bool RecommendationPageNotFound_OverrideForStore { get; set; }
    }
}