using IIS.Ftp.SimpleAuth.Core.Configuration;
using NUnit.Framework;

namespace IIS.Ftp.SimpleAuth.Core.Tests.Configuration
{
    [TestFixture]
    public class AuthProviderConfigTests
    {
        [Test]
        public void AuthProviderConfig_DefaultConstructor_ShouldInitializeWithDefaults()
        {
            // Act
            var config = new AuthProviderConfig();

            // Assert
            Assert.That(config.UserStore, Is.Not.Null);
            Assert.That(config.Hashing, Is.Not.Null);
            Assert.That(config.Logging, Is.Not.Null);
        }

        [Test]
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

    [TestFixture]
    public class UserStoreConfigTests
    {
        [Test]
        public void UserStoreConfig_DefaultConstructor_ShouldInitializeWithDefaults()
        {
            // Act
            var config = new UserStoreConfig();

            // Assert
            Assert.That(config.Type, Is.EqualTo("Json"));
            Assert.That(config.Path, Is.EqualTo("C:\\inetpub\\ftpusers\\users.json"));
            Assert.That(config.EncryptionKeyEnv, Is.Null);
            Assert.That(config.EnableHotReload, Is.True);
        }

        [Test]
        public void UserStoreConfig_Type_ShouldBeSettable()
        {
            // Arrange
            var config = new UserStoreConfig();
            var expectedType = "SqlServer";

            // Act
            config.Type = expectedType;

            // Assert
            Assert.That(config.Type, Is.EqualTo(expectedType));
        }

        [Test]
        public void UserStoreConfig_Path_ShouldBeSettable()
        {
            // Arrange
            var config = new UserStoreConfig();
            var expectedPath = "D:\\data\\users.json";

            // Act
            config.Path = expectedPath;

            // Assert
            Assert.That(config.Path, Is.EqualTo(expectedPath));
        }

        [Test]
        public void UserStoreConfig_EncryptionKeyEnv_ShouldBeSettable()
        {
            // Arrange
            var config = new UserStoreConfig();
            var expectedKeyEnv = "FTP_ENCRYPTION_KEY";

            // Act
            config.EncryptionKeyEnv = expectedKeyEnv;

            // Assert
            Assert.That(config.EncryptionKeyEnv, Is.EqualTo(expectedKeyEnv));
        }

        [Test]
        public void UserStoreConfig_EnableHotReload_ShouldBeSettable()
        {
            // Arrange
            var config = new UserStoreConfig();

            // Act
            config.EnableHotReload = false;

            // Assert
            Assert.That(config.EnableHotReload, Is.False);
        }

        [Test]
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
            Assert.That(config.Type, Is.EqualTo(expectedType));
            Assert.That(config.Path, Is.EqualTo(expectedPath));
            Assert.That(config.EncryptionKeyEnv, Is.EqualTo(expectedKeyEnv));
            Assert.That(config.EnableHotReload, Is.EqualTo(expectedHotReload));
        }
    }

    [TestFixture]
    public class HashingConfigTests
    {
        [Test]
        public void HashingConfig_DefaultConstructor_ShouldInitializeWithDefaults()
        {
            // Act
            var config = new HashingConfig();

            // Assert
            Assert.That(config.Algorithm, Is.EqualTo("PBKDF2"));
            Assert.That(config.Iterations, Is.EqualTo(100_000));
            Assert.That(config.SaltSize, Is.EqualTo(16));
        }

        [Test]
        public void HashingConfig_Algorithm_ShouldBeSettable()
        {
            // Arrange
            var config = new HashingConfig();
            var expectedAlgorithm = "BCrypt";

            // Act
            config.Algorithm = expectedAlgorithm;

            // Assert
            Assert.That(config.Algorithm, Is.EqualTo(expectedAlgorithm));
        }

        [Test]
        public void HashingConfig_Iterations_ShouldBeSettable()
        {
            // Arrange
            var config = new HashingConfig();
            var expectedIterations = 200_000;

            // Act
            config.Iterations = expectedIterations;

            // Assert
            Assert.That(config.Iterations, Is.EqualTo(expectedIterations));
        }

