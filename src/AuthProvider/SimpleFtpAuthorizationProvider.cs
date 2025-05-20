using System;
using IIS.Ftp.SimpleAuth.Core.Domain;
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

        public SimpleFtpAuthorizationProvider() : this(UserStoreFactory.Create())
        {
        }

        internal SimpleFtpAuthorizationProvider(IUserStore userStore)
        {
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
        }

        public bool GetUserAccessPermission(
            string sessionId,
            string siteName,
            string virtualPath,
            string physicalPath,
            string userName,
            string userPassword,
            System.Security.Principal.SecurityIdentifier userSid,
            FTP_ACCESS requestedAccess,
            out FTP_ACCESS allowedAccess)
        {
            allowedAccess = 0;
            var permissions = _userStore.GetPermissionsAsync(userName).ConfigureAwait(false).GetAwaiter().GetResult();
            foreach (Permission entry in permissions)
            {
                if (!virtualPath.StartsWith(entry.Path, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (entry.CanRead)
                    allowedAccess |= FTP_ACCESS.FTP_ACCESS_READ;
                if (entry.CanWrite)
                    allowedAccess |= FTP_ACCESS.FTP_ACCESS_WRITE;
            }

            // Allow if we satisfied all requested bits.
            return (requestedAccess & allowedAccess) == requestedAccess;
        }
    }
} 