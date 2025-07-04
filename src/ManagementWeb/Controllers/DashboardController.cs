using IIS.FTP.ManagementWeb.Models;
using IIS.FTP.ManagementWeb.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace IIS.FTP.ManagementWeb.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IApplicationServices _applicationServices;

        public DashboardController(IApplicationServices applicationServices)
        {
            _applicationServices = applicationServices ?? throw new ArgumentNullException(nameof(applicationServices));
        }

        // GET: /Dashboard
        public async Task<ActionResult> Index()
        {
            var users = await _applicationServices.GetAllUsersAsync();
            var recentActivities = await _applicationServices.GetRecentAuditEntriesAsync(10);
            var systemHealth = await _applicationServices.GetSystemHealthAsync();

            var model = new DashboardViewModel
            {
                TotalUsers = users.Count(),
                ActiveUsers = users.Count(), // TODO: Implement actual active user count
                LastUserCreated = DateTime.UtcNow, // TODO: Get from audit log
                RecentActivities = recentActivities.Select(a => new RecentActivityViewModel
                {
                    Timestamp = a.Timestamp,
                    UserId = a.UserId,
                    Action = a.Action,
                    Details = a.Details,
                    Success = a.Success
                }).ToList(),
                SystemHealth = new SystemHealthViewModel
                {
                    IsHealthy = systemHealth.IsHealthy,
                    UserStoreType = systemHealth.UserStoreType,
                    UserStoreConnected = systemHealth.UserStoreConnected,
                    AuthSuccessCount = systemHealth.AuthSuccessCount,
                    AuthFailureCount = systemHealth.AuthFailureCount,
                    LastChecked = systemHealth.LastChecked
                }
            };

            return View(model);
        }
    }
} 