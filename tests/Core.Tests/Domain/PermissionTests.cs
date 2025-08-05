using IIS.Ftp.SimpleAuth.Core.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IIS.Ftp.SimpleAuth.Core.Tests.Domain
{
    [TestClass]
    public class PermissionTests
    {
        [TestMethod]
        public void Permission_DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var permission = new Permission();

            // Assert
            Assert.AreEqual("/", permission.Path);
            Assert.IsFalse(permission.CanRead);
            Assert.IsFalse(permission.CanWrite);
        }

        [TestMethod]
        public void Permission_Properties_ShouldBeSettable()
        {
            // Arrange
            var expectedPath = "/home/user/documents";
            var expectedCanRead = true;
            var expectedCanWrite = true;

            // Act
            var permission = new Permission
            {
                Path = expectedPath,
                CanRead = expectedCanRead,
                CanWrite = expectedCanWrite
            };

            // Assert
            Assert.AreEqual(expectedPath, permission.Path);
            Assert.AreEqual(expectedCanRead, permission.CanRead);
            Assert.AreEqual(expectedCanWrite, permission.CanWrite);
        }

        [TestMethod]
        public void Permission_CanRead_ShouldToggle()
        {
            // Arrange
            var permission = new Permission();

            // Act & Assert
            Assert.IsFalse(permission.CanRead);
            
            permission.CanRead = true;
            Assert.IsTrue(permission.CanRead);
            
            permission.CanRead = false;
            Assert.IsFalse(permission.CanRead);
        }

        [TestMethod]
        public void Permission_CanWrite_ShouldToggle()
        {
            // Arrange
            var permission = new Permission();

            // Act & Assert
            Assert.IsFalse(permission.CanWrite);
            
            permission.CanWrite = true;
            Assert.IsTrue(permission.CanWrite);
            
            permission.CanWrite = false;
            Assert.IsFalse(permission.CanWrite);
        }

        [TestMethod]
        public void Permission_Path_ShouldAcceptVariousFormats()
        {
            // Arrange
            var permission = new Permission();
            var testPaths = new[]
            {
                "/",
                "/home",
                "/home/user",
                "C:\\Users\\TestUser",
                "\\\\server\\share",
                "/var/www/html"
            };

            // Act & Assert
            foreach (var path in testPaths)
            {
                permission.Path = path;
                Assert.AreEqual(path, permission.Path);
            }
        }
    }
} 