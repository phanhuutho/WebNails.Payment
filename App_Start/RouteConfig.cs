using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace WebNails.Payment
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Main Site",
                url: "{Domain}/{Controller}/{Action}/{Id}",
                defaults: new { Domain = "system.nail", Controller = "home", Action = "index", Id = UrlParameter.Optional }
            );
        }
    }
}
