using IIS.Ftp.SimpleAuth.Core.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IIS.Ftp.SimpleAuth.Core.Tests.Configuration
{
    [TestClass]
    public class AuthProviderConfigTests
    {
        [TestMethod]
        public void AuthProviderConfig_DefaultConstructor_ShouldInitializeWithDefaults()
        {
            // Act
            var config = new AuthProviderConfig();

            // Assert
            Assert.IsNotNull(config.UserStore);
            Assert.IsNotNull(config.Hashing);
            Assert.IsNotNull(config.Logging);
        }

        [TestMethod]
        public void AuthProviderConfig_Properties_ShouldBeSettable()
        {
            // Arrange
            var userStore = new UserStoreConfig { Type = "SqlServer" };
            var hashing = new HashingConfig { Algorithm = "BCrypt" };
            var logging = new LoggingConfig { EnableEventLog = false };

            // Act
            var config = new AuthProviderConfig
            {
                UserStore = userStore,
                Hashing = hashing,
                Logging = logging
            };

            // Assert
            Assert.That(config.UserStore, Is.SameAs(userStore));
            Assert.That(config.Hashing, Is.SameAs(hashing));
            Assert.That(config.Logging, Is.SameAs(logging));
        }
    }

    [TestClass]
    public class UserStoreConfigTests
    {
        [TestMethod]
        public void UserStoreConfig_DefaultConstructor_ShouldInitializeWithDefaults()
        {
            // Act
            var config = new UserStoreConfig();

            // Assert
            Assert.AreEqual("Json", config.Type);
            Assert.AreEqual("C:\\inetpub\\ftpusers\\users.json", config.Path);
            Assert.IsNull(config.EncryptionKeyEnv);
            Assert.IsTrue(config.EnableHotReload);
        }

        [TestMethod]
        public void UserStoreConfig_Type_ShouldBeSettable()
        {
            // Arrange
            var config = new UserStoreConfig();
            var expectedType = "SqlServer";

            // Act
            config.Type = expectedType;

            // Assert
            Assert.AreEqual(expectedType, config.Type);
        }

        [TestMethod]
        public void UserStoreConfig_Path_ShouldBeSettable()
        {
            // Arrange
            var config = new UserStoreConfig();
            var expectedPath = "D:\\data\\users.json";

            // Act
            config.Path = expectedPath;

            // Assert
            Assert.AreEqual(expectedPath, config.Path);
        }

        [TestMethod]
        public void UserStoreConfig_EncryptionKeyEnv_ShouldBeSettable()
        {
            // Arrange
            var config = new UserStoreConfig();
            var expectedKeyEnv = "FTP_ENCRYPTION_KEY";

            // Act
            config.EncryptionKeyEnv = expectedKeyEnv;

            // Assert
            Assert.AreEqual(expectedKeyEnv, config.EncryptionKeyEnv);
        }

        [TestMethod]
        public void UserStoreConfig_EnableHotReload_ShouldBeSettable()
        {
            // Arrange
            var config = new UserStoreConfig();

            // Act
            config.EnableHotReload = false;

            // Assert
            Assert.IsFalse(config.EnableHotReload);
        }

        [TestMethod]
        public void UserStoreConfig_AllProperties_ShouldBeSettable()
        {
            // Arrange
            var expectedType = "SQLite";
            var expectedPath = "/var/lib/ftp/users.db";
            var expectedKeyEnv = "FTP_DB_KEY";
            var expectedHotReload = false;

            // Act
            var config = new UserStoreConfig
            {
                Type = expectedType,
                Path = expectedPath,
                EncryptionKeyEnv = expectedKeyEnv,
                EnableHotReload = expectedHotReload
            };

            // Assert
            Assert.AreEqual(expectedType, config.Type);
            Assert.AreEqual(expectedPath, config.Path);
            Assert.AreEqual(expectedKeyEnv, config.EncryptionKeyEnv);
            Assert.AreEqual(expectedHotReload, config.EnableHotReload);
        }
    }

    [TestClass]
    public class HashingConfigTests
    {
        [TestMethod]
        public void HashingConfig_DefaultConstructor_ShouldInitializeWithDefaults()
        {
            // Act
            var config = new HashingConfig();

            // Assert
            Assert.AreEqual("PBKDF2", config.Algorithm);
            Assert.AreEqual(100_000, config.Iterations);
            Assert.AreEqual(16, config.SaltSize);
        }

        [TestMethod]
        public void HashingConfig_Algorithm_ShouldBeSettable()
        {
            // Arrange
            var config = new HashingConfig();
            var expectedAlgorithm = "BCrypt";

            // Act
            config.Algorithm = expectedAlgorithm;

            // Assert
            Assert.AreEqual(expectedAlgorithm, config.Algorithm);
        }

        [TestMethod]
        public void HashingConfig_Iterations_ShouldBeSettable()
        {
            // Arrange
            var config = new HashingConfig();
            var expectedIterations = 200_000;

            // Act
            config.Iterations = expectedIterations;

            // Assert
            Assert.AreEqual(expectedIterations, config.Iterations);
        }

        [TestMethod]
        public void HashingConfig_SaltSize_ShouldBeSettable()
        {
            // Arrange
            var config = new HashingConfig();
            var expectedSaltSize = 32;

            // Act
            config.SaltSize = expectedSaltSize;

            // Assert
            Assert.AreEqual(expectedSaltSize, config.SaltSize);
        }

        [TestMethod]
        public void HashingConfig_AllProperties_ShouldBeSettable()
        {
            // Arrange
            var expectedAlgorithm = "Argon2";
            var expectedIterations = 50_000;
            var expectedSaltSize = 24;

            // Act
            var config = new HashingConfig
            {
                Algorithm = expectedAlgorithm,
                Iterations = expectedIterations,
                SaltSize = expectedSaltSize
            };

            // Assert
            Assert.AreEqual(expectedAlgorithm, config.Algorithm);
            Assert.AreEqual(expectedIterations, config.Iterations);
            Assert.AreEqual(expectedSaltSize, config.SaltSize);
        }

        [TestMethod]
        [DataRow("PBKDF2")]
        [DataRow("BCrypt")]
        [DataRow("Argon2")]
        public void HashingConfig_Algorithm_ShouldAcceptValidValues(string algorithm)
        {
            // Arrange
            var config = new HashingConfig();

            // Act
            config.Algorithm = algorithm;

            // Assert
            Assert.AreEqual(algorithm, config.Algorithm);
        }

        [TestMethod]
        [DataRow(1000)]
        [DataRow(10_000)]
        [DataRow(100_000)]
        [DataRow(1_000_000)]
        public void HashingConfig_Iterations_ShouldAcceptValidValues(int iterations)
        {
            // Arrange
            var config = new HashingConfig();

            // Act
            config.Iterations = iterations;

            // Assert
            Assert.AreEqual(iterations, config.Iterations);
        }

        [TestMethod]
        [DataRow(8)]
        [DataRow(16)]
        [DataRow(32)]
        [DataRow(64)]
        public void HashingConfig_SaltSize_ShouldAcceptValidValues(int saltSize)
        {
            // Arrange
            var config = new HashingConfig();

            // Act
            config.SaltSize = saltSize;

            // Assert
            Assert.AreEqual(saltSize, config.SaltSize);
        }
    }

    [TestClass]
    public class LoggingConfigTests
    {
        [TestMethod]
        public void LoggingConfig_DefaultConstructor_ShouldInitializeWithDefaults()
        {
            // Act
            var config = new LoggingConfig();

            // Assert
            Assert.IsTrue(config.EnableEventLog);
            Assert.AreEqual("IIS-FTP-SimpleAuth", config.EventLogSource);
            Assert.IsTrue(config.LogFailures);
            Assert.IsFalse(config.LogSuccesses);
        }

        [TestMethod]
        public void LoggingConfig_EnableEventLog_ShouldBeSettable()
        {
            // Arrange
            var config = new LoggingConfig();

            // Act
            config.EnableEventLog = false;

            // Assert
            Assert.IsFalse(config.EnableEventLog);
        }

        [TestMethod]
        public void LoggingConfig_EventLogSource_ShouldBeSettable()
        {
            // Arrange
            var config = new LoggingConfig();
            var expectedSource = "CustomFTPAuth";

            // Act
            config.EventLogSource = expectedSource;

            // Assert
            Assert.AreEqual(expectedSource, config.EventLogSource);
        }

        [TestMethod]
        public void LoggingConfig_LogFailures_ShouldBeSettable()
        {
            // Arrange
            var config = new LoggingConfig();

            // Act
            config.LogFailures = false;

            // Assert
            Assert.IsFalse(config.LogFailures);
        }

        [TestMethod]
        public void LoggingConfig_LogSuccesses_ShouldBeSettable()
        {
            // Arrange
            var config = new LoggingConfig();

            // Act
            config.LogSuccesses = true;

            // Assert
            Assert.IsTrue(config.LogSuccesses);
        }

        [TestMethod]
        public void LoggingConfig_AllProperties_ShouldBeSettable()
        {
            // Arrange
            var expectedEnableEventLog = false;
            var expectedEventLogSource = "MyCustomAuth";
            var expectedLogFailures = false;
            var expectedLogSuccesses = true;

            // Act
            var config = new LoggingConfig
            {
                EnableEventLog = expectedEnableEventLog,
                EventLogSource = expectedEventLogSource,
                LogFailures = expectedLogFailures,
                LogSuccesses = expectedLogSuccesses
            };

            // Assert
            Assert.AreEqual(expectedEnableEventLog, config.EnableEventLog);
            Assert.AreEqual(expectedEventLogSource, config.EventLogSource);
            Assert.AreEqual(expectedLogFailures, config.LogFailures);
            Assert.AreEqual(expectedLogSuccesses, config.LogSuccesses);
        }

        [TestMethod]
        public void LoggingConfig_EventLogSource_ShouldAcceptEmptyString()
        {
            // Arrange
            var config = new LoggingConfig();

            // Act
            config.EventLogSource = string.Empty;

            // Assert
            Assert.AreEqual(string.Empty, config.EventLogSource);
        }
    }
} 