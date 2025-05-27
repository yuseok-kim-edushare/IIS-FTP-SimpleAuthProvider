using IIS.Ftp.SimpleAuth.Core.Domain;
using NUnit.Framework;
using System.Collections.Generic;

namespace IIS.Ftp.SimpleAuth.Core.Tests.Domain
{
    [TestFixture]
    public class UserTests
    {
        [Test]
        public void User_DefaultConstructor_ShouldInitializePropertiesWithDefaultValues()
        {
            // Act
            var user = new User();

            // Assert
            Assert.That(user.UserId, Is.EqualTo(string.Empty));
            Assert.That(user.DisplayName, Is.EqualTo(string.Empty));
            Assert.That(user.Salt, Is.EqualTo(string.Empty));
            Assert.That(user.PasswordHash, Is.EqualTo(string.Empty));
            Assert.That(user.HomeDirectory, Is.EqualTo(string.Empty));
            Assert.That(user.Permissions, Is.Not.Null);
            Assert.That(user.Permissions, Is.Empty);
        }

        [Test]
        public void User_Properties_ShouldBeSettable()
        {
            // Arrange
            var expectedUserId = "testuser";
            var expectedDisplayName = "Test User";
            var expectedSalt = "testsalt";
            var expectedPasswordHash = "testhash";
            var expectedHomeDirectory = "/home/testuser";
            var expectedPermissions = new List<Permission>
            {
                new Permission { Path = "/home/testuser", CanRead = true, CanWrite = true }
            };

            // Act
            var user = new User
            {
                UserId = expectedUserId,
                DisplayName = expectedDisplayName,
                Salt = expectedSalt,
                PasswordHash = expectedPasswordHash,
                HomeDirectory = expectedHomeDirectory,
                Permissions = expectedPermissions
            };

            // Assert
            Assert.That(user.UserId, Is.EqualTo(expectedUserId));
            Assert.That(user.DisplayName, Is.EqualTo(expectedDisplayName));
            Assert.That(user.Salt, Is.EqualTo(expectedSalt));
            Assert.That(user.PasswordHash, Is.EqualTo(expectedPasswordHash));
            Assert.That(user.HomeDirectory, Is.EqualTo(expectedHomeDirectory));
            Assert.That(user.Permissions, Is.EquivalentTo(expectedPermissions));
        }

        [Test]
        public void User_Permissions_ShouldBeModifiable()
        {
            // Arrange
            var user = new User();
            var permission1 = new Permission { Path = "/path1", CanRead = true, CanWrite = false };
            var permission2 = new Permission { Path = "/path2", CanRead = false, CanWrite = true };

            // Act
            user.Permissions.Add(permission1);
            user.Permissions.Add(permission2);

            // Assert
            Assert.That(user.Permissions, Has.Count.EqualTo(2));
            Assert.That(user.Permissions, Contains.Item(permission1));
            Assert.That(user.Permissions, Contains.Item(permission2));
        }
    }
} 