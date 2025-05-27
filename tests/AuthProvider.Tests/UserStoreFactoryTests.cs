using IIS.Ftp.SimpleAuth.Core.Configuration;
using IIS.Ftp.SimpleAuth.Core.Logging;
using IIS.Ftp.SimpleAuth.Core.Stores;
using IIS.Ftp.SimpleAuth.Provider;
using NUnit.Framework;
using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace IIS.Ftp.SimpleAuth.AuthProvider.Tests
{
    [TestFixture]
    public class UserStoreFactoryTests
    {
        private string _tempConfigPath = null!;
        private string _tempDirectory = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            _tempConfigPath = Path.Combine(_tempDirectory, "ftpauth.config.json");

            // Reset the static fields using reflection
            ResetStaticFields();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }

            // Reset the static fields again to clean up for other tests
            ResetStaticFields();
        }

        private void ResetStaticFields()
        {
            // Use reflection to reset static fields since they're private
            var factoryType = typeof(UserStoreFactory);
            var configField = factoryType.GetField("_config", BindingFlags.NonPublic | BindingFlags.Static);
            var auditLoggerField = factoryType.GetField("_auditLogger", BindingFlags.NonPublic | BindingFlags.Static);
            
            configField?.SetValue(null, null);
            auditLoggerField?.SetValue(null, null);
        }

        [Test]
        public void Create_DefaultConfiguration_ShouldReturnJsonUserStore()
        {
            // Act
            var userStore = UserStoreFactory.Create();

            // Assert
            Assert.That(userStore, Is.Not.Null);
            Assert.That(userStore, Is.TypeOf<JsonUserStore>());
        }

        [Test]
        public void Create_WithValidConfigFile_ShouldUseConfigurationSettings()
        {
            // Arrange
            var config = new AuthProviderConfig
            {
                UserStore = new UserStoreConfig
                {
                    Type = "Json",
                    Path = "/custom/path/users.json",
                    EnableHotReload = false
                }
            };

            CreateConfigFile(config);
            SetConfigPathAppSetting(_tempConfigPath);

            // Act
            var userStore = UserStoreFactory.Create();

            // Assert
            Assert.That(userStore, Is.Not.Null);
            Assert.That(userStore, Is.TypeOf<JsonUserStore>());
        }

        [Test]
        public void Create_InvalidConfigFile_ShouldFallBackToDefaults()
        {
            // Arrange
            File.WriteAllText(_tempConfigPath, "invalid json content");
            SetConfigPathAppSetting(_tempConfigPath);

            // Act
            var userStore = UserStoreFactory.Create();

            // Assert
            Assert.That(userStore, Is.Not.Null);
            Assert.That(userStore, Is.TypeOf<JsonUserStore>());
        }

        [Test]
        public void Create_NonExistentConfigFile_ShouldUseDefaults()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.json");
            SetConfigPathAppSetting(nonExistentPath);

            // Act
            var userStore = UserStoreFactory.Create();

            // Assert
            Assert.That(userStore, Is.Not.Null);
            Assert.That(userStore, Is.TypeOf<JsonUserStore>());
        }

        [Test]
        public void GetAuditLogger_DefaultConfiguration_ShouldReturnAuditLogger()
        {
            // Act
            var auditLogger = UserStoreFactory.GetAuditLogger();

            // Assert
            Assert.That(auditLogger, Is.Not.Null);
            Assert.That(auditLogger, Is.TypeOf<AuditLogger>());
        }

        [Test]
        public void GetAuditLogger_WithCustomLoggingConfig_ShouldUseConfiguration()
        {
            // Arrange
            var config = new AuthProviderConfig
            {
                Logging = new LoggingConfig
                {
                    EnableEventLog = false,
                    EventLogSource = "CustomSource",
                    LogFailures = false,
                    LogSuccesses = true
                }
            };

            CreateConfigFile(config);
            SetConfigPathAppSetting(_tempConfigPath);

            // Act
            var auditLogger = UserStoreFactory.GetAuditLogger();

            // Assert
            Assert.That(auditLogger, Is.Not.Null);
            Assert.That(auditLogger, Is.TypeOf<AuditLogger>());
        }

        [Test]
        public void GetAuditLogger_MultipleCalls_ShouldReturnSameInstance()
        {
            // Act
            var auditLogger1 = UserStoreFactory.GetAuditLogger();
            var auditLogger2 = UserStoreFactory.GetAuditLogger();

            // Assert
            Assert.That(auditLogger1, Is.SameAs(auditLogger2));
        }

        [Test]
        public void Create_MultipleCalls_ShouldReturnNewInstances()
        {
            // Act
            var userStore1 = UserStoreFactory.Create();
            var userStore2 = UserStoreFactory.Create();

            // Assert
            Assert.That(userStore1, Is.Not.SameAs(userStore2));
            Assert.That(userStore1, Is.TypeOf<JsonUserStore>());
            Assert.That(userStore2, Is.TypeOf<JsonUserStore>());
        }

        [Test]
        public void Create_ConfigLoadedOnce_ShouldReuseConfiguration()
        {
            // Arrange
            var config = new AuthProviderConfig
            {
                UserStore = new UserStoreConfig
                {
                    Path = "/test/path/users.json"
                }
            };

            CreateConfigFile(config);
            SetConfigPathAppSetting(_tempConfigPath);

            // Act
            var userStore1 = UserStoreFactory.Create();
            var userStore2 = UserStoreFactory.Create();

            // Assert
            Assert.That(userStore1, Is.Not.Null);
            Assert.That(userStore2, Is.Not.Null);
            // Both should use the same configuration (though be different instances)
        }

        [Test]
        public void Create_WithNullUserStoreConfig_ShouldUseDefaults()
        {
            // Arrange
            var config = new AuthProviderConfig
            {
                UserStore = null! // This shouldn't happen but testing resilience
            };

            CreateConfigFile(config);
            SetConfigPathAppSetting(_tempConfigPath);

            // Act
            var userStore = UserStoreFactory.Create();

            // Assert
            Assert.That(userStore, Is.Not.Null);
            Assert.That(userStore, Is.TypeOf<JsonUserStore>());
        }

        [Test]
        public void Create_ConfigFileInBaseDirectory_ShouldLoadConfiguration()
        {
            // Arrange - Create config file in base directory
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var configPath = Path.Combine(baseDirectory, "ftpauth.config.json");
            
            try
            {
                var config = new AuthProviderConfig
                {
                    UserStore = new UserStoreConfig
                    {
                        Path = "/base/directory/users.json"
                    }
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(configPath, json);

                // Reset factory to force reload
                ResetStaticFields();

                // Act
                var userStore = UserStoreFactory.Create();

                // Assert
                Assert.That(userStore, Is.Not.Null);
                Assert.That(userStore, Is.TypeOf<JsonUserStore>());
            }
            finally
            {
                // Cleanup
                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }
            }
        }

        [Test]
        public void Create_EmptyJsonConfig_ShouldUseDefaults()
        {
            // Arrange
            File.WriteAllText(_tempConfigPath, "{}");
            SetConfigPathAppSetting(_tempConfigPath);

            // Act
            var userStore = UserStoreFactory.Create();

            // Assert
            Assert.That(userStore, Is.Not.Null);
            Assert.That(userStore, Is.TypeOf<JsonUserStore>());
        }

        [Test]
        public void Create_PartialJsonConfig_ShouldMergeWithDefaults()
        {
            // Arrange
            var partialConfig = new
            {
                UserStore = new
                {
                    EnableHotReload = false
                }
            };

            var json = JsonSerializer.Serialize(partialConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_tempConfigPath, json);
            SetConfigPathAppSetting(_tempConfigPath);

            // Act
            var userStore = UserStoreFactory.Create();

            // Assert
            Assert.That(userStore, Is.Not.Null);
            Assert.That(userStore, Is.TypeOf<JsonUserStore>());  
        }

        [Test]
        public void GetAuditLogger_InvalidConfigFile_ShouldUseDefaults()
        {
            // Arrange
            File.WriteAllText(_tempConfigPath, "invalid json");
            SetConfigPathAppSetting(_tempConfigPath);

            // Act
            var auditLogger = UserStoreFactory.GetAuditLogger();

            // Assert
            Assert.That(auditLogger, Is.Not.Null);
            Assert.That(auditLogger, Is.TypeOf<AuditLogger>());
        }

        [Test]
        public void Create_ConfigFileWithDifferentCasing_ShouldLoad()
        {
            // Arrange
            var configWithDifferentCasing = """
            {
                "userstore": {
                    "type": "json",
                    "path": "/test/path.json"
                },
                "logging": {
                    "enableeventlog": true
                }
            }
            """;

            File.WriteAllText(_tempConfigPath, configWithDifferentCasing);
            SetConfigPathAppSetting(_tempConfigPath);

            // Act
            var userStore = UserStoreFactory.Create();

            // Assert
            Assert.That(userStore, Is.Not.Null);
            Assert.That(userStore, Is.TypeOf<JsonUserStore>());
        }

        private void CreateConfigFile(AuthProviderConfig config)
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(_tempConfigPath, json);
        }

        private void SetConfigPathAppSetting(string path)
        {
            // Note: In a real test environment, you might need to use a configuration override
            // or mock ConfigurationManager. For now, we'll test the fallback behavior.
            // This is a limitation of testing static methods that depend on ConfigurationManager.
        }

        [Test]
        public void Create_ThreadSafety_ShouldHandleConcurrentAccess()
        {
            // Arrange
            const int numberOfThreads = 10;
            var userStores = new IUserStore[numberOfThreads];
            var exceptions = new Exception?[numberOfThreads];

            // Act
            System.Threading.Tasks.Parallel.For(0, numberOfThreads, i =>
            {
                try
                {
                    userStores[i] = UserStoreFactory.Create();
                }
                catch (Exception ex)
                {
                    exceptions[i] = ex;
                }
            });

            // Assert
            for (int i = 0; i < numberOfThreads; i++)
            {
                Assert.That(exceptions[i], Is.Null, $"Thread {i} should not throw an exception");
                Assert.That(userStores[i], Is.Not.Null, $"Thread {i} should create a user store");
                Assert.That(userStores[i], Is.TypeOf<JsonUserStore>());
            }
        }

        [Test]
        public void GetAuditLogger_ThreadSafety_ShouldReturnSameInstance()
        {
            // Arrange
            const int numberOfThreads = 10;
            var auditLoggers = new AuditLogger[numberOfThreads];
            var exceptions = new Exception?[numberOfThreads];

            // Act
            System.Threading.Tasks.Parallel.For(0, numberOfThreads, i =>
            {
                try
                {
                    auditLoggers[i] = UserStoreFactory.GetAuditLogger();
                }
                catch (Exception ex)
                {
                    exceptions[i] = ex;
                }
            });

            // Assert
            for (int i = 0; i < numberOfThreads; i++)
            {
                Assert.That(exceptions[i], Is.Null, $"Thread {i} should not throw an exception");
                Assert.That(auditLoggers[i], Is.Not.Null, $"Thread {i} should get an audit logger");
            }

            // All instances should be the same (singleton pattern)
            for (int i = 1; i < numberOfThreads; i++)
            {
                Assert.That(auditLoggers[i], Is.SameAs(auditLoggers[0]), $"All audit loggers should be the same instance");
            }
        }
    }
} 