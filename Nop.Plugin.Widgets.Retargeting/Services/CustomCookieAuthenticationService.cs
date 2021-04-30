using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Customers;
using Nop.Services.Authentication;
using Nop.Services.Customers;

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

        public override async Task SignInAsync(Customer customer, bool createPersistentCookie)
        {
            await base.SignInAsync(customer, createPersistentCookie);

            _httpContextAccessor.HttpContext?.Session?.SetInt32("ra_CustomerId", customer.Id);
        }
    }
}
