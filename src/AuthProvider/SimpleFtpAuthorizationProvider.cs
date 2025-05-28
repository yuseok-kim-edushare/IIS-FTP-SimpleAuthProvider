using System;
using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Logging;
using IIS.Ftp.SimpleAuth.Core.Monitoring;
using IIS.Ftp.SimpleAuth.Core.Stores;
using Microsoft.Web.FtpServer;

namespace IIS.Ftp.SimpleAuth.Provider
{
    /// <summary>
    /// Determines per-path read/write permissions for a user.
    /// </summary>
    public sealed class SimpleFtpAuthorizationProvider : IFtpAuthorizationProvider
    {
        private readonly IUserStore _userStore;
        private readonly AuditLogger _auditLogger;
        private readonly MetricsCollector? _metricsCollector;

        public SimpleFtpAuthorizationProvider() : this(
            UserStoreFactory.Create(),
            UserStoreFactory.GetAuditLogger(),
            UserStoreFactory.GetMetricsCollector())
        {
        }

        internal SimpleFtpAuthorizationProvider(IUserStore userStore, AuditLogger auditLogger, MetricsCollector? metricsCollector = null)
        {
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
            _metricsCollector = metricsCollector;
        }

        public FtpAccess GetUserAccessPermission(
            string pszSessionId,
            string pszSiteName,
            string pszVirtualPath,
            string pszUserName)
        {
            FtpAccess allowedAccess = FtpAccess.None;
            try
            {
                // Synchronize async call
                var permissions = _userStore.GetPermissionsAsync(pszUserName).GetAwaiter().GetResult();
                var normalizedVirtual = NormalizePath(pszVirtualPath);
                foreach (var entry in permissions)
                {
                    var normalizedEntry = NormalizePath(entry.Path);
                    if (!normalizedVirtual.StartsWith(normalizedEntry, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (entry.CanRead)
                        allowedAccess |= FtpAccess.Read;
                    if (entry.CanWrite)
                        allowedAccess |= FtpAccess.Write;
                }
            }
            catch (Exception ex)
            {
                _auditLogger.LogUserStoreError("GetUserAccessPermission", 
                    $"Error getting permissions for user '{pszUserName}': {ex.Message}");
                allowedAccess = FtpAccess.None;
            }
            finally
            {
                _metricsCollector?.RecordAuthorizationCheck(pszUserName, pszVirtualPath, allowedAccess != FtpAccess.None);
            }
            return allowedAccess;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "/";
            
            // Ensure path starts with /
            if (!path.StartsWith("/")) path = "/" + path;
            
            // Ensure path ends with / for directory matching
            if (!path.EndsWith("/")) path += "/";
            
            // Handle root path special case
            if (path == "//") path = "/";
            
            return path;
        }
    }
} 