using System;
using IIS.Ftp.SimpleAuth.Core.Logging;
using IIS.Ftp.SimpleAuth.Core.Monitoring;
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
        private readonly MetricsCollector? _metricsCollector;

        // IIS loads providers via the parameterless constructor.
        public SimpleFtpAuthenticationProvider() : this(UserStoreFactory.Create(), UserStoreFactory.GetAuditLogger(), UserStoreFactory.GetMetricsCollector())
        {
        }

        internal SimpleFtpAuthenticationProvider(IUserStore userStore, AuditLogger auditLogger, MetricsCollector? metricsCollector = null)
        {
            _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
            _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
            _metricsCollector = metricsCollector;
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
                // Since IIS FTP interface is synchronous, we need to block on the async call
                // This is acceptable in this specific scenario as IIS manages the thread pool
                var valid = _userStore.ValidateAsync(userName, userPassword).GetAwaiter().GetResult();
                
                if (valid)
                {
                    _auditLogger.LogAuthenticationSuccess(sessionId, siteName, userName);
                    _metricsCollector?.RecordAuthSuccess(userName);
                }
                else
                {
                    _auditLogger.LogAuthenticationFailure(sessionId, siteName, userName, "Invalid credentials");
                    _metricsCollector?.RecordAuthFailure(userName, "invalid_credentials");
                }
                
                return valid;
            }
            catch (Exception ex)
            {
                _auditLogger.LogAuthenticationFailure(sessionId, siteName, userName, $"Authentication error: {ex.Message}");
                _metricsCollector?.RecordAuthFailure(userName, "exception");
                return false;
            }
        }
    }
} 