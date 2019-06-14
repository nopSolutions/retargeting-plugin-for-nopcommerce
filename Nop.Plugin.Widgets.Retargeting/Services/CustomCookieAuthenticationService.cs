using Nop.Core.Domain.Customers;
using Nop.Services.Authentication;
using Nop.Services.Customers;
using Microsoft.AspNetCore.Http;

namespace Nop.Plugin.Widgets.Retargeting.Services
{
    public class CustomCookieAuthenticationService : CookieAuthenticationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomCookieAuthenticationService(CustomerSettings customerSettings, ICustomerService customerService, IHttpContextAccessor httpContextAccessor)
            : base(customerSettings, customerService, httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override void SignIn(Customer customer, bool createPersistentCookie)
        {
            base.SignIn(customer, createPersistentCookie);

            _httpContextAccessor.HttpContext?.Session?.SetInt32("ra_CustomerId", customer.Id);
        }
    }
}
