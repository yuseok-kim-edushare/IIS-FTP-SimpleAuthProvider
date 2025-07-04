using IIS.FTP.Core.Configuration;
using IIS.FTP.Core.Domain;
using IIS.FTP.Core.Logging;
using IIS.FTP.Core.Monitoring;
using IIS.FTP.Core.Security;
using IIS.FTP.Core.Stores;
using IIS.FTP.Core.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IIS.FTP.ManagementWeb.Services
{
    public interface IApplicationServices
    {
        Task<bool> ValidateUserAsync(string userId, string password);
        Task<User> GetUserAsync(string userId);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<bool> CreateUserAsync(User user, string password);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
        Task<IEnumerable<AuditEntry>> GetRecentAuditEntriesAsync(int count = 10);
        Task<SystemHealth> GetSystemHealthAsync();
    }

    public class ApplicationServices : IApplicationServices
    {
        private readonly IUserStore _userStore;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditLogger _auditLogger;
        private readonly IMetricsCollector _metricsCollector;
        private readonly AuthProviderConfig _config;
        private readonly UserManager _userManager;

        public ApplicationServices(
            IUserStore userStore,
            IPasswordHasher passwordHasher,
            IAuditLogger auditLogger,
            IMetricsCollector metricsCollector,
            AuthProviderConfig config)
        {
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
            _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            _userManager = new UserManager(userStore, passwordHasher);
        }

        public async Task<bool> ValidateUserAsync(string userId, string password)
        {
            try
            {
                var result = await _userStore.ValidateAsync(userId, password);
                
                if (result)
                {
                    await _auditLogger.LogAuthenticationAsync(userId, true, "Web UI login");
                    _metricsCollector.IncrementAuthSuccess();
                }
                else
                {
                    await _auditLogger.LogAuthenticationAsync(userId, false, "Web UI login failed");
                    _metricsCollector.IncrementAuthFailure();
                }

                return result;
            }
            catch (Exception ex)
            {
                await _auditLogger.LogErrorAsync($"Error validating user {userId}: {ex.Message}");
                _metricsCollector.IncrementAuthFailure();
                throw;
            }
        }

        public async Task<User> GetUserAsync(string userId)
        {
            return await _userStore.FindAsync(userId);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userStore.GetAllAsync();
        }

        public async Task<bool> CreateUserAsync(User user, string password)
        {
            try
            {
                var success = await _userManager.CreateUserAsync(
                    user.UserId,
                    user.DisplayName,
                    password,
                    user.HomeDirectory,
                    user.Permissions);

                if (success)
                {
                    await _auditLogger.LogUserManagementAsync("Admin", $"Created user: {user.UserId}");
                }

                return success;
            }
            catch (Exception ex)
            {
                await _auditLogger.LogErrorAsync($"Error creating user {user.UserId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                var success = await _userStore.UpdateAsync(user);

                if (success)
                {
                    await _auditLogger.LogUserManagementAsync("Admin", $"Updated user: {user.UserId}");
                }

                return success;
            }
            catch (Exception ex)
            {
                await _auditLogger.LogErrorAsync($"Error updating user {user.UserId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                var success = await _userStore.DeleteAsync(userId);

                if (success)
                {
                    await _auditLogger.LogUserManagementAsync("Admin", $"Deleted user: {userId}");
                }

                return success;
            }
            catch (Exception ex)
            {
                await _auditLogger.LogErrorAsync($"Error deleting user {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            try
            {
                // First validate current password
                var isValid = await _userStore.ValidateAsync(userId, currentPassword);
                if (!isValid)
                {
                    await _auditLogger.LogUserManagementAsync("Admin", $"Failed password change for user {userId}: Invalid current password");
                    return false;
                }

                // Update password
                var success = await _userManager.ChangePasswordAsync(userId, newPassword);

                if (success)
                {
                    await _auditLogger.LogUserManagementAsync("Admin", $"Changed password for user: {userId}");
                }

                return success;
            }
            catch (Exception ex)
            {
                await _auditLogger.LogErrorAsync($"Error changing password for user {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<AuditEntry>> GetRecentAuditEntriesAsync(int count = 10)
        {
            return await _auditLogger.GetRecentEntriesAsync(count);
        }

        public async Task<SystemHealth> GetSystemHealthAsync()
        {
            var metrics = _metricsCollector.GetMetrics();
            
            return new SystemHealth
            {
                IsHealthy = true, // TODO: Implement actual health checks
                UserStoreType = _config.UserStore.Type,
                UserStoreConnected = true, // TODO: Check actual connection
                AuthSuccessCount = metrics.GetValueOrDefault("auth_success_total", 0),
                AuthFailureCount = metrics.GetValueOrDefault("auth_failure_total", 0),
                LastChecked = DateTime.UtcNow
            };
        }
    }

    public class AuditEntry
    {
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public bool Success { get; set; }
    }

    public class SystemHealth
    {
        public bool IsHealthy { get; set; }
        public string UserStoreType { get; set; }
        public bool UserStoreConnected { get; set; }
        public long AuthSuccessCount { get; set; }
        public long AuthFailureCount { get; set; }
        public DateTime LastChecked { get; set; }
    }
} 