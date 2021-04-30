using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Linq;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Services.Configuration;

namespace Nop.Plugin.Widgets.Retargeting.Filters
{
    public class RetargetingAddToCartFilterAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ActionExecutedContext actionExecutedContext = await next();

            var settingService = EngineContext.Current.Resolve<ISettingService>();
            var storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var retargetingSettings = await settingService.LoadSettingAsync<RetargetingSettings>((await storeContext.GetCurrentStoreAsync()).Id);

            if (actionExecutedContext.HttpContext.Session != null &&
                !retargetingSettings.UseHttpPostInsteadOfAjaxInAddToCart &&
                actionExecutedContext.HttpContext.Session.Get("ra_addToCartProductInfo") != null)
            {
                if (actionExecutedContext.Result is JsonResult jsonResult)
                {
                    var jsonData = jsonResult.Value;
                    var data = JObject.FromObject(jsonData);

                    var addToCartProductInfo = actionExecutedContext.HttpContext.Session.GetString("ra_addToCartProductInfo");
                    data.Add("ra_addToCartProductInfo", addToCartProductInfo);

                    actionExecutedContext.HttpContext.Session.Remove("ra_addToCartProductInfo");

                    actionExecutedContext.Result = new ContentResult
                    {
                        Content = data.ToString(),
                        ContentType = "application/json"
                    };
                }
            }
        }
    }
}