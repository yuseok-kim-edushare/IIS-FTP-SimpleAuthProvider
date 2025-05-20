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

        public FtpAccess GetUserAccessPermission(
            string pszSessionId,
            string pszSiteName,
            string pszVirtualPath,
            string pszUserName)
        {
            FtpAccess allowedAccess = FtpAccess.None;
            var permissions = _userStore.GetPermissionsAsync(pszUserName).ConfigureAwait(false).GetAwaiter().GetResult();
            foreach (Permission entry in permissions)
            {
                if (!pszVirtualPath.StartsWith(entry.Path, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (entry.CanRead)
                    allowedAccess |= FtpAccess.Read;
                if (entry.CanWrite)
                    allowedAccess |= FtpAccess.Write;
            }

            return allowedAccess;
        }
    }
} 