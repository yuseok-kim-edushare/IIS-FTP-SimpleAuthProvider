using IIS.Ftp.SimpleAuth.Core.Configuration;
using IIS.Ftp.SimpleAuth.Core.Stores;
using IIS.Ftp.SimpleAuth.Core.Security;
using IIS.Ftp.SimpleAuth.Core.Logging;
using IIS.Ftp.SimpleAuth.Core.Monitoring;
using IIS.FTP.ManagementWeb.Services;
using System;
using System.Configuration;
using System.Web.Mvc;
using Unity;
using Unity.Mvc5;

namespace IIS.FTP.ManagementWeb
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
            var container = new UnityContainer();
            ConfigureContainer(container);
            DependencyResolver.SetResolver(new UnityDependencyResolver(container));
        }

        private static void ConfigureContainer(IUnityContainer container)
        {
            try
            {
                // Load configuration
                var config = LoadConfiguration();
                container.RegisterInstance<AuthProviderConfig>(config);
                
                // Debug logging to file
                
                try
                {
                    System.IO.Directory.CreateDirectory(@"C:\temp");
                    System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                        $"{DateTime.Now}: Unity Configuration - UserStore.Type: {config.UserStore.Type}, Path: {config.UserStore.Path}, EncryptionKey: {config.UserStore.EncryptionKeyEnv}\n");
                }
                catch { }

                // Register Core services with simplified approach
                container.RegisterType<IPasswordHasher, PasswordHasherService>();
                container.RegisterType<IAuditLogger, IIS.Ftp.SimpleAuth.Core.Logging.AuditLogger>();
                
                // Simplified metrics registration
                if (config.Metrics.Enabled)
                {
                    container.RegisterFactory<IMetricsCollector>(_ => new IIS.Ftp.SimpleAuth.Core.Monitoring.MetricsCollector(
                        config.Metrics.MetricsFilePath ?? @"C:\inetpub\ftpmetrics\ftp_metrics.prom", 
                        TimeSpan.FromSeconds(config.Metrics.ExportIntervalSeconds > 0 ? config.Metrics.ExportIntervalSeconds : 60)
                    ));
                }
                else
                {
                    container.RegisterType<IMetricsCollector, IIS.Ftp.SimpleAuth.Core.Monitoring.NoOpMetricsCollector>();
                }

                // Simplified user store registration
                RegisterUserStore(container, config);

                // Register application services
                container.RegisterType<IApplicationServices, ApplicationServices>();
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - this allows the application to start
                System.Diagnostics.Debug.WriteLine($"Unity configuration error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private static AuthProviderConfig LoadConfiguration()
        {
            var config = new AuthProviderConfig
            {
                UserStore = new UserStoreConfig
                {
                    Type = ConfigurationManager.AppSettings["UserStore:Type"] ?? "Json",
                    Path = ConfigurationManager.AppSettings["UserStore:Path"] ?? @"C:\inetpub\ftpusers\users.enc",
                    EncryptionKeyEnv = ConfigurationManager.AppSettings["UserStore:EncryptionKeyEnv"] ?? "FTP_USERS_KEY",
                    EnableHotReload = bool.Parse(ConfigurationManager.AppSettings["UserStore:EnableHotReload"] ?? "true")
                },
                Hashing = new HashingConfig
                {
                    Algorithm = ConfigurationManager.AppSettings["Hashing:Algorithm"] ?? "PBKDF2",
                    Iterations = int.Parse(ConfigurationManager.AppSettings["Hashing:Iterations"] ?? "100000")
                },
                Logging = new LoggingConfig
                {
                    EnableEventLog = bool.Parse(ConfigurationManager.AppSettings["Logging:EnableEventLog"] ?? "true"),
                    EventLogSource = ConfigurationManager.AppSettings["Logging:EventLogSource"] ?? "IIS-FTP-SimpleAuth",
                    LogFailures = bool.Parse(ConfigurationManager.AppSettings["Logging:LogFailures"] ?? "true"),
                    LogSuccesses = bool.Parse(ConfigurationManager.AppSettings["Logging:LogSuccesses"] ?? "false"),
                    EnableFileLog = bool.Parse(ConfigurationManager.AppSettings["Logging:EnableFileLog"] ?? "false"),
                    FileLogPath = ConfigurationManager.AppSettings["Logging:FileLogPath"] ?? @"C:\inetpub\ftpauth\auth.log"
                },
                Metrics = new MetricsConfig
                {
                    Enabled = bool.Parse(ConfigurationManager.AppSettings["Metrics:Enabled"] ?? "true"),
                    MetricsFilePath = ConfigurationManager.AppSettings["Metrics:MetricsFilePath"] ?? @"C:\inetpub\ftpmetrics\ftp_metrics.prom",
                    ExportIntervalSeconds = int.Parse(ConfigurationManager.AppSettings["Metrics:ExportIntervalSeconds"] ?? "60")
                }
            };

            // Add connection string if using SQL
            var connectionString = ConfigurationManager.ConnectionStrings["FtpAuthDb"];
            if (connectionString != null)
            {
                config.UserStore.ConnectionString = connectionString.ConnectionString;
            }

            return config;
        }

        private static void RegisterUserStore(IUnityContainer container, AuthProviderConfig config)
        {
            // Debug logging for UserStore registration
            try
            {
                System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                    $"{DateTime.Now}: RegisterUserStore - Type: {config.UserStore.Type}, Path: {config.UserStore.Path}, EncryptionKeyEnv: '{config.UserStore.EncryptionKeyEnv}'\n");
            }
            catch { }

            // Use factory registration to provide constructor parameters
            switch (config.UserStore.Type.ToLowerInvariant())
            {
                case "json":
                    if (!string.IsNullOrWhiteSpace(config.UserStore.EncryptionKeyEnv))
                    {
                        try
                        {
                            System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                                $"{DateTime.Now}: Registering EncryptedJsonUserStore with path: {config.UserStore.Path}\n");
                        }
                        catch { }
                        
                        container.RegisterFactory<IUserStore>(c => 
                            new EncryptedJsonUserStore(
                                config.UserStore.Path, 
                                config.UserStore.EnableHotReload, 
                                config.UserStore.EncryptionKeyEnv,
                                c.Resolve<IAuditLogger>() as IIS.Ftp.SimpleAuth.Core.Logging.AuditLogger
                            )
                        );
                    }
                    else
                    {
                        try
                        {
                            System.IO.File.AppendAllText(@"C:\temp\login-debug.log", 
                                $"{DateTime.Now}: Registering JsonUserStore with path: {config.UserStore.Path}\n");
                        }
                        catch { }
                        
                        container.RegisterFactory<IUserStore>(c => 
                            new JsonUserStore(
                                config.UserStore.Path, 
                                config.UserStore.EnableHotReload,
                                c.Resolve<IAuditLogger>() as IIS.Ftp.SimpleAuth.Core.Logging.AuditLogger
                            )
                        );
                    }
                    break;
                case "sqlite":
                    container.RegisterFactory<IUserStore>(c => 
                        new SqliteUserStore(
                            config.UserStore.Path,
                            c.Resolve<IAuditLogger>() as IIS.Ftp.SimpleAuth.Core.Logging.AuditLogger
                        )
                    );
                    break;
                case "sqlserver":
                    container.RegisterFactory<IUserStore>(c => 
                        new SqlServerUserStore(
                            config.UserStore.ConnectionString ?? throw new InvalidOperationException("SQL Server connection string is required"),
                            c.Resolve<IAuditLogger>() as IIS.Ftp.SimpleAuth.Core.Logging.AuditLogger
                        )
                    );
                    break;
                default:
                    throw new InvalidOperationException($"Unknown user store type: {config.UserStore.Type}");
            }
        }
    }
} 