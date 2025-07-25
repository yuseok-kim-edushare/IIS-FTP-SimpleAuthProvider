using System;

namespace IIS.FTP.ManagementWeb.Services
{
    public class SystemHealth
    {
        public bool IsHealthy { get; set; }
        public string UserStoreType { get; set; } = string.Empty;
        public bool UserStoreConnected { get; set; }
        public long AuthSuccessCount { get; set; }
        public long AuthFailureCount { get; set; }
        public DateTime LastChecked { get; set; }
    }
}