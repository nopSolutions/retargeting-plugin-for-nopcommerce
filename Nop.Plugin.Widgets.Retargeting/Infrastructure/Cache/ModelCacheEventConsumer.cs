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
        //manufacturers
        IConsumer<EntityUpdatedEvent<Manufacturer>>,
        IConsumer<EntityDeletedEvent<Manufacturer>>,
        //product manufacturers
        IConsumer<EntityInsertedEvent<ProductManufacturer>>,
        IConsumer<EntityUpdatedEvent<ProductManufacturer>>,
        IConsumer<EntityDeletedEvent<ProductManufacturer>>
    {
        /// <summary>
        /// Key for ProductManufacturers model caching
        /// </summary>
        /// <remarks>
        /// {0} : product id
        /// {1} : language id
        /// {2} : roles of the current user
        /// {3} : current store ID
        /// </remarks>
        public static CacheKey PRODUCT_MANUFACTURERS_MODEL_KEY = new CacheKey("Nop.plugins.widgets.retargeting.product.manufacturers-{0}-{1}-{2}-{3}");
        public const string PRODUCT_MANUFACTURERS_PATTERN_KEY = "Nop.plugins.widgets.retargeting.product.manufacturers";

        /// <summary>
        /// Key for ProductManufacturers model caching
        /// </summary>
        /// <remarks>
        /// {0} : product id
        /// {1} : language id
        /// {2} : roles of the current user
        /// {3} : current store ID
        /// </remarks>
        public static CacheKey PRODUCT_CATEGORIES_MODEL_KEY = new CacheKey("Nop.plugins.widgets.retargeting.product.categories-{0}-{1}-{2}-{3}");
        public const string PRODUCT_CATEGORIES_PATTERN_KEY = "Nop.plugins.widgets.retargeting.categories.manufacturers";

        private readonly IStaticCacheManager _cacheManager;

        public ModelCacheEventConsumer(IStaticCacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        //languages
        public void HandleEvent(EntityInsertedEvent<Language> eventMessage)
        {
            _cacheManager.RemoveByPrefix(PRODUCT_MANUFACTURERS_PATTERN_KEY);
        }
        public void HandleEvent(EntityUpdatedEvent<Language> eventMessage)
        {
            _cacheManager.RemoveByPrefix(PRODUCT_MANUFACTURERS_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeletedEvent<Language> eventMessage)
        {
            _cacheManager.RemoveByPrefix(PRODUCT_MANUFACTURERS_PATTERN_KEY);
        }

        //manufacturers
        public void HandleEvent(EntityUpdatedEvent<Manufacturer> eventMessage)
        {
            _cacheManager.RemoveByPrefix(PRODUCT_MANUFACTURERS_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeletedEvent<Manufacturer> eventMessage)
        {
            _cacheManager.RemoveByPrefix(PRODUCT_MANUFACTURERS_PATTERN_KEY);
        }

        //product manufacturers
        public void HandleEvent(EntityInsertedEvent<ProductManufacturer> eventMessage)
        {
            _cacheManager.RemoveByPrefix(PRODUCT_MANUFACTURERS_PATTERN_KEY);
        }
        public void HandleEvent(EntityUpdatedEvent<ProductManufacturer> eventMessage)
        {
            _cacheManager.RemoveByPrefix(PRODUCT_MANUFACTURERS_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeletedEvent<ProductManufacturer> eventMessage)
        {
            _cacheManager.RemoveByPrefix(PRODUCT_MANUFACTURERS_PATTERN_KEY);
        }
    }
}
