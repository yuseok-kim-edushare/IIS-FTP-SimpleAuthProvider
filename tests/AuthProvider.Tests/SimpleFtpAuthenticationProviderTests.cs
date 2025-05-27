using IIS.Ftp.SimpleAuth.Core.Logging;
using IIS.Ftp.SimpleAuth.Core.Stores;
using IIS.Ftp.SimpleAuth.Provider;
using Moq;
using NUnit.Framework;
using System;

namespace IIS.Ftp.SimpleAuth.AuthProvider.Tests
{
    [TestFixture]
    public class SimpleFtpAuthenticationProviderTests
    {
        private Mock<IUserStore> _mockUserStore = null!;
        private Mock<AuditLogger> _mockAuditLogger = null!;
        private SimpleFtpAuthenticationProvider _provider = null!;

        [SetUp]
        public void SetUp()
        {
            _mockUserStore = new Mock<IUserStore>();
            _mockAuditLogger = new Mock<AuditLogger>(Mock.Of<Core.Configuration.LoggingConfig>());
            _provider = new SimpleFtpAuthenticationProvider(_mockUserStore.Object, _mockAuditLogger.Object);
        }

        [Test]
        public void Constructor_NullUserStore_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var action = () => new SimpleFtpAuthenticationProvider(null!, _mockAuditLogger.Object);
            Assert.That(action, Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("userStore"));
        }

