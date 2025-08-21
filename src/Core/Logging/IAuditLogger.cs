using System.Collections.Generic;
using System.Threading.Tasks;

namespace IIS.Ftp.SimpleAuth.Core.Logging
{
    /// <summary>
    /// Interface for audit logging functionality.
    /// </summary>
    public interface IAuditLogger
    {
        /// <summary>
        /// Logs a successful authentication event.
        /// </summary>
        Task LogAuthenticationAsync(string userId, bool success, string details);

        /// <summary>
        /// Logs a user management event.
        /// </summary>
        Task LogUserManagementAsync(string adminUser, string action);

        /// <summary>
        /// Logs an error event.
        /// </summary>
        Task LogErrorAsync(string message);

        /// <summary>
        /// Gets recent audit entries.
        /// </summary>
        Task<IEnumerable<AuditEntry>> GetRecentEntriesAsync(int count);
    }

    /// <summary>
    /// Represents an audit log entry.
    /// </summary>
    public class AuditEntry
    {
        public System.DateTime Timestamp { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public bool Success { get; set; }
    }
}