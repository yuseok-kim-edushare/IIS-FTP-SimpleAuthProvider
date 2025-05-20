using System;
using IIS.Ftp.SimpleAuth.Core.Stores;
using Microsoft.Web.FtpServer;

namespace IIS.Ftp.SimpleAuth.Provider
{
    /// <summary>
    /// IIS FTP authentication provider that delegates validation to <see cref="IUserStore"/>.
    /// </summary>
    public sealed class SimpleFtpAuthenticationProvider : IFtpAuthenticationProvider
    {
        private readonly IUserStore _userStore;

        // IIS loads providers via the parameterless constructor.
        public SimpleFtpAuthenticationProvider() : this(UserStoreFactory.Create())
        {
        }

        internal SimpleFtpAuthenticationProvider(IUserStore userStore)
        {
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
        }

        public bool AuthenticateUser(string sessionId,
                                     string siteName,
                                     string userName,
                                     string userPassword,
                                     out string canonicalUserName)
        {
            canonicalUserName = userName;
            var valid = _userStore.ValidateAsync(userName, userPassword).ConfigureAwait(false).GetAwaiter().GetResult();
            return valid;
        }
    }
} 