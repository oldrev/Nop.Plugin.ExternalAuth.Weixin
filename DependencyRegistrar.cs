using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.ExternalAuth.Weixin.Core;
using Nop.Services.Authentication.External;
using Nop.Services.Customers;

namespace Nop.Plugin.ExternalAuth.Weixin
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
            builder.RegisterType<WeixinProviderAuthorizer>().As<IOAuthProviderWeixinAuthorizer>().InstancePerLifetimeScope();

            builder.RegisterType<Nop.Plugin.ExternalAuth.Weixin.Authentication.ExternalAuthorizer>().As<IExternalAuthorizer>().InstancePerLifetimeScope();
            builder.RegisterType<Nop.Plugin.ExternalAuth.Weixin.Services.CustomerRegistrationService>().As<ICustomerRegistrationService>().InstancePerLifetimeScope();
            builder.RegisterType<Nop.Plugin.ExternalAuth.Weixin.Authentication.External.OpenAuthenticationService>().As<IOpenAuthenticationService>().InstancePerLifetimeScope();

        }

        /// <summary>
        /// Order of this dependency registrar implementation
        /// </summary>
        public int Order
        {
            get { return 1; }
        }
    }
}
