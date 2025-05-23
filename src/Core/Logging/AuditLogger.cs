using System;
using System.Diagnostics;
using IIS.Ftp.SimpleAuth.Core.Configuration;

namespace IIS.Ftp.SimpleAuth.Core.Logging
{
    /// <summary>
    /// Provides audit logging for authentication events to Windows Event Log.
    /// </summary>
    public class AuditLogger
    {
        private readonly LoggingConfig _config;
        private readonly EventLog? _eventLog;

        public AuditLogger(LoggingConfig config)
        {
            _config = config;

            if (_config.EnableEventLog)
            {
                try
                {
                    if (!EventLog.SourceExists(_config.EventLogSource))
                    {
                        EventLog.CreateEventSource(_config.EventLogSource, "Application");
                    }
                    _eventLog = new EventLog("Application") { Source = _config.EventLogSource };
                }
                catch (Exception ex)
                {
                    // Fallback if EventLog is not available (non-admin scenarios)
                    Debug.WriteLine($"Failed to initialize EventLog: {ex.Message}");
                }
            }
        }

        public void LogAuthenticationSuccess(string sessionId, string siteName, string userName, string clientIp = "unknown")
        {
            if (!_config.LogSuccesses) return;

            var message = $"FTP Authentication SUCCESS - User: {userName}, Site: {siteName}, SessionId: {sessionId}, ClientIP: {clientIp}";
            LogEvent(EventLogEntryType.SuccessAudit, 1001, message);
        }

        public void LogAuthenticationFailure(string sessionId, string siteName, string userName, string reason = "Invalid credentials", string clientIp = "unknown")
        {
            if (!_config.LogFailures) return;

            var message = $"FTP Authentication FAILURE - User: {userName}, Site: {siteName}, SessionId: {sessionId}, Reason: {reason}, ClientIP: {clientIp}";
            LogEvent(EventLogEntryType.FailureAudit, 1002, message);
        }

        public void LogUserStoreError(string operation, string error)
        {
            var message = $"FTP UserStore ERROR - Operation: {operation}, Error: {error}";
            LogEvent(EventLogEntryType.Error, 1003, message);
        }

        public void LogConfigurationChange(string component, string change)
        {
            var message = $"FTP Configuration CHANGE - Component: {component}, Change: {change}";
            LogEvent(EventLogEntryType.Information, 1004, message);
        }

        private void LogEvent(EventLogEntryType entryType, int eventId, string message)
        {
            try
            {
                _eventLog?.WriteEntry(message, entryType, eventId);
            }
            catch (Exception ex)
            {
                // Fallback to Debug output if EventLog fails
                Debug.WriteLine($"EventLog write failed: {ex.Message}");
                Debug.WriteLine($"Event: {message}");
            }
        }

        public void Dispose()
        {
            _eventLog?.Dispose();
        }
    }
} 