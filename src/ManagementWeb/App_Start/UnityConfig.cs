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
            container.RegisterType<IPasswordHasher, PasswordHasher>();
            container.RegisterType<IAuditLogger, AuditLogger>();
            container.RegisterType<IMetricsCollector, MetricsCollector>();

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
                    container.RegisterType<IUserStore, JsonUserStore>();
                    break;
                case "sqlite":
                    container.RegisterType<IUserStore, SqliteUserStore>();
                    break;
                case "sqlserver":
                    container.RegisterType<IUserStore, SqlServerUserStore>();
                    break;
                default:
                    throw new InvalidOperationException($"Unknown user store type: {config.UserStore.Type}");
            }

            // Wrap with instrumentation
            container.RegisterType<IUserStore, InstrumentedUserStore>(
                new Unity.Injection.InjectionConstructor(
                    new Unity.Injection.ResolvedParameter<IUserStore>(),
                    new Unity.Injection.ResolvedParameter<IMetricsCollector>(),
                    new Unity.Injection.ResolvedParameter<IAuditLogger>()
                )
            );
        }
    }
} 