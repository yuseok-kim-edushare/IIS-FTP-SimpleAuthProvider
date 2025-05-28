using System;
using System.Configuration;
using System.IO;
using System.Text.Json;
using IIS.Ftp.SimpleAuth.Core.Configuration;
using IIS.Ftp.SimpleAuth.Core.Logging;
using IIS.Ftp.SimpleAuth.Core.Monitoring;
using IIS.Ftp.SimpleAuth.Core.Stores;

namespace IIS.Ftp.SimpleAuth.Provider
{
    internal static class UserStoreFactory
    {
        private static readonly object _lock = new object();
        private static AuthProviderConfig? _config;
        private static AuditLogger? _auditLogger;
        private static MetricsCollector? _metricsCollector;

        public static IUserStore Create()
        {
            LoadConfiguration();
            
            var auditLogger = GetAuditLogger();
            var storeType = _config?.UserStore?.Type ?? "Json";
            var path = _config?.UserStore?.Path ?? GetLegacyPath();
            IUserStore store;
            switch (storeType.ToUpperInvariant())
            {
                case "SQLITE":
                    auditLogger.LogConfigurationChange("UserStoreFactory", $"Creating SQLite user store at: {path}");
                    store = new SqliteUserStore(path, auditLogger);
                    break;

                case "JSON":
                default:
                    var enableHotReload = _config?.UserStore?.EnableHotReload ?? true;
                    auditLogger.LogConfigurationChange("UserStoreFactory", $"Creating JSON user store at: {path}, HotReload: {enableHotReload}");
                    store = new JsonUserStore(path, enableHotReload, auditLogger);
                    break;
            }
            
            // Wrap with metrics instrumentation if enabled
            var metrics = GetMetricsCollector();
            if (metrics != null)
            {
                return new InstrumentedUserStore(store, metrics);
            }
            return store;
        }

        public static AuditLogger GetAuditLogger()
        {
            LoadConfiguration();
            
            if (_auditLogger == null)
            {
                lock (_lock)
                {
                    _auditLogger ??= new AuditLogger(_config?.Logging ?? new LoggingConfig());
                }
            }
            
            return _auditLogger;
        }

        public static MetricsCollector? GetMetricsCollector()
        {
            LoadConfiguration();
            
            if (_metricsCollector == null && _config?.Metrics?.Enabled == true)
            {
                lock (_lock)
                {
                    if (_metricsCollector == null && _config?.Metrics?.Enabled == true)
                    {
                        try
                        {
                            var exportInterval = TimeSpan.FromSeconds(_config.Metrics.ExportIntervalSeconds);
                            _metricsCollector = new MetricsCollector(_config.Metrics.MetricsFilePath, exportInterval);
                            _auditLogger?.LogConfigurationChange("MetricsCollector", 
                                $"Metrics collection enabled. Export path: {_config.Metrics.MetricsFilePath}");
                        }
                        catch (Exception ex)
                        {
                            _auditLogger?.LogConfigurationChange("MetricsCollector", 
                                $"Failed to initialize metrics: {ex.Message}");
                        }
                    }
                }
            }
            
            return _metricsCollector;
        }

        private static void LoadConfiguration()
        {
            if (_config != null) return;

            lock (_lock)
            {
                if (_config != null) return;

                try
                {
                    // Try to load from configuration file
                    var configPath = ConfigurationManager.AppSettings["ConfigPath"] 
                                    ?? Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "ftpauth.config.json");
                    
                    if (File.Exists(configPath))
                    {
                        var json = File.ReadAllText(configPath);
                        _config = JsonSerializer.Deserialize<AuthProviderConfig>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    }
                }
                catch (FileNotFoundException)
                {
                    // Expected if config file doesn't exist - fall back to defaults
                }
                catch (UnauthorizedAccessException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Access denied to config file: {ex.Message}");
                }
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid JSON in config file: {ex.Message}");
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"IO error reading config file: {ex.Message}");
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Unexpected error loading config: {ex.GetType().Name} - {ex.Message}");
                }

                _config ??= new AuthProviderConfig();
            }
        }

        private static string GetLegacyPath()
        {
            return ConfigurationManager.AppSettings["UserStorePath"]
                   ?? "C:\\inetpub\\ftpusers\\users.json";
        }
    }
} 