        [Test]
        public void HashingConfig_SaltSize_ShouldBeSettable()
        {
            // Arrange
            var config = new HashingConfig();
            var expectedSaltSize = 32;

            // Act
            config.SaltSize = expectedSaltSize;

            // Assert
            Assert.That(config.SaltSize, Is.EqualTo(expectedSaltSize));
        }

        [Test]
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
            Assert.That(config.Algorithm, Is.EqualTo(expectedAlgorithm));
            Assert.That(config.Iterations, Is.EqualTo(expectedIterations));
            Assert.That(config.SaltSize, Is.EqualTo(expectedSaltSize));
        }

        [Test]
        [TestCase("PBKDF2")]
        [TestCase("BCrypt")]
        [TestCase("Argon2")]
        public void HashingConfig_Algorithm_ShouldAcceptValidValues(string algorithm)
        {
            // Arrange
            var config = new HashingConfig();

            // Act
            config.Algorithm = algorithm;

            // Assert
            Assert.That(config.Algorithm, Is.EqualTo(algorithm));
        }

        [Test]
        [TestCase(1000)]
        [TestCase(10_000)]
        [TestCase(100_000)]
        [TestCase(1_000_000)]
        public void HashingConfig_Iterations_ShouldAcceptValidValues(int iterations)
        {
            // Arrange
            var config = new HashingConfig();

            // Act
            config.Iterations = iterations;

            // Assert
            Assert.That(config.Iterations, Is.EqualTo(iterations));
        }

        [Test]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(64)]
        public void HashingConfig_SaltSize_ShouldAcceptValidValues(int saltSize)
        {
            // Arrange
            var config = new HashingConfig();

            // Act
            config.SaltSize = saltSize;

            // Assert
            Assert.That(config.SaltSize, Is.EqualTo(saltSize));
        }
    }

    [TestFixture]
    public class LoggingConfigTests
    {
        [Test]
        public void LoggingConfig_DefaultConstructor_ShouldInitializeWithDefaults()
        {
            // Act
            var config = new LoggingConfig();

            // Assert
            Assert.That(config.EnableEventLog, Is.True);
            Assert.That(config.EventLogSource, Is.EqualTo("IIS-FTP-SimpleAuth"));
            Assert.That(config.LogFailures, Is.True);
            Assert.That(config.LogSuccesses, Is.False);
        }

        [Test]
        public void LoggingConfig_EnableEventLog_ShouldBeSettable()
        {
            // Arrange
            var config = new LoggingConfig();

            // Act
            config.EnableEventLog = false;

            // Assert
            Assert.That(config.EnableEventLog, Is.False);
        }

        [Test]
        public void LoggingConfig_EventLogSource_ShouldBeSettable()
        {
            // Arrange
            var config = new LoggingConfig();
            var expectedSource = "CustomFTPAuth";

            // Act
            config.EventLogSource = expectedSource;

            // Assert
            Assert.That(config.EventLogSource, Is.EqualTo(expectedSource));
        }

        [Test]
        public void LoggingConfig_LogFailures_ShouldBeSettable()
        {
            // Arrange
            var config = new LoggingConfig();

            // Act
            config.LogFailures = false;

            // Assert
            Assert.That(config.LogFailures, Is.False);
        }

        [Test]
        public void LoggingConfig_LogSuccesses_ShouldBeSettable()
        {
            // Arrange
            var config = new LoggingConfig();

            // Act
            config.LogSuccesses = true;

            // Assert
            Assert.That(config.LogSuccesses, Is.True);
        }

        [Test]
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
            Assert.That(config.EnableEventLog, Is.EqualTo(expectedEnableEventLog));
            Assert.That(config.EventLogSource, Is.EqualTo(expectedEventLogSource));
            Assert.That(config.LogFailures, Is.EqualTo(expectedLogFailures));
            Assert.That(config.LogSuccesses, Is.EqualTo(expectedLogSuccesses));
        }

        [Test]
        public void LoggingConfig_EventLogSource_ShouldAcceptEmptyString()
        {
            // Arrange
            var config = new LoggingConfig();

            // Act
            config.EventLogSource = string.Empty;

            // Assert
            Assert.That(config.EventLogSource, Is.EqualTo(string.Empty));
        }
    }
} 