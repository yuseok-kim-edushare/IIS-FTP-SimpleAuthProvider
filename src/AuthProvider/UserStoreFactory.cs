using System.Configuration;
using System.IO;
using System.Text.Json;
using IIS.Ftp.SimpleAuth.Core.Configuration;
using IIS.Ftp.SimpleAuth.Core.Logging;
using IIS.Ftp.SimpleAuth.Core.Stores;

namespace IIS.Ftp.SimpleAuth.Provider
{
    internal static class UserStoreFactory
    {
        private static readonly object _lock = new object();
        private static AuthProviderConfig? _config;
        private static AuditLogger? _auditLogger;

        public static IUserStore Create()
        {
            LoadConfiguration();
            
            var path = _config?.UserStore?.Path ?? GetLegacyPath();
            var enableHotReload = _config?.UserStore?.EnableHotReload ?? true;

            return new JsonUserStore(path, enableHotReload);
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
                catch
                {
                    // Fall back to defaults if config loading fails
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