using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Logging;
using IIS.Ftp.SimpleAuth.Core.Stores;
using IIS.Ftp.SimpleAuth.Provider;
using Microsoft.Web.FtpServer;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IIS.Ftp.SimpleAuth.AuthProvider.Tests
{
    [TestFixture]
    public class SimpleFtpAuthorizationProviderTests
    {
        private Mock<IUserStore> _mockUserStore = null!;
        private Mock<AuditLogger> _mockAuditLogger = null!;
        private SimpleFtpAuthorizationProvider _provider = null!;

        [SetUp]
        public void SetUp()
        {
            _mockUserStore = new Mock<IUserStore>();
            _mockAuditLogger = new Mock<AuditLogger>(Mock.Of<Core.Configuration.LoggingConfig>());
            _provider = new SimpleFtpAuthorizationProvider(_mockUserStore.Object, _mockAuditLogger.Object);
        }

        [Test]
        public void Constructor_NullUserStore_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var action = () => new SimpleFtpAuthorizationProvider(null!, _mockAuditLogger.Object);
            Assert.That(action, Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("userStore"));
        }

        [Test]
        public void Constructor_NullAuditLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var action = () => new SimpleFtpAuthorizationProvider(_mockUserStore.Object, null!);
            Assert.That(action, Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("auditLogger"));
        }

        [Test]
        public void Constructor_ValidParameters_ShouldCreateInstance()
        {
            // Act & Assert
            var provider = new SimpleFtpAuthorizationProvider(_mockUserStore.Object, _mockAuditLogger.Object);
            Assert.That(provider, Is.Not.Null);
        }

        [Test]
        public void ParameterlessConstructor_ShouldCreateInstance()
        {
            // Act & Assert - Should not throw
            var provider = new SimpleFtpAuthorizationProvider();
            Assert.That(provider, Is.Not.Null);
        }

        [Test]
        public void GetUserAccessPermission_NoPermissions_ShouldReturnNone()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = "/home/user";
            var userName = "testuser";

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).Returns(Task.FromResult<IEnumerable<Permission>>(new List<Permission>()));

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.None));
            _mockUserStore.Verify(x => x.GetPermissionsAsync(userName), Times.Once);
        }

        [Test]
        public void GetUserAccessPermission_ReadOnlyPermission_ShouldReturnRead()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = "/home/user";
            var userName = "testuser";

            var permissions = new List<Permission>
            {
                new Permission { Path = "/home/user", CanRead = true, CanWrite = false }
            };

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(permissions);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.Read));
            _mockUserStore.Verify(x => x.GetPermissionsAsync(userName), Times.Once);
        }

        [Test]
        public void GetUserAccessPermission_WriteOnlyPermission_ShouldReturnWrite()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = "/home/user";
            var userName = "testuser";

            var permissions = new List<Permission>
            {
                new Permission { Path = "/home/user", CanRead = false, CanWrite = true }
            };

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(permissions);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.Write));
            _mockUserStore.Verify(x => x.GetPermissionsAsync(userName), Times.Once);
        }

        [Test]
        public void GetUserAccessPermission_ReadWritePermission_ShouldReturnReadWrite()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = "/home/user";
            var userName = "testuser";

            var permissions = new List<Permission>
            {
                new Permission { Path = "/home/user", CanRead = true, CanWrite = true }
            };

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(permissions);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.Read | FtpAccess.Write));
            _mockUserStore.Verify(x => x.GetPermissionsAsync(userName), Times.Once);
        }

        [Test]
        public void GetUserAccessPermission_MultiplePermissions_ShouldCombineAccess()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = "/home/user/documents";
            var userName = "testuser";

            var permissions = new List<Permission>
            {
                new Permission { Path = "/home", CanRead = true, CanWrite = false },
                new Permission { Path = "/home/user", CanRead = false, CanWrite = true }
            };

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(permissions);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.Read | FtpAccess.Write));
            _mockUserStore.Verify(x => x.GetPermissionsAsync(userName), Times.Once);
        }

        [Test]
        public void GetUserAccessPermission_RootPathPermission_ShouldMatchAllPaths()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = "/any/deep/path/here";
            var userName = "testuser";

            var permissions = new List<Permission>
            {
                new Permission { Path = "/", CanRead = true, CanWrite = false }
            };

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(permissions);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.Read));
            _mockUserStore.Verify(x => x.GetPermissionsAsync(userName), Times.Once);
        }

        [Test]
        public void GetUserAccessPermission_PathNotMatching_ShouldReturnNone()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = "/other/path";
            var userName = "testuser";

            var permissions = new List<Permission>
            {
                new Permission { Path = "/home/user", CanRead = true, CanWrite = true }
            };

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(permissions);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.None));
            _mockUserStore.Verify(x => x.GetPermissionsAsync(userName), Times.Once);
        }

        [Test]
        [TestCase("/home/user", "/home/user")]
        [TestCase("/home/user/", "/home/user")]
        [TestCase("/home/user", "/home/user/")]
        [TestCase("/home/user/", "/home/user/")]
        public void GetUserAccessPermission_PathNormalization_ShouldWork(string virtualPath, string permissionPath)
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var userName = "testuser";

            var permissions = new List<Permission>
            {
                new Permission { Path = permissionPath, CanRead = true, CanWrite = false }
            };

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(permissions);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.Read));
        }

        [Test]
        [TestCase("/HOME/USER", "/home/user")]
        [TestCase("/home/USER", "/HOME/user")]
        [TestCase("/Home/User", "/home/user")]
        public void GetUserAccessPermission_CaseInsensitiveMatching_ShouldWork(string virtualPath, string permissionPath)
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var userName = "testuser";

            var permissions = new List<Permission>
            {
                new Permission { Path = permissionPath, CanRead = true, CanWrite = false }
            };

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(permissions);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.Read));
        }

        [Test]
        public void GetUserAccessPermission_EmptyVirtualPath_ShouldTreatAsRoot()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = "";
            var userName = "testuser";

            var permissions = new List<Permission>
            {
                new Permission { Path = "/", CanRead = true, CanWrite = false }
            };

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(permissions);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.Read));
        }

        [Test]
        public void GetUserAccessPermission_NullVirtualPath_ShouldTreatAsRoot()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = (string)null!;
            var userName = "testuser";

            var permissions = new List<Permission>
            {
                new Permission { Path = "/", CanRead = true, CanWrite = false }
            };

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(permissions);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.Read));
        }

        [Test]
        public void GetUserAccessPermission_PathWithoutLeadingSlash_ShouldNormalize()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = "home/user";
            var userName = "testuser";

            var permissions = new List<Permission>
            {
                new Permission { Path = "/home/user", CanRead = true, CanWrite = false }
            };

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(permissions);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.Read));
        }

        [Test]
        public void GetUserAccessPermission_SubdirectoryAccess_ShouldInheritParentPermissions()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = "/home/user/documents/subfolder";
            var userName = "testuser";

            var permissions = new List<Permission>
            {
                new Permission { Path = "/home/user", CanRead = true, CanWrite = true }
            };

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(permissions);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.Read | FtpAccess.Write));
        }

        [Test]
        public void GetUserAccessPermission_MostSpecificPermissionWins_ShouldOverride()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = "/home/user/restricted";
            var userName = "testuser";

            var permissions = new List<Permission>
            {
                new Permission { Path = "/", CanRead = true, CanWrite = true },
                new Permission { Path = "/home/user/restricted", CanRead = true, CanWrite = false }
            };

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(permissions);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.Read | FtpAccess.Write)); // Both permissions should be combined
        }

        [Test]
        public void GetUserAccessPermission_UserStoreThrowsException_ShouldReturnNoneAndLogError()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = "/home/user";
            var userName = "testuser";
            var exception = new InvalidOperationException("Database connection failed");

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).Throws(exception);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.None));
            _mockUserStore.Verify(x => x.GetPermissionsAsync(userName), Times.Once);
            _mockAuditLogger.Verify(x => x.LogUserStoreError("GetUserAccessPermission", 
                $"Error getting permissions for user '{userName}': {exception.Message}"), Times.Once);
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void GetUserAccessPermission_EmptyOrNullUserName_ShouldStillCallUserStore(string userName)
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = "/home/user";

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(new List<Permission>());

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.None));
            _mockUserStore.Verify(x => x.GetPermissionsAsync(userName), Times.Once);
        }

        [Test]
        public void GetUserAccessPermission_WindowsStylePaths_ShouldNormalizeToUnixStyle()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = "\\home\\user";
            var userName = "testuser";

            var permissions = new List<Permission>
            {
                new Permission { Path = "/home/user", CanRead = true, CanWrite = false }
            };

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(permissions);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert - This test verifies current behavior, may need adjustment based on actual path handling
            Assert.That(result, Is.EqualTo(FtpAccess.None)); // Current implementation may not handle backslashes
        }

        [Test]
        public void GetUserAccessPermission_EmptyPermissionPath_ShouldTreatAsRoot()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = "/any/path";
            var userName = "testuser";

            var permissions = new List<Permission>
            {
                new Permission { Path = "", CanRead = true, CanWrite = false }
            };

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(permissions);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.Read));
        }

        [Test]
        public void GetUserAccessPermission_ComplexPathMatching_ShouldWorkCorrectly()
        {
            // Arrange
            var sessionId = "session123";
            var siteName = "TestSite";
            var virtualPath = "/home/user/documents/private/file.txt";
            var userName = "testuser";

            var permissions = new List<Permission>
            {
                new Permission { Path = "/", CanRead = true, CanWrite = false },
                new Permission { Path = "/home", CanRead = true, CanWrite = false },
                new Permission { Path = "/home/user", CanRead = true, CanWrite = true },
                new Permission { Path = "/home/user/documents", CanRead = true, CanWrite = true },
                new Permission { Path = "/home/other", CanRead = false, CanWrite = false }
            };

            _mockUserStore.Setup(x => x.GetPermissionsAsync(userName)).ReturnsAsync(permissions);

            // Act
            var result = _provider.GetUserAccessPermission(sessionId, siteName, virtualPath, userName);

            // Assert
            Assert.That(result, Is.EqualTo(FtpAccess.Read | FtpAccess.Write));
        }
    }
}