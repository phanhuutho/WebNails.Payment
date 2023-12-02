using System.Net;
using System.Web;
using System.Web.Mvc;

namespace WebNails.Payment
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }
    }
}
