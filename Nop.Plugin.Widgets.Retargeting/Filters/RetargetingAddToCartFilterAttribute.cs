using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Services.Configuration;

namespace Nop.Plugin.Widgets.Retargeting.Filters
{
    public class RetargetingAddToCartFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var retargetingSettings = settingService.LoadSetting<RetargetingSettings>(storeContext.CurrentStore.Id);

            if (filterContext.HttpContext.Session != null &&
                !retargetingSettings.UseHttpPostInsteadOfAjaxInAddToCart &&
                filterContext.HttpContext.Session["ra_addToCartProductInfo"] != null)
            {
                var jsonResult = (filterContext.Result as JsonResult);
                if (jsonResult != null)
                {
                    var jsonData = ((JsonResult)filterContext.Result).Data;
                    var data = JObject.FromObject(jsonData);
                    data.Add("ra_addToCartProductInfo", JToken.FromObject(filterContext.HttpContext.Session["ra_addToCartProductInfo"]));

                    filterContext.HttpContext.Session["ra_addToCartProductInfo"] = null;

                    filterContext.Result = new ContentResult
                    {
                        Content = data.ToString(),
                        ContentType = "application/json"
                    };
                }
            }

            base.OnActionExecuted(filterContext);
        }
    }
}