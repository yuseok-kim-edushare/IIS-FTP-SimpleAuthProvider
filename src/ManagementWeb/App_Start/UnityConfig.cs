using IIS.Ftp.SimpleAuth.Core.Configuration;
using IIS.Ftp.SimpleAuth.Core.Stores;
using IIS.Ftp.SimpleAuth.Core.Security;
using IIS.FTP.Core.Logging;
using IIS.FTP.Core.Monitoring;
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

            // Register your types here
            ConfigureContainer(container);

            DependencyResolver.SetResolver(new UnityDependencyResolver(container));
        }

        private static void ConfigureContainer(IUnityContainer container)
        {
            // Load configuration
            var config = LoadConfiguration();
            container.RegisterInstance<AuthProviderConfig>(config);

            // Register Core services
            // Configure password hasher: default BCrypt; allow PBKDF2 via appSettings
            var algorithm = config.Hashing.Algorithm ?? "BCrypt";
            var iterations = config.Hashing.Iterations;
            var bcryptCost = 12; // could be moved to config later
            // Argon2 defaults (can become part of config later)
            var argonMemory = 32768; var argonIters = 3; var argonParallel = 2;
            container.RegisterFactory<IPasswordHasher>(_ => new PasswordHasherService(algorithm, iterations, bcryptCost, argonMemory, argonIters, argonParallel));
            container.RegisterFactory<IAuditLogger>(_ => new IIS.Ftp.SimpleAuth.Core.Logging.AuditLogger(config.Logging));
            container.RegisterFactory<IMetricsCollector>(_ => new IIS.Ftp.SimpleAuth.Core.Monitoring.MetricsCollector(config.Metrics.MetricsFilePath, TimeSpan.FromSeconds(config.Metrics.ExportIntervalSeconds)));

            // Register user store based on configuration
            RegisterUserStore(container, config);

            // Register application services
            container.RegisterType<IApplicationServices, ApplicationServices>();
        }

        private static AuthProviderConfig LoadConfiguration()
        {
            var config = new AuthProviderConfig
            {
                UserStore = new UserStoreConfig
                {
                    Type = ConfigurationManager.AppSettings["UserStore:Type"] ?? "Json",
                    Path = ConfigurationManager.AppSettings["UserStore:Path"] ?? @"C:\inetpub\ftpusers\users.enc",
                    EncryptionKeyEnv = ConfigurationManager.AppSettings["UserStore:EncryptionKeyEnv"] ?? "FTP_USERS_KEY"
                },
                Hashing = new HashingConfig
                {
                    Algorithm = ConfigurationManager.AppSettings["Hashing:Algorithm"] ?? "PBKDF2",
                    Iterations = int.Parse(ConfigurationManager.AppSettings["Hashing:Iterations"] ?? "100000")
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
            switch (config.UserStore.Type.ToLowerInvariant())
            {
                case "json":
                    if (!string.IsNullOrWhiteSpace(config.UserStore.EncryptionKeyEnv))
                    {
                        container.RegisterType<IUserStore, EncryptedJsonUserStore>("InnerUserStore",
                            new Unity.Injection.InjectionConstructor(
                                config.UserStore.Path,
                                config.UserStore.EnableHotReload,
                                config.UserStore.EncryptionKeyEnv,
                                new Unity.Injection.ResolvedParameter<IAuditLogger>()));
                    }
                    else
                    {
                        container.RegisterType<IUserStore, JsonUserStore>("InnerUserStore",
                            new Unity.Injection.InjectionConstructor(config.UserStore.Path, config.UserStore.EnableHotReload, new Unity.Injection.ResolvedParameter<IAuditLogger>()));
                    }
                    break;
                case "sqlite":
                    container.RegisterType<IUserStore, SqliteUserStore>("InnerUserStore",
                        new Unity.Injection.InjectionConstructor(config.UserStore.Path, new Unity.Injection.ResolvedParameter<IAuditLogger>()));
                    break;
                case "sqlserver":
                    container.RegisterType<IUserStore, SqlServerUserStore>("InnerUserStore",
                        new Unity.Injection.InjectionConstructor(config.UserStore.ConnectionString ?? string.Empty, new Unity.Injection.ResolvedParameter<IAuditLogger>()));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown user store type: {config.UserStore.Type}");
            }

            // Wrap with instrumentation: default mapping resolves to decorator over named inner
            container.RegisterType<IUserStore, InstrumentedUserStore>(
                new Unity.Injection.InjectionConstructor(
                    new Unity.Injection.ResolvedParameter<IUserStore>("InnerUserStore"),
                    new Unity.Injection.ResolvedParameter<IMetricsCollector>()
                ));
        }
    }
} 