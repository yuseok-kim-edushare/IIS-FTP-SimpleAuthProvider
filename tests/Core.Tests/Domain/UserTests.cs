using IIS.Ftp.SimpleAuth.Core.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace IIS.Ftp.SimpleAuth.Core.Tests.Domain
{
    [TestClass]
    public class UserTests
    {
        [TestMethod]
        public void User_DefaultConstructor_ShouldInitializePropertiesWithDefaultValues()
        {
            // Act
            var user = new User();

            // Assert
            Assert.AreEqual(string.Empty, user.UserId);
            Assert.AreEqual(string.Empty, user.DisplayName);
            Assert.AreEqual(string.Empty, user.Salt);
            Assert.AreEqual(string.Empty, user.PasswordHash);
            Assert.AreEqual(string.Empty, user.HomeDirectory);
            Assert.IsNotNull(user.Permissions);
            Assert.That(user.Permissions, Is.Empty);
        }

        [TestMethod]
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
            Assert.AreEqual(expectedUserId, user.UserId);
            Assert.AreEqual(expectedDisplayName, user.DisplayName);
            Assert.AreEqual(expectedSalt, user.Salt);
            Assert.AreEqual(expectedPasswordHash, user.PasswordHash);
            Assert.AreEqual(expectedHomeDirectory, user.HomeDirectory);
            Assert.That(user.Permissions, Is.EquivalentTo(expectedPermissions));
        }

        [TestMethod]
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