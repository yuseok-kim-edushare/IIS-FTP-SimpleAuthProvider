using IIS.FTP.ManagementWeb.Services;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace IIS.FTP.ManagementWeb.Controllers
{
    [AllowAnonymous]
    public class HealthController : Controller
    {
        private readonly IApplicationServices _applicationServices;

        public HealthController(IApplicationServices applicationServices)
        {
            _applicationServices = applicationServices ?? throw new ArgumentNullException(nameof(applicationServices));
        }

        // GET: /healthz
        public async Task<ActionResult> Index()
        {
            try
            {
                var health = await _applicationServices.GetSystemHealthAsync();

                var result = new
                {
                    status = health.IsHealthy ? "healthy" : "unhealthy",
                    timestamp = DateTime.UtcNow,
                    checks = new
                    {
                        userStore = new
                        {
                            status = health.UserStoreConnected ? "healthy" : "unhealthy",
                            type = health.UserStoreType
                        }
                    }
                };

                Response.ContentType = "application/json";
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 503; // Service Unavailable
                var errorResult = new
                {
                    status = "unhealthy",
                    timestamp = DateTime.UtcNow,
                    error = ex.Message
                };

                Response.ContentType = "application/json";
                return Json(errorResult, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: /metrics
        public async Task<ActionResult> Metrics()
        {
            try
            {
                var health = await _applicationServices.GetSystemHealthAsync();

                var sb = new StringBuilder();
                sb.AppendLine("# HELP auth_success_total Total number of successful authentications");
                sb.AppendLine("# TYPE auth_success_total counter");
                sb.AppendLine($"auth_success_total {health.AuthSuccessCount}");
                sb.AppendLine();
                sb.AppendLine("# HELP auth_failure_total Total number of failed authentications");
                sb.AppendLine("# TYPE auth_failure_total counter");
                sb.AppendLine($"auth_failure_total {health.AuthFailureCount}");

                Response.ContentType = "text/plain; version=0.0.4";
                return Content(sb.ToString());
            }
            catch (Exception ex)
            {
                Response.StatusCode = 503;
                return Content($"# Error retrieving metrics: {ex.Message}");
            }
        }
    }
} 