using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.ExternalAuth.Weixin
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("Plugin.ExternalAuth.Weixin.Login",
                 "Plugins/ExternalAuthWeixin/Login",
                 new { controller = "ExternalAuthWeixin", action = "Login" },
                 new[] { "Nop.Plugin.ExternalAuth.Weixin.Controllers" }
            );

            routes.MapRoute("Plugin.ExternalAuth.Weixin.LoginCallback",
                 "Plugins/ExternalAuthWeixin/LoginCallback",
                 new { controller = "ExternalAuthWeixin", action = "LoginCallback" },
                 new[] { "Nop.Plugin.ExternalAuth.Weixin.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