        [Test]
        public void Constructor_NullAuditLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var action = () => new SimpleFtpAuthenticationProvider(_mockUserStore.Object, null!);
            Assert.That(action, Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("auditLogger"));
        }

        [Test]
        public void Constructor_ValidParameters_ShouldCreateInstance()
        {
            // Act & Assert
            var provider = new SimpleFtpAuthenticationProvider(_mockUserStore.Object, _mockAuditLogger.Object);
            Assert.That(provider, Is.Not.Null);
        }

        [Test]
        public void ParameterlessConstructor_ShouldCreateInstance()
        {
            // Act & Assert - Should not throw
            var provider = new SimpleFtpAuthenticationProvider();
            Assert.That(provider, Is.Not.Null);
        }

        [Test]
        public void AuthenticateUser_ValidCredentials_ShouldReturnTrueAndLogSuccess()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var userName = "testuser";
            var userPassword = "password123";
            
            _mockUserStore.Setup(x => x.Validate(userName, userPassword)).Returns(true);

            // Act
            var result = _provider.AuthenticateUser(sessionId, siteName, userName, userPassword, out var canonicalUserName);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(canonicalUserName, Is.EqualTo(userName));
            
            _mockUserStore.Verify(x => x.Validate(userName, userPassword), Times.Once);
            _mockAuditLogger.Verify(x => x.LogAuthenticationSuccess(sessionId, siteName, userName, It.IsAny<string>()), Times.Once);
            _mockAuditLogger.Verify(x => x.LogAuthenticationFailure(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void AuthenticateUser_InvalidCredentials_ShouldReturnFalseAndLogFailure()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var userName = "testuser";
            var userPassword = "wrongpassword";
            
            _mockUserStore.Setup(x => x.Validate(userName, userPassword)).Returns(false);

            // Act
            var result = _provider.AuthenticateUser(sessionId, siteName, userName, userPassword, out var canonicalUserName);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(canonicalUserName, Is.EqualTo(userName));
            
            _mockUserStore.Verify(x => x.Validate(userName, userPassword), Times.Once);
            _mockAuditLogger.Verify(x => x.LogAuthenticationFailure(sessionId, siteName, userName, "Invalid credentials", It.IsAny<string>()), Times.Once);
            _mockAuditLogger.Verify(x => x.LogAuthenticationSuccess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void AuthenticateUser_UserStoreThrowsException_ShouldReturnFalseAndLogFailure()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var userName = "testuser";
            var userPassword = "password123";
            var exception = new InvalidOperationException("Database connection failed");
            
            _mockUserStore.Setup(x => x.Validate(userName, userPassword)).Throws(exception);

            // Act
            var result = _provider.AuthenticateUser(sessionId, siteName, userName, userPassword, out var canonicalUserName);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(canonicalUserName, Is.EqualTo(userName));
            
            _mockUserStore.Verify(x => x.Validate(userName, userPassword), Times.Once);
            _mockAuditLogger.Verify(x => x.LogAuthenticationFailure(sessionId, siteName, userName, $"Authentication error: {exception.Message}", It.IsAny<string>()), Times.Once);
            _mockAuditLogger.Verify(x => x.LogAuthenticationSuccess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void AuthenticateUser_EmptyOrNullUserName_ShouldStillCallUserStoreAndSetCanonicalName(string userName)
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var userPassword = "password123";
            
            _mockUserStore.Setup(x => x.Validate(userName, userPassword)).Returns(false);

            // Act
            var result = _provider.AuthenticateUser(sessionId, siteName, userName, userPassword, out var canonicalUserName);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(canonicalUserName, Is.EqualTo(userName));
            
            _mockUserStore.Verify(x => x.Validate(userName, userPassword), Times.Once);
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void AuthenticateUser_EmptyOrNullPassword_ShouldStillCallUserStore(string userPassword)
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var userName = "testuser";
            
            _mockUserStore.Setup(x => x.Validate(userName, userPassword)).Returns(false);

            // Act
            var result = _provider.AuthenticateUser(sessionId, siteName, userName, userPassword, out var canonicalUserName);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(canonicalUserName, Is.EqualTo(userName));
            
            _mockUserStore.Verify(x => x.Validate(userName, userPassword), Times.Once);
        }

        [Test]
        public void AuthenticateUser_EmptySessionId_ShouldStillWork()
        {
            // Arrange
            var sessionId = "";
            var siteName = "TestSite";
            var userName = "testuser";
            var userPassword = "password123";
            
            _mockUserStore.Setup(x => x.Validate(userName, userPassword)).Returns(true);

            // Act
            var result = _provider.AuthenticateUser(sessionId, siteName, userName, userPassword, out var canonicalUserName);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(canonicalUserName, Is.EqualTo(userName));
            
            _mockAuditLogger.Verify(x => x.LogAuthenticationSuccess(sessionId, siteName, userName, It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void AuthenticateUser_EmptySiteName_ShouldStillWork()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "";
            var userName = "testuser";
            var userPassword = "password123";
            
            _mockUserStore.Setup(x => x.Validate(userName, userPassword)).Returns(true);

            // Act
            var result = _provider.AuthenticateUser(sessionId, siteName, userName, userPassword, out var canonicalUserName);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(canonicalUserName, Is.EqualTo(userName));
            
            _mockAuditLogger.Verify(x => x.LogAuthenticationSuccess(sessionId, siteName, userName, It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void AuthenticateUser_SpecialCharactersInUserName_ShouldWork()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var userName = "test@domain.com";
            var userPassword = "password123";
            
            _mockUserStore.Setup(x => x.Validate(userName, userPassword)).Returns(true);

            // Act
            var result = _provider.AuthenticateUser(sessionId, siteName, userName, userPassword, out var canonicalUserName);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(canonicalUserName, Is.EqualTo(userName));
            
            _mockUserStore.Verify(x => x.Validate(userName, userPassword), Times.Once);
            _mockAuditLogger.Verify(x => x.LogAuthenticationSuccess(sessionId, siteName, userName, It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void AuthenticateUser_LongUserName_ShouldWork()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var userName = new string('a', 256); // Very long username
            var userPassword = "password123";
            
            _mockUserStore.Setup(x => x.Validate(userName, userPassword)).Returns(true);

            // Act
            var result = _provider.AuthenticateUser(sessionId, siteName, userName, userPassword, out var canonicalUserName);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(canonicalUserName, Is.EqualTo(userName));
            
            _mockUserStore.Verify(x => x.Validate(userName, userPassword), Times.Once);
        }

        [Test]
        public void AuthenticateUser_CaseVariationsInUserName_ShouldPassExactValueToUserStore()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var userName = "TestUser";
            var userPassword = "password123";
            
            _mockUserStore.Setup(x => x.Validate(userName, userPassword)).Returns(true);

            // Act
            var result = _provider.AuthenticateUser(sessionId, siteName, userName, userPassword, out var canonicalUserName);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(canonicalUserName, Is.EqualTo(userName));
            
            // Verify exact case is passed to user store
            _mockUserStore.Verify(x => x.Validate(userName, userPassword), Times.Once);
        }
    }
} 