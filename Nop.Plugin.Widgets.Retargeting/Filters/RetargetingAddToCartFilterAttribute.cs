using Newtonsoft.Json.Linq;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Services.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

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
                filterContext.HttpContext.Session.Get("ra_addToCartProductInfo") != null)
            {
                var jsonResult = (filterContext.Result as JsonResult);
                if (jsonResult != null)
                {
                    var jsonData = ((JsonResult)filterContext.Result).Value;
                    var data = JObject.FromObject(jsonData);

                    var addToCartProductInfo = filterContext.HttpContext.Session.GetString("ra_addToCartProductInfo");
                    data.Add("ra_addToCartProductInfo", addToCartProductInfo);

                    filterContext.HttpContext.Session.Remove("ra_addToCartProductInfo");

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