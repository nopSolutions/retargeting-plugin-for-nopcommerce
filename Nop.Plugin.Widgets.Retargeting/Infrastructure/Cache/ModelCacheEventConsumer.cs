using System.Threading.Tasks;
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
        public static CacheKey ProductPicturesModelKey => new("Nop.plugins.widgets.retargeting.product.pictures-{0}-{1}-{2}", ProductPicturesPrefixCacheKey, ProductPicturesPrefixCacheKeyById);
        public static string ProductPicturesPrefixCacheKey => "Nop.plugins.widgets.retargeting.product.pictures";
        public static string ProductPicturesPrefixCacheKeyById => "Nop.plugins.widgets.retargeting.product.pictures-{0}-";

        private readonly IStaticCacheManager _staticCacheManager;

        public ModelCacheEventConsumer(IStaticCacheManager staticCacheManager)
        {
            _staticCacheManager = staticCacheManager;
        }

        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task HandleEventAsync(EntityUpdatedEvent<Picture> eventMessage)
        {
            await _staticCacheManager.RemoveByPrefixAsync(ProductPicturesPrefixCacheKey);
        }

        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task HandleEventAsync(EntityDeletedEvent<Picture> eventMessage)
        {
            await _staticCacheManager.RemoveByPrefixAsync(ProductPicturesPrefixCacheKey);
        }

        //product picture mappings
        public async Task HandleEventAsync(EntityInsertedEvent<ProductPicture> eventMessage)
        {
            await _staticCacheManager.RemoveByPrefixAsync(string.Format(ProductPicturesPrefixCacheKeyById, eventMessage.Entity.ProductId));
        }
        public async Task HandleEventAsync(EntityUpdatedEvent<ProductPicture> eventMessage)
        {
            await _staticCacheManager.RemoveByPrefixAsync(string.Format(ProductPicturesPrefixCacheKeyById, eventMessage.Entity.ProductId));
        }
        public async Task HandleEventAsync(EntityDeletedEvent<ProductPicture> eventMessage)
        {
            await _staticCacheManager.RemoveByPrefixAsync(string.Format(ProductPicturesPrefixCacheKeyById, eventMessage.Entity.ProductId));
        }
    }
}
