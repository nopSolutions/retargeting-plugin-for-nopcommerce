using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Localization;
using Nop.Core.Events;
using Nop.Services.Events;

namespace Nop.Plugin.Widgets.Retargeting.Infrastructure.Cache
{
    /// <summary>
    /// Model cache event consumer (used for caching of presentation layer models)
    /// </summary>
    public partial class ModelCacheEventConsumer :
        //languages
        IConsumer<EntityInserted<Language>>,
        IConsumer<EntityUpdated<Language>>,
        IConsumer<EntityDeleted<Language>>,
        //Product picture mapping
        IConsumer<EntityInserted<ProductPicture>>,
        IConsumer<EntityUpdated<ProductPicture>>,
        IConsumer<EntityDeleted<ProductPicture>>,
        //product categories
        IConsumer<EntityInserted<ProductCategory>>,
        IConsumer<EntityUpdated<ProductCategory>>,
        IConsumer<EntityDeleted<ProductCategory>>,
        //categories
        IConsumer<EntityUpdated<Category>>,
        IConsumer<EntityDeleted<Category>>
    {
        /// <summary>
        /// Key for product pictures caching
        /// </summary>
        /// <remarks>
        /// {0} : product id
        /// {2} : is connection secured
        /// {3} : current store ID
        /// </remarks>
        public const string PRODUCT_PICTURES_MODEL_KEY = "Nop.plugins.widgets.retargeting.product.pictures-{0}-{1}-{2}";
        public const string PRODUCT_PICTURES_PATTERN_KEY_BY_ID = "Nop.plugins.widgets.retargeting.product.pictures-{0}-";

        /// <summary>
        /// Key for ProductBreadcrumbModel caching
        /// </summary>
        /// <remarks>
        /// {0} : product id
        /// {1} : language id
        /// {2} : comma separated list of customer roles
        /// {3} : current store ID
        /// </remarks>
        public const string PRODUCT_BREADCRUMB_MODEL_KEY = "Nop.plugins.widgets.retargeting.product.breadcrumb-{0}-{1}-{2}-{3}";
        public const string PRODUCT_BREADCRUMB_PATTERN_KEY = "Nop.plugins.widgets.retargeting.product.breadcrumb";
        public const string PRODUCT_BREADCRUMB_PATTERN_KEY_BY_ID = "Nop.plugins.widgets.retargeting.product.breadcrumb-{0}-";

        private readonly IStaticCacheManager _cacheManager;

        public ModelCacheEventConsumer(IStaticCacheManager cacheManager)
        {
            this._cacheManager = cacheManager;
        }

        //languages
        public void HandleEvent(EntityInserted<Language> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_BREADCRUMB_PATTERN_KEY);
        }
        public void HandleEvent(EntityUpdated<Language> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_BREADCRUMB_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeleted<Language> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_BREADCRUMB_PATTERN_KEY);
        }

        //product picture mappings
        public void HandleEvent(EntityInserted<ProductPicture> eventMessage)
        {
            _cacheManager.RemoveByPattern(string.Format(PRODUCT_PICTURES_PATTERN_KEY_BY_ID, eventMessage.Entity.ProductId));
        }
        public void HandleEvent(EntityUpdated<ProductPicture> eventMessage)
        {
            _cacheManager.RemoveByPattern(string.Format(PRODUCT_PICTURES_PATTERN_KEY_BY_ID, eventMessage.Entity.ProductId));
        }
        public void HandleEvent(EntityDeleted<ProductPicture> eventMessage)
        {
            _cacheManager.RemoveByPattern(string.Format(PRODUCT_PICTURES_PATTERN_KEY_BY_ID, eventMessage.Entity.ProductId));
        }

        //product categories
        public void HandleEvent(EntityInserted<ProductCategory> eventMessage)
        {
            _cacheManager.RemoveByPattern(string.Format(PRODUCT_BREADCRUMB_PATTERN_KEY_BY_ID, eventMessage.Entity.ProductId));
        }
        public void HandleEvent(EntityUpdated<ProductCategory> eventMessage)
        {
            _cacheManager.RemoveByPattern(string.Format(PRODUCT_BREADCRUMB_PATTERN_KEY_BY_ID, eventMessage.Entity.ProductId));
        }
        public void HandleEvent(EntityDeleted<ProductCategory> eventMessage)
        {
            _cacheManager.RemoveByPattern(string.Format(PRODUCT_BREADCRUMB_PATTERN_KEY_BY_ID, eventMessage.Entity.ProductId));
        }

        //categories
        public void HandleEvent(EntityUpdated<Category> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_BREADCRUMB_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeleted<Category> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_BREADCRUMB_PATTERN_KEY);
        }
    }
}
