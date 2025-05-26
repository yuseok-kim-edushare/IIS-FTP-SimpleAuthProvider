using System;
using IIS.Ftp.SimpleAuth.Core.Logging;
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
        private readonly AuditLogger _auditLogger;

        // IIS loads providers via the parameterless constructor.
        public SimpleFtpAuthenticationProvider() : this(UserStoreFactory.Create(), UserStoreFactory.GetAuditLogger())
        {
        }

        internal SimpleFtpAuthenticationProvider(IUserStore userStore, AuditLogger auditLogger)
        {
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        }

        public bool AuthenticateUser(string sessionId,
                                     string siteName,
                                     string userName,
                                     string userPassword,
                                     out string canonicalUserName)
        {
            canonicalUserName = userName;
            
            try
            {
                var valid = _userStore.Validate(userName, userPassword);
                
                if (valid)
                {
                    _auditLogger.LogAuthenticationSuccess(sessionId, siteName, userName);
                }
                else
                {
                    _auditLogger.LogAuthenticationFailure(sessionId, siteName, userName, "Invalid credentials");
                }
                
                return valid;
            }
            catch (Exception ex)
            {
                _auditLogger.LogAuthenticationFailure(sessionId, siteName, userName, $"Authentication error: {ex.Message}");
                return false;
            }
        }
    }
} 