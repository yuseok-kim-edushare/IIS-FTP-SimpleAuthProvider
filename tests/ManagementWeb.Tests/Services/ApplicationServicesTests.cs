using IIS.FTP.ManagementWeb.Services;
using IIS.Ftp.SimpleAuth.Core.Logging;
using IIS.Ftp.SimpleAuth.Core.Monitoring;
using IIS.Ftp.SimpleAuth.Core.Configuration;
using IIS.Ftp.SimpleAuth.Core.Security;
using IIS.Ftp.SimpleAuth.Core.Stores;
using IIS.Ftp.SimpleAuth.Core.Domain;
using NUnit.Framework;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace IIS.FTP.ManagementWeb.Tests.Services
{
    [TestFixture]
    public class ApplicationServicesTests
    {
        private Mock<IUserStore> _mockUserStore;
        private Mock<IPasswordHasher> _mockPasswordHasher;
        private Mock<IAuditLogger> _mockAuditLogger;
        private Mock<IMetricsCollector> _mockMetricsCollector;
        private AuthProviderConfig _config;
        private ApplicationServices _applicationServices;

        [SetUp]
        public void TestInitialize()
        {
            _mockUserStore = new Mock<IUserStore>();
            _mockPasswordHasher = new Mock<IPasswordHasher>();
            _mockAuditLogger = new Mock<IAuditLogger>();
            _mockMetricsCollector = new Mock<IMetricsCollector>();
            
            _config = new AuthProviderConfig
            {
                UserStore = new UserStoreConfig { Type = "Json" },
                Hashing = new HashingConfig { Algorithm = "BCrypt" }
            };

            _applicationServices = new ApplicationServices(
                _mockUserStore.Object,
                _mockPasswordHasher.Object,
                _mockAuditLogger.Object,
                _mockMetricsCollector.Object,
                _config);
        }

        [Test]
        public async Task ValidateUserAsync_ValidCredentials_ReturnsTrue()
        {
            // Arrange
            var userId = "testuser";
            var password = "testpassword";
            _mockUserStore.Setup(x => x.ValidateAsync(userId, password))
                         .ReturnsAsync(true);

            // Act
            var result = await _applicationServices.ValidateUserAsync(userId, password);

            // Assert
            Assert.That(result, Is.True);
            _mockAuditLogger.Verify(x => x.LogAuthenticationAsync(userId, true, "Web UI login"), Times.Once);
            _mockMetricsCollector.Verify(x => x.IncrementAuthSuccess(), Times.Once);
        }

        [Test]
        public async Task ValidateUserAsync_InvalidCredentials_ReturnsFalse()
        {
            // Arrange
            var userId = "testuser";
            var password = "wrongpassword";
            _mockUserStore.Setup(x => x.ValidateAsync(userId, password))
                         .ReturnsAsync(false);

            // Act
            var result = await _applicationServices.ValidateUserAsync(userId, password);

            // Assert
            Assert.That(result, Is.False);
            _mockAuditLogger.Verify(x => x.LogAuthenticationAsync(userId, false, "Web UI login failed"), Times.Once);
            _mockMetricsCollector.Verify(x => x.IncrementAuthFailure(), Times.Once);
        }

        [Test]
        public async Task GetAllUsersAsync_ReturnsUsersFromStore()
        {
            // Arrange
            var expectedUsers = new List<User>
            {
                new User { UserId = "user1", DisplayName = "User One" },
                new User { UserId = "user2", DisplayName = "User Two" }
            };
            _mockUserStore.Setup(x => x.GetAllUsersAsync())
                         .ReturnsAsync(expectedUsers);

            // Act
            var result = await _applicationServices.GetAllUsersAsync();

            // Assert
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.Any(u => u.UserId == "user1"), Is.True);
            Assert.That(result.Any(u => u.UserId == "user2"), Is.True);
        }

        [Test]
        public async Task GetSystemHealthAsync_ReturnsHealthInformation()
        {
            // Arrange
            var metrics = new Dictionary<string, long>
            {
                { "ftp_auth_success_total", 10 },
                { "ftp_auth_failure_total", 2 }
            };
            _mockMetricsCollector.Setup(x => x.GetMetrics())
                               .Returns(metrics);

            // Act
            var result = await _applicationServices.GetSystemHealthAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsHealthy, Is.True);
            Assert.That(result.UserStoreType, Is.EqualTo("Json"));
            Assert.That(result.AuthSuccessCount, Is.EqualTo(10));
            Assert.That(result.AuthFailureCount, Is.EqualTo(2));
        }
    }
}