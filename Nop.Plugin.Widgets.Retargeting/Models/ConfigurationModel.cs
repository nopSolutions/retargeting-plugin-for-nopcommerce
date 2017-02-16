using Nop.Web.Framework;

namespace Nop.Plugin.Widgets.Retargeting.Models
{
    public class ConfigurationModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.TrackingApiKey")]
        public string TrackingApiKey { get; set; }
        public bool TrackingApiKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.RestApiKey")]
        public string RestApiKey { get; set; }
        public bool RestApiKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Widgets.Retargeting.UseHttpPostInsteadOfAjaxInAddToCart")]
        public bool UseHttpPostInsteadOfAjaxInAddToCart { get; set; }
        public bool UseHttpPostInsteadOfAjaxInAddToCart_OverrideForStore { get; set; }

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
    }
}