using IIS.FTP.ManagementWeb.Services;
using IIS.FTP.Core.Logging;
using IIS.FTP.Core.Monitoring;
using IIS.Ftp.SimpleAuth.Core.Configuration;
using IIS.Ftp.SimpleAuth.Core.Security;
using IIS.Ftp.SimpleAuth.Core.Stores;
using IIS.Ftp.SimpleAuth.Core.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace IIS.FTP.ManagementWeb.Tests.Services
{
    [TestClass]
    public class ApplicationServicesTests
    {
        private Mock<IUserStore> _mockUserStore;
        private Mock<IPasswordHasher> _mockPasswordHasher;
        private Mock<IAuditLogger> _mockAuditLogger;
        private Mock<IMetricsCollector> _mockMetricsCollector;
        private AuthProviderConfig _config;
        private ApplicationServices _applicationServices;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockUserStore = new Mock<IUserStore>();
            _mockPasswordHasher = new Mock<IPasswordHasher>();
            _mockAuditLogger = new Mock<IAuditLogger>();
            _mockMetricsCollector = new Mock<IMetricsCollector>();
            
            _config = new AuthProviderConfig
            {
                UserStore = new UserStoreConfig { Type = "Json" },
                Hashing = new HashingConfig { Algorithm = "PBKDF2" }
            };

            _applicationServices = new ApplicationServices(
                _mockUserStore.Object,
                _mockPasswordHasher.Object,
                _mockAuditLogger.Object,
                _mockMetricsCollector.Object,
                _config);
        }

        [TestMethod]
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
            Assert.IsTrue(result);
            _mockAuditLogger.Verify(x => x.LogAuthenticationAsync(userId, true, "Web UI login"), Times.Once);
            _mockMetricsCollector.Verify(x => x.IncrementAuthSuccess(), Times.Once);
        }

        [TestMethod]
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
            Assert.IsFalse(result);
            _mockAuditLogger.Verify(x => x.LogAuthenticationAsync(userId, false, "Web UI login failed"), Times.Once);
            _mockMetricsCollector.Verify(x => x.IncrementAuthFailure(), Times.Once);
        }

        [TestMethod]
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
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Any(u => u.UserId == "user1"));
            Assert.IsTrue(result.Any(u => u.UserId == "user2"));
        }

        [TestMethod]
        public async Task GetSystemHealthAsync_ReturnsHealthInformation()
        {
            // Arrange
            var metrics = new Dictionary<string, long>
            {
                { "auth_success_total", 10 },
                { "auth_failure_total", 2 }
            };
            _mockMetricsCollector.Setup(x => x.GetMetrics())
                               .Returns(metrics);

            // Act
            var result = await _applicationServices.GetSystemHealthAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsHealthy);
            Assert.AreEqual("Json", result.UserStoreType);
            Assert.AreEqual(10, result.AuthSuccessCount);
            Assert.AreEqual(2, result.AuthFailureCount);
        }
    }
}