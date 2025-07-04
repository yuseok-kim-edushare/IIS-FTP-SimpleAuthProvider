using System.Web.Mvc;
using System.Web.Routing;

namespace IIS.FTP.ManagementWeb
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Health and metrics endpoints
            routes.MapRoute(
                name: "Health",
                url: "healthz",
                defaults: new { controller = "Health", action = "Index" }
            );

            routes.MapRoute(
                name: "Metrics",
                url: "metrics",
                defaults: new { controller = "Health", action = "Metrics" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Dashboard", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
} 