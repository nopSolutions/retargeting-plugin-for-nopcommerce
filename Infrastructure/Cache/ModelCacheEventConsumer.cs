using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Localization;
using Nop.Core.Events;
using Nop.Core.Infrastructure;
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
        //manufacturers
        IConsumer<EntityUpdated<Manufacturer>>,
        IConsumer<EntityDeleted<Manufacturer>>,
        //product manufacturers
        IConsumer<EntityInserted<ProductManufacturer>>,
        IConsumer<EntityUpdated<ProductManufacturer>>,
        IConsumer<EntityDeleted<ProductManufacturer>>
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
        public const string PRODUCT_MANUFACTURERS_MODEL_KEY = "Nop.plugins.widgets.retargeting.product.manufacturers-{0}-{1}-{2}-{3}";
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
        public const string PRODUCT_CATEGORIES_MODEL_KEY = "Nop.plugins.widgets.retargeting.product.categories-{0}-{1}-{2}-{3}";
        public const string PRODUCT_CATEGORIES_PATTERN_KEY = "Nop.plugins.widgets.retargeting.categories.manufacturers";

        private readonly ICacheManager _cacheManager;

        public ModelCacheEventConsumer()
        {
            //TODO inject static cache manager using constructor
            this._cacheManager = EngineContext.Current.ContainerManager.Resolve<ICacheManager>("nop_cache_static");
        }

        //languages
        public void HandleEvent(EntityInserted<Language> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_MANUFACTURERS_PATTERN_KEY);
        }
        public void HandleEvent(EntityUpdated<Language> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_MANUFACTURERS_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeleted<Language> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_MANUFACTURERS_PATTERN_KEY);
        }

        //manufacturers
        public void HandleEvent(EntityUpdated<Manufacturer> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_MANUFACTURERS_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeleted<Manufacturer> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_MANUFACTURERS_PATTERN_KEY);
        }

        //product manufacturers
        public void HandleEvent(EntityInserted<ProductManufacturer> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_MANUFACTURERS_PATTERN_KEY);
        }
        public void HandleEvent(EntityUpdated<ProductManufacturer> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_MANUFACTURERS_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeleted<ProductManufacturer> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_MANUFACTURERS_PATTERN_KEY);
        }
    }
}
