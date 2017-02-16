using System.Collections.Generic;
using System.Web.Mvc;

namespace Nop.Plugin.Widgets.Retargeting.Filters
{
    public class RetargetingFilterProvider : IFilterProvider
    {
        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            return new[]
            {
                new Filter(new RetargetingAddToCartFilterAttribute(), FilterScope.Action, null)
            };
        }
    }
}