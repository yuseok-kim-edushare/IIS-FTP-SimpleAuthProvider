using IIS.Ftp.SimpleAuth.Core.Domain;
using NUnit.Framework;

namespace IIS.Ftp.SimpleAuth.Core.Tests.Domain
{
    [TestFixture]
    public class PermissionTests
    {
        [Test]
        public void Permission_DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var permission = new Permission();

            // Assert
            Assert.That(permission.Path, Is.EqualTo("/"));
            Assert.That(permission.CanRead, Is.False);
            Assert.That(permission.CanWrite, Is.False);
        }

        [Test]
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
            Assert.That(permission.Path, Is.EqualTo(expectedPath));
            Assert.That(permission.CanRead, Is.EqualTo(expectedCanRead));
            Assert.That(permission.CanWrite, Is.EqualTo(expectedCanWrite));
        }

        [Test]
        public void Permission_CanRead_ShouldToggle()
        {
            // Arrange
            var permission = new Permission();

            // Act & Assert
            Assert.That(permission.CanRead, Is.False);
            
            permission.CanRead = true;
            Assert.That(permission.CanRead, Is.True);
            
            permission.CanRead = false;
            Assert.That(permission.CanRead, Is.False);
        }

        [Test]
        public void Permission_CanWrite_ShouldToggle()
        {
            // Arrange
            var permission = new Permission();

            // Act & Assert
            Assert.That(permission.CanWrite, Is.False);
            
            permission.CanWrite = true;
            Assert.That(permission.CanWrite, Is.True);
            
            permission.CanWrite = false;
            Assert.That(permission.CanWrite, Is.False);
        }

        [Test]
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
                Assert.That(permission.Path, Is.EqualTo(path));
            }
        }
    }
} 
