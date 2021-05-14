using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Core.Events;
using Nop.Services.Events;

namespace Nop.Plugin.Widgets.Retargeting.Infrastructure.Cache
{
    /// <summary>
    /// Model cache event consumer (used for caching of presentation layer models)
    /// </summary>
    public partial class ModelCacheEventConsumer :
        //Picture
        IConsumer<EntityUpdatedEvent<Picture>>,
        IConsumer<EntityDeletedEvent<Picture>>,
        //Product picture mapping
        IConsumer<EntityInsertedEvent<ProductPicture>>,
        IConsumer<EntityUpdatedEvent<ProductPicture>>,
        IConsumer<EntityDeletedEvent<ProductPicture>>
    {
        /// <summary>
        /// Key for product pictures caching
        /// </summary>
        /// <remarks>
        /// {0} : product id
        /// {2} : is connection secured
        /// {3} : current store ID
        /// </remarks>
        public static CacheKey ProductPicturesModelKey = new CacheKey("Nop.plugins.widgets.retargeting.product.pictures-{0}-{1}-{2}", ProductPicturesPrefixCacheKey, ProductPicturesPrefixCacheKeyById);
        public static string ProductPicturesPrefixCacheKey => "Nop.plugins.widgets.retargeting.product.pictures";
        public static string ProductPicturesPrefixCacheKeyById => "Nop.plugins.widgets.retargeting.product.pictures-{0}-";

        private readonly IStaticCacheManager _staticCacheManager;

        public ModelCacheEventConsumer(IStaticCacheManager staticCacheManager)
        {
            _staticCacheManager = staticCacheManager;
        }

        public void HandleEvent(EntityUpdatedEvent<Picture> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(ProductPicturesPrefixCacheKey);
        }

        /// <returns>A task that represents the asynchronous operation</returns>
        public void HandleEvent(EntityDeletedEvent<Picture> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(ProductPicturesPrefixCacheKey);
        }

        //product picture mappings
        public void HandleEvent(EntityInsertedEvent<ProductPicture> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(string.Format(ProductPicturesPrefixCacheKeyById, eventMessage.Entity.ProductId));
        }
        public void HandleEvent(EntityUpdatedEvent<ProductPicture> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(string.Format(ProductPicturesPrefixCacheKeyById, eventMessage.Entity.ProductId));
        }
        public void HandleEvent(EntityDeletedEvent<ProductPicture> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(string.Format(ProductPicturesPrefixCacheKeyById, eventMessage.Entity.ProductId));
        }
    }
}
