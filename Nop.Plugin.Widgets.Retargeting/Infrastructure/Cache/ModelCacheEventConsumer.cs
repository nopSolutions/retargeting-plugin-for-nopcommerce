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
        IConsumer<EntityInsertedEvent<Language>>,
        IConsumer<EntityUpdatedEvent<Language>>,
        IConsumer<EntityDeletedEvent<Language>>,
        //Product picture mapping
        IConsumer<EntityInsertedEvent<ProductPicture>>,
        IConsumer<EntityUpdatedEvent<ProductPicture>>,
        IConsumer<EntityDeletedEvent<ProductPicture>>,
        //product categories
        IConsumer<EntityInsertedEvent<ProductCategory>>,
        IConsumer<EntityUpdatedEvent<ProductCategory>>,
        IConsumer<EntityDeletedEvent<ProductCategory>>,
        //categories
        IConsumer<EntityUpdatedEvent<Category>>,
        IConsumer<EntityDeletedEvent<Category>>
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
        public void HandleEvent(EntityInsertedEvent<Language> eventMessage)
        {
            _cacheManager.RemoveByPrefix(PRODUCT_BREADCRUMB_PATTERN_KEY);
        }
        public void HandleEvent(EntityUpdatedEvent<Language> eventMessage)
        {
            _cacheManager.RemoveByPrefix(PRODUCT_BREADCRUMB_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeletedEvent<Language> eventMessage)
        {
            _cacheManager.RemoveByPrefix(PRODUCT_BREADCRUMB_PATTERN_KEY);
        }

        //product picture mappings
        public void HandleEvent(EntityInsertedEvent<ProductPicture> eventMessage)
        {
            _cacheManager.RemoveByPrefix(string.Format(PRODUCT_PICTURES_PATTERN_KEY_BY_ID, eventMessage.Entity.ProductId));
        }
        public void HandleEvent(EntityUpdatedEvent<ProductPicture> eventMessage)
        {
            _cacheManager.RemoveByPrefix(string.Format(PRODUCT_PICTURES_PATTERN_KEY_BY_ID, eventMessage.Entity.ProductId));
        }
        public void HandleEvent(EntityDeletedEvent<ProductPicture> eventMessage)
        {
            _cacheManager.RemoveByPrefix(string.Format(PRODUCT_PICTURES_PATTERN_KEY_BY_ID, eventMessage.Entity.ProductId));
        }

        //product categories
        public void HandleEvent(EntityInsertedEvent<ProductCategory> eventMessage)
        {
            _cacheManager.RemoveByPrefix(string.Format(PRODUCT_BREADCRUMB_PATTERN_KEY_BY_ID, eventMessage.Entity.ProductId));
        }
        public void HandleEvent(EntityUpdatedEvent<ProductCategory> eventMessage)
        {
            _cacheManager.RemoveByPrefix(string.Format(PRODUCT_BREADCRUMB_PATTERN_KEY_BY_ID, eventMessage.Entity.ProductId));
        }
        public void HandleEvent(EntityDeletedEvent<ProductCategory> eventMessage)
        {
            _cacheManager.RemoveByPrefix(string.Format(PRODUCT_BREADCRUMB_PATTERN_KEY_BY_ID, eventMessage.Entity.ProductId));
        }

        //categories
        public void HandleEvent(EntityUpdatedEvent<Category> eventMessage)
        {
            _cacheManager.RemoveByPrefix(PRODUCT_BREADCRUMB_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeletedEvent<Category> eventMessage)
        {
            _cacheManager.RemoveByPrefix(PRODUCT_BREADCRUMB_PATTERN_KEY);
        }
    }
}