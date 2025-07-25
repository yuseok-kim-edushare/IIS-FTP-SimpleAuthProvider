using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IIS.Ftp.SimpleAuth.Core.Configuration;
using System.IO;
using IIS.FTP.Core.Logging;

namespace IIS.Ftp.SimpleAuth.Core.Logging
{
    /// <summary>
    /// Provides audit logging for authentication events to Windows Event Log.
    /// </summary>
    public class AuditLogger : IAuditLogger, IDisposable
    {
        private readonly LoggingConfig _config;
        private readonly EventLog? _eventLog;
        private readonly string? _logFilePath;
        private readonly StreamWriter? _fileWriter;
        private readonly object _fileLock = new object();
        private readonly ConcurrentQueue<AuditEntry> _recentEntries = new ConcurrentQueue<AuditEntry>();

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

            if (_config.EnableFileLog && !string.IsNullOrEmpty(_config.FileLogPath))
            {
                _logFilePath = _config.FileLogPath;
                try
                {
                    // Ensure directory exists
                    var logDirectory = Path.GetDirectoryName(_logFilePath);
                    if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
                    {
                        Directory.CreateDirectory(logDirectory);
                    }

                    // Open file in append mode
                    _fileWriter = new StreamWriter(new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to initialize FileLog at {_logFilePath}: {ex.Message}");
                    _fileWriter?.Dispose();
                    _fileWriter = null;
                }
            }
        }

        public virtual void LogAuthenticationSuccess(string sessionId, string siteName, string userName, string clientIp = "unknown")
        {
            if (!_config.LogSuccesses) return;

            var message = $"FTP Authentication SUCCESS - User: {userName}, Site: {siteName}, SessionId: {sessionId}, ClientIP: {clientIp}";
            LogEvent(EventLogEntryType.SuccessAudit, 1001, message);
        }

        public virtual void LogAuthenticationFailure(string sessionId, string siteName, string userName, string reason = "Invalid credentials", string clientIp = "unknown")
        {
            if (!_config.LogFailures) return;

            var message = $"FTP Authentication FAILURE - User: {userName}, Site: {siteName}, SessionId: {sessionId}, Reason: {reason}, ClientIP: {clientIp}";
            LogEvent(EventLogEntryType.FailureAudit, 1002, message);
        }

        public virtual void LogUserStoreError(string operation, string error)
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

            if (_fileWriter != null)
            {
                var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ssZ} [{(entryType == EventLogEntryType.SuccessAudit ? "INFO" : entryType == EventLogEntryType.FailureAudit ? "WARN" : "ERROR")}] {message}";
                lock (_fileLock)
                {
                    _fileWriter.WriteLine(logEntry);
                }
            }
        }

        public void Dispose()
        {
            _eventLog?.Dispose();
            _fileWriter?.Dispose();
        }

        // Interface implementations
        public async Task LogAuthenticationAsync(string userId, bool success, string details)
        {
            await Task.Run(() =>
            {
                if (success)
                {
                    LogAuthenticationSuccess("web-session", "web-ui", userId);
                }
                else
                {
                    LogAuthenticationFailure("web-session", "web-ui", userId, details);
                }

                AddToRecentEntries(new AuditEntry
                {
                    Timestamp = DateTime.UtcNow,
                    UserId = userId,
                    Action = success ? "Login Success" : "Login Failed",
                    Details = details,
                    Success = success
                });
            });
        }

        public async Task LogUserManagementAsync(string adminUser, string action)
        {
            await Task.Run(() =>
            {
                LogConfigurationChange("UserManagement", $"{adminUser}: {action}");
                
                AddToRecentEntries(new AuditEntry
                {
                    Timestamp = DateTime.UtcNow,
                    UserId = adminUser,
                    Action = "User Management",
                    Details = action,
                    Success = true
                });
            });
        }

        public async Task LogErrorAsync(string message)
        {
            await Task.Run(() =>
            {
                LogUserStoreError("WebUI", message);
                
                AddToRecentEntries(new AuditEntry
                {
                    Timestamp = DateTime.UtcNow,
                    UserId = "System",
                    Action = "Error",
                    Details = message,
                    Success = false
                });
            });
        }

        public async Task<IEnumerable<AuditEntry>> GetRecentEntriesAsync(int count)
        {
            return await Task.FromResult(GetRecentEntries(count));
        }

        private void AddToRecentEntries(AuditEntry entry)
        {
            lock (_entriesLock)
            {
                _recentEntries.Add(entry);
                
                // Keep only the last 100 entries
                if (_recentEntries.Count > 100)
                {
                    _recentEntries.RemoveRange(0, _recentEntries.Count - 100);
                }
            }
        }

        private IEnumerable<AuditEntry> GetRecentEntries(int count)
        {
            lock (_entriesLock)
            {
                return _recentEntries
                    .OrderByDescending(e => e.Timestamp)
                    .Take(count)
                    .ToList();
            }
        }
    }
} 