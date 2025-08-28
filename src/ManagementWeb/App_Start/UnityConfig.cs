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
using Unity.Lifetime;
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
                
                // Register Core services with proper lifecycle management
                container.RegisterType<IPasswordHasher, PasswordHasherService>(new ContainerControlledLifetimeManager());
                
                // Register AuditLogger as singleton to avoid circular dependencies
                container.RegisterFactory<IAuditLogger>(c => 
                    new IIS.Ftp.SimpleAuth.Core.Logging.AuditLogger(config.Logging), 
                    new ContainerControlledLifetimeManager());
                
                // Register metrics collector
                if (config.Metrics.Enabled)
                {
                    container.RegisterFactory<IMetricsCollector>(c => 
                        new IIS.Ftp.SimpleAuth.Core.Monitoring.MetricsCollector(
                            config.Metrics.MetricsFilePath ?? @"C:\inetpub\ftpmetrics\ftp_metrics.prom", 
                            TimeSpan.FromSeconds(config.Metrics.ExportIntervalSeconds > 0 ? config.Metrics.ExportIntervalSeconds : 60)
                        ), new ContainerControlledLifetimeManager());
                }
                else
                {
                    container.RegisterType<IMetricsCollector, IIS.Ftp.SimpleAuth.Core.Monitoring.NoOpMetricsCollector>(new ContainerControlledLifetimeManager());
                }

                // Register user store with proper dependency resolution
                RegisterUserStore(container, config);

                // Register application services
                container.RegisterType<IApplicationServices, ApplicationServices>();
                
                // Validate container configuration
                ValidateContainerConfiguration(container);
            }
            catch (Exception ex)
            {
                // Log the error with more detail and throw to prevent broken DI
                System.Diagnostics.EventLog.WriteEntry("IIS-FTP-SimpleAuth", 
                    $"Unity configuration failed: {ex.Message}\n{ex.StackTrace}", 
                    System.Diagnostics.EventLogEntryType.Error);
                throw new InvalidOperationException($"Dependency injection configuration failed: {ex.Message}", ex);
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
            // Use factory registration with proper dependency resolution
            switch (config.UserStore.Type.ToLowerInvariant())
            {
                case "json":
                    if (!string.IsNullOrWhiteSpace(config.UserStore.EncryptionKeyEnv))
                    {
                        container.RegisterFactory<IUserStore>(c => 
                        {
                            var auditLogger = c.Resolve<IAuditLogger>() as IIS.Ftp.SimpleAuth.Core.Logging.AuditLogger;
                            return new EncryptedJsonUserStore(
                                config.UserStore.Path, 
                                config.UserStore.EnableHotReload, 
                                config.UserStore.EncryptionKeyEnv,
                                auditLogger
                            );
                        }, new ContainerControlledLifetimeManager());
                    }
                    else
                    {
                        container.RegisterFactory<IUserStore>(c => 
                        {
                            var auditLogger = c.Resolve<IAuditLogger>() as IIS.Ftp.SimpleAuth.Core.Logging.AuditLogger;
                            return new JsonUserStore(
                                config.UserStore.Path, 
                                config.UserStore.EnableHotReload,
                                auditLogger
                            );
                        }, new ContainerControlledLifetimeManager());
                    }
                    break;
                case "sqlite":
                    container.RegisterFactory<IUserStore>(c => 
                    {
                        var auditLogger = c.Resolve<IAuditLogger>() as IIS.Ftp.SimpleAuth.Core.Logging.AuditLogger;
                        return new SqliteUserStore(
                            config.UserStore.Path,
                            auditLogger
                        );
                    }, new ContainerControlledLifetimeManager());
                    break;
                case "sqlserver":
                    container.RegisterFactory<IUserStore>(c => 
                    {
                        var auditLogger = c.Resolve<IAuditLogger>() as IIS.Ftp.SimpleAuth.Core.Logging.AuditLogger;
                        return new SqlServerUserStore(
                            config.UserStore.ConnectionString ?? throw new InvalidOperationException("SQL Server connection string is required"),
                            auditLogger
                        );
                    }, new ContainerControlledLifetimeManager());
                    break;
                default:
                    throw new InvalidOperationException($"Unknown user store type: {config.UserStore.Type}");
            }
        }

        private static void ValidateContainerConfiguration(IUnityContainer container)
        {
            // Validate that all required services can be resolved
            try
            {
                container.Resolve<AuthProviderConfig>();
                container.Resolve<IPasswordHasher>();
                container.Resolve<IAuditLogger>();
                container.Resolve<IMetricsCollector>();
                container.Resolve<IUserStore>();
                container.Resolve<IApplicationServices>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Container validation failed. One or more services cannot be resolved: {ex.Message}", ex);
            }
        }
    }
} 