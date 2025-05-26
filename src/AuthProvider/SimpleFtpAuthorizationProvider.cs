using System;
using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Logging;
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

        public SimpleFtpAuthorizationProvider() : this(UserStoreFactory.Create(), UserStoreFactory.GetAuditLogger())
        {
        }

        internal SimpleFtpAuthorizationProvider(IUserStore userStore, AuditLogger auditLogger)
        {
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        }

        public FtpAccess GetUserAccessPermission(
            string pszSessionId,
            string pszSiteName,
            string pszVirtualPath,
            string pszUserName)
        {
            try
            {
                FtpAccess allowedAccess = FtpAccess.None;
                var permissions = _userStore.GetPermissions(pszUserName);
                
                foreach (Permission entry in permissions)
                {
                    // Normalize paths for better matching
                    var normalizedVirtualPath = NormalizePath(pszVirtualPath);
                    var normalizedEntryPath = NormalizePath(entry.Path);
                    
                    if (!normalizedVirtualPath.StartsWith(normalizedEntryPath, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (entry.CanRead)
                        allowedAccess |= FtpAccess.Read;
                    if (entry.CanWrite)
                        allowedAccess |= FtpAccess.Write;
                }

                return allowedAccess;
            }
            catch (Exception ex)
            {
                _auditLogger.LogUserStoreError("GetUserAccessPermission", 
                    $"Error getting permissions for user '{pszUserName}': {ex.Message}");
                return FtpAccess.None;
            }
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