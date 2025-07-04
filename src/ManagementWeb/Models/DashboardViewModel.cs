using System;
using System.Collections.Generic;

namespace IIS.FTP.ManagementWeb.Models
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public DateTime LastUserCreated { get; set; }
        public List<RecentActivityViewModel> RecentActivities { get; set; } = new List<RecentActivityViewModel>();
        public SystemHealthViewModel SystemHealth { get; set; } = new SystemHealthViewModel();
    }

    public class RecentActivityViewModel
    {
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public bool Success { get; set; }
    }

    public class SystemHealthViewModel
    {
        public bool IsHealthy { get; set; }
        public string UserStoreType { get; set; }
        public bool UserStoreConnected { get; set; }
        public long AuthSuccessCount { get; set; }
        public long AuthFailureCount { get; set; }
        public DateTime LastChecked { get; set; }
    }
} 