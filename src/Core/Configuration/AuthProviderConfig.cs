using System.Text.Json.Serialization;

namespace IIS.Ftp.SimpleAuth.Core.Configuration
{
    /// <summary>
    /// Root configuration for the FTP authentication provider.
    /// </summary>
    public class AuthProviderConfig
    {
        public UserStoreConfig UserStore { get; set; } = new();
        public HashingConfig Hashing { get; set; } = new();
        public LoggingConfig Logging { get; set; } = new();
        public MetricsConfig Metrics { get; set; } = new();
    }

    public class UserStoreConfig
    {
        /// <summary>
        /// Type of user store: "Json", "SqlServer", "SQLite", etc.
        /// </summary>
        public string Type { get; set; } = "Json";

        /// <summary>
        /// Path to the user store file.
        /// </summary>
        public string Path { get; set; } = "C:\\inetpub\\ftpusers\\users.json";

        /// <summary>
        /// Environment variable name containing the encryption key.
        /// If null/empty, uses DPAPI.
        /// </summary>
        public string? EncryptionKeyEnv { get; set; }

        /// <summary>
        /// Enable hot-reload of user store changes.
        /// </summary>
        public bool EnableHotReload { get; set; } = true;

        /// <summary>
        /// Database connection string for SQL-based stores.
        /// </summary>
        public string? ConnectionString { get; set; }
    }

    public class HashingConfig
    {
        /// <summary>
        /// Password hashing algorithm: "PBKDF2", "BCrypt", "Argon2".
        /// </summary>
        public string Algorithm { get; set; } = "BCrypt";

        /// <summary>
        /// Number of iterations for PBKDF2.
        /// </summary>
        public int Iterations { get; set; } = 100_000;

        /// <summary>
        /// Salt size in bytes.
        /// </summary>
        public int SaltSize { get; set; } = 16;
    }

    public class LoggingConfig
    {
        /// <summary>
        /// Enable audit logging to Windows Event Log.
        /// </summary>
        public bool EnableEventLog { get; set; } = true;

        /// <summary>
        /// Event log source name.
        /// </summary>
        public string EventLogSource { get; set; } = "IIS-FTP-SimpleAuth";

        /// <summary>
        /// Log authentication failures.
        /// </summary>
        public bool LogFailures { get; set; } = true;

        /// <summary>
        /// Log authentication successes.
        /// </summary>
        public bool LogSuccesses { get; set; } = false;

        /// <summary>
        /// Enable file logging.
        /// </summary>
        public bool EnableFileLog { get; set; } = false;

        /// <summary>
        /// Path to the file log.
        /// </summary>
        public string? FileLogPath { get; set; } = "C:\\inetpub\\ftpauth\\auth.log";
    }

    public class MetricsConfig
    {
        /// <summary>
        /// Enable metrics collection.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Path to the metrics file for Prometheus textfile collector.
        /// </summary>
        public string MetricsFilePath { get; set; } = "C:\\inetpub\\ftpmetrics\\ftp_metrics.prom";

        /// <summary>
        /// Export interval in seconds.
        /// </summary>
        public int ExportIntervalSeconds { get; set; } = 60;
    }
} 