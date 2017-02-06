using System.Web;
using Nop.Core.Domain.Customers;
using Nop.Services.Authentication;
using Nop.Services.Customers;

namespace Nop.Plugin.Widgets.Retargeting.Services
{
    public class CustomFormsAuthenticationService: FormsAuthenticationService
    {
        private readonly HttpContextBase _httpContext;

        public CustomFormsAuthenticationService(HttpContextBase httpContext, ICustomerService customerService, CustomerSettings customerSettings) 
            : base(httpContext, customerService, customerSettings)
        {
            this._httpContext = httpContext;
        }

        public override void SignIn(Customer customer, bool createPersistentCookie)
        {
            base.SignIn(customer, createPersistentCookie);

            if (_httpContext.Session != null)
                _httpContext.Session["ra_Customer"] = customer;
        }
    }
}
