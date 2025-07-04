using System.Web.Mvc;

namespace IIS.FTP.ManagementWeb
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new RequireHttpsAttribute());
            filters.Add(new AuthorizeAttribute()); // Require authentication for all actions by default
        }
    }
} 