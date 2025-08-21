using IIS.Ftp.SimpleAuth.Core.Configuration;
using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Logging;
using IIS.Ftp.SimpleAuth.Core.Monitoring;
using IIS.Ftp.SimpleAuth.Core.Security;
using IIS.Ftp.SimpleAuth.Core.Stores;
using IIS.Ftp.SimpleAuth.Core.Tools;
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
        Task<IEnumerable<IIS.Ftp.SimpleAuth.Core.Logging.AuditEntry>> GetRecentAuditEntriesAsync(int count = 10);
        Task<SystemHealth> GetSystemHealthAsync();
    }

    public class ApplicationServices : IApplicationServices
    {
        private readonly IUserStore _userStore;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditLogger _auditLogger;
        private readonly IMetricsCollector _metricsCollector;
        private readonly AuthProviderConfig _config;
        private readonly UserManagerService _userManager;

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
            
            _userManager = new UserManagerService(userStore, passwordHasher);
        }

        // Parameterless constructor for Unity IoC container fallback
        public ApplicationServices()
        {
            // This constructor is used when Unity IoC container fails to resolve dependencies
            // Note: All dependencies will be null in this case, so methods should handle it gracefully
        }

        public async Task<bool> ValidateUserAsync(string userId, string password)
        {
            // Debug logging to file
            try
            {
                System.IO.Directory.CreateDirectory(@"C:\temp");
                System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                    $"{DateTime.Now}: ApplicationServices.ValidateUserAsync called - UserId: {userId}, Dependencies: UserStore={_userStore != null}, AuditLogger={_auditLogger != null}, MetricsCollector={_metricsCollector != null}\n");
            }
            catch { }

            // Check if Unity IoC container failed to inject dependencies
            if (_userStore == null || _auditLogger == null || _metricsCollector == null)
            {
                try
                {
                    System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                        $"{DateTime.Now}: ApplicationServices dependencies not initialized - UserStore={_userStore != null}, AuditLogger={_auditLogger != null}, MetricsCollector={_metricsCollector != null}\n");
                }
                catch { }
                return false;
            }

            try
            {
                // First check if user exists
                try
                {
                    var user = await _userStore.FindAsync(userId);
                    System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                        $"{DateTime.Now}: UserStore.FindAsync({userId}) result: {(user != null ? "User found" : "User NOT found")}\n");
                    
                    if (user != null)
                    {
                        System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                            $"{DateTime.Now}: User details - UserId: {user.UserId}, DisplayName: {user.DisplayName}, PasswordHash length: {user.PasswordHash?.Length ?? 0}\n");
                    }
                }
                catch (Exception findEx)
                {
                    System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                        $"{DateTime.Now}: Exception in FindAsync: {findEx.Message}\n");
                }

                // Now validate password
                System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                    $"{DateTime.Now}: Calling UserStore.ValidateAsync with password length: {password?.Length ?? 0}\n");
                    
                var result = await _userStore.ValidateAsync(userId, password);
                
                System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                    $"{DateTime.Now}: UserStore.ValidateAsync result for {userId}: {result}\n");
                
                if (result)
                {
                    await _auditLogger.LogAuthenticationAsync(userId, true, "Web UI login");
                    _metricsCollector.IncrementAuthSuccess();
                    
                    System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                        $"{DateTime.Now}: Authentication successful, logged to audit\n");
                }
                else
                {
                    await _auditLogger.LogAuthenticationAsync(userId, false, "Web UI login failed");
                    _metricsCollector.IncrementAuthFailure();
                    
                    System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                        $"{DateTime.Now}: Authentication failed, logged to audit\n");
                }

                return result;
            }
            catch (Exception ex)
            {
                try
                {
                    System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                        $"{DateTime.Now}: Exception in ValidateUserAsync for {userId}: {ex.GetType().Name} - {ex.Message}\nStack: {ex.StackTrace}\n");
                }
                catch { }
                
                await _auditLogger.LogErrorAsync($"Error validating user {userId}: {ex.Message}");
                _metricsCollector.IncrementAuthFailure();
                throw;
            }
        }

        public async Task<User> GetUserAsync(string userId)
        {
            if (_userStore == null)
            {
                System.Diagnostics.Debug.WriteLine("UserStore not initialized - returning null");
                return null;
            }
            return await _userStore.FindAsync(userId);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            if (_userStore == null)
            {
                System.Diagnostics.Debug.WriteLine("UserStore not initialized - returning empty list");
                return Enumerable.Empty<User>();
            }
            return await _userStore.GetAllUsersAsync();
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
                await _userStore.SaveUserAsync(user);
                await _auditLogger.LogUserManagementAsync("Admin", $"Updated user: {user.UserId}");
                return true;
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
                await _userStore.DeleteUserAsync(userId);
                await _auditLogger.LogUserManagementAsync("Admin", $"Deleted user: {userId}");
                return true;
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

        public async Task<IEnumerable<IIS.Ftp.SimpleAuth.Core.Logging.AuditEntry>> GetRecentAuditEntriesAsync(int count = 10)
        {
            return await _auditLogger.GetRecentEntriesAsync(count);
        }

        public Task<SystemHealth> GetSystemHealthAsync()
        {
            var metrics = _metricsCollector.GetMetrics();
            
            var systemHealth = new SystemHealth
            {
                IsHealthy = true, // TODO: Implement actual health checks
                UserStoreType = _config.UserStore.Type,
                UserStoreConnected = true, // TODO: Check actual connection
                AuthSuccessCount = metrics.ContainsKey("ftp_auth_success_total") ? metrics["ftp_auth_success_total"] : 0,
                AuthFailureCount = metrics.ContainsKey("ftp_auth_failure_total") ? metrics["ftp_auth_failure_total"] : 0,
                LastChecked = DateTime.UtcNow
            };
            
            return Task.FromResult(systemHealth);
        }
    }
} 
