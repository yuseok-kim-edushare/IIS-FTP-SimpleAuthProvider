using System.Collections.Generic;

namespace IIS.FTP.Core.Monitoring
{
    /// <summary>
    /// Interface for collecting and managing metrics.
    /// </summary>
    public interface IMetricsCollector
    {
        /// <summary>
        /// Increments the authentication success counter.
        /// </summary>
        void IncrementAuthSuccess();

        /// <summary>
        /// Increments the authentication failure counter.
        /// </summary>
        void IncrementAuthFailure();

        /// <summary>
        /// Gets current metrics as a dictionary.
        /// </summary>
        Dictionary<string, long> GetMetrics();
    }
}