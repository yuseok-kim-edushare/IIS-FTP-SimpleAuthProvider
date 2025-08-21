using System;
using System.Collections.Generic;
using IIS.Ftp.SimpleAuth.Core.Monitoring;

namespace IIS.Ftp.SimpleAuth.Core.Monitoring
{
    /// <summary>
    /// No-operation implementation of IMetricsCollector for when metrics are disabled.
    /// </summary>
    public class NoOpMetricsCollector : IMetricsCollector
    {
        public void IncrementCounter(string metricName, long value = 1)
        {
            // No-op
        }

        public void RecordAuthSuccess(string userId)
        {
            // No-op
        }

        public void RecordAuthFailure(string userId, string reason = "invalid_credentials")
        {
            // No-op
        }

        public void RecordAuthorizationCheck(string userId, string path, bool allowed)
        {
            // No-op
        }

        public void RecordUserStoreOperation(string operation, bool success)
        {
            // No-op
        }

        public void IncrementAuthSuccess()
        {
            // No-op
        }

        public void IncrementAuthFailure()
        {
            // No-op
        }

        public Dictionary<string, long> GetMetrics()
        {
            return new Dictionary<string, long>();
        }

        public void Dispose()
        {
            // No-op
        }
    }
}
