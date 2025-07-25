using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;
using IIS.FTP.Core.Monitoring;

namespace IIS.Ftp.SimpleAuth.Core.Monitoring
{
    /// <summary>
    /// Collects and exports metrics in Prometheus textfile format.
    /// </summary>
    public class MetricsCollector : IMetricsCollector, IDisposable
    {
        private readonly ConcurrentDictionary<string, long> _counters = new ConcurrentDictionary<string, long>();
        private readonly string _metricsFilePath;
        private readonly Timer _exportTimer;
        private readonly object _writeLock = new object();

        public MetricsCollector(string metricsFilePath, TimeSpan exportInterval)
        {
            _metricsFilePath = metricsFilePath ?? throw new ArgumentNullException(nameof(metricsFilePath));
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_metricsFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Set up timer to export metrics periodically
            _exportTimer = new Timer(_ => ExportMetrics(), null, exportInterval, exportInterval);
            
            // Export initial empty metrics file
            ExportMetrics();
        }

        /// <summary>
        /// Increments a counter metric.
        /// </summary>
        public void IncrementCounter(string metricName, long value = 1)
        {
            _counters.AddOrUpdate(metricName, value, (key, oldValue) => oldValue + value);
        }

        /// <summary>
        /// Records an authentication success.
        /// </summary>
        public void RecordAuthSuccess(string userId)
        {
            IncrementCounter("ftp_auth_success_total");
            IncrementCounter($"ftp_auth_success_by_user{{user=\"{EscapeLabel(userId)}\"}}");
        }

        /// <summary>
        /// Records an authentication failure.
        /// </summary>
        public void RecordAuthFailure(string userId, string reason = "invalid_credentials")
        {
            IncrementCounter("ftp_auth_failure_total");
            IncrementCounter($"ftp_auth_failure_by_reason{{reason=\"{EscapeLabel(reason)}\"}}");
            if (!string.IsNullOrEmpty(userId))
            {
                IncrementCounter($"ftp_auth_failure_by_user{{user=\"{EscapeLabel(userId)}\"}}");
            }
        }

        /// <summary>
        /// Records an authorization check.
        /// </summary>
        public void RecordAuthorizationCheck(string userId, string path, bool allowed)
        {
            var result = allowed ? "allowed" : "denied";
            IncrementCounter($"ftp_authorization_checks_total{{result=\"{result}\"}}");
        }

        /// <summary>
        /// Records a user store operation.
        /// </summary>
        public void RecordUserStoreOperation(string operation, bool success)
        {
            var status = success ? "success" : "failure";
            IncrementCounter($"ftp_user_store_operations_total{{operation=\"{EscapeLabel(operation)}\",status=\"{status}\"}}");
        }

        /// <summary>
        /// Exports metrics to file in Prometheus textfile format.
        /// </summary>
        private void ExportMetrics()
        {
            try
            {
                var sb = new StringBuilder();
                
                // Add header
                sb.AppendLine("# HELP ftp_auth_success_total Total number of successful FTP authentications");
                sb.AppendLine("# TYPE ftp_auth_success_total counter");
                
                // Export counters
                foreach (var kvp in _counters)
                {
                    sb.AppendLine($"{kvp.Key} {kvp.Value}");
                }
                
                // Add timestamp
                sb.AppendLine($"# Generated at {DateTimeOffset.UtcNow:O}");
                
                // Write atomically
                lock (_writeLock)
                {
                    var tempFile = _metricsFilePath + ".tmp";
                    File.WriteAllText(tempFile, sb.ToString());
                    File.Move(tempFile, _metricsFilePath);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - metrics should not break the application
                System.Diagnostics.Debug.WriteLine($"Failed to export metrics: {ex.Message}");
            }
        }

        /// <summary>
        /// Escapes a label value for Prometheus format.
        /// </summary>
        private static string EscapeLabel(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n");
        }

        /// <summary>
        /// Disposes the metrics collector.
        /// </summary>
        public void Dispose()
        {
            _exportTimer?.Dispose();
            ExportMetrics(); // Final export
        }

        // Interface implementations
        public void IncrementAuthSuccess()
        {
            IncrementCounter("ftp_auth_success_total");
        }

        public void IncrementAuthFailure()
        {
            IncrementCounter("ftp_auth_failure_total");
        }

        public Dictionary<string, long> GetMetrics()
        {
            return _counters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
} 