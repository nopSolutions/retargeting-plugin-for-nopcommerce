using System.Web.Mvc;
using Autofac;
using Autofac.Core;
using Nop.Core.Caching;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Widgets.Retargeting.Controllers;
using Nop.Plugin.Widgets.Retargeting.Services;
using Nop.Services.Authentication;
using Nop.Services.Orders;

namespace Nop.Plugin.Widgets.Retargeting.Infrastructure
{
    /// <summary>
    /// Dependency registrar
    /// </summary>
    public class DependencyRegistrar : IDependencyRegistrar
    {
        /// <summary>
        /// Register services and interfaces
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="typeFinder">Type finder</param>
        /// <param name="config">Config</param>
        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            //we cache presentation models between requests
            builder.RegisterType<WidgetsRetargetingController>()
                .WithParameter(ResolvedParameter.ForNamed<ICacheManager>("nop_cache_static"));

            builder.RegisterType<CustomFormsAuthenticationService>().As<IAuthenticationService>().InstancePerLifetimeScope();
            builder.RegisterType<CustomOrderProcessingService>().As<IOrderProcessingService>().InstancePerLifetimeScope();
        }

        /// <summary>
        /// Order of this dependency registrar implementation
        /// </summary>
        public int Order
        {
            get { return 2; }
        }
    }
}
