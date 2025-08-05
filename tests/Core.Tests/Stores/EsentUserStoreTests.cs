using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Security;
using IIS.Ftp.SimpleAuth.Core.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IIS.Ftp.SimpleAuth.Core.Tests.Stores
{
    [TestClass]
    public class EsentUserStoreTests
    {
        private string _tempDataDirectory = null!;
        private EsentUserStore _store = null!;

        [TestInitialize]
        public void SetUp()
        {
            _tempDataDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDataDirectory);
        }

        [TestCleanup]
        public void TearDown()
        {
            _store?.Dispose();
            if (Directory.Exists(_tempDataDirectory))
            {
                try
                {
                    Directory.Delete(_tempDataDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [TestMethod]
        public async Task Constructor_ShouldCreateStore()
        {
            // Act
            _store = new EsentUserStore(_tempDataDirectory);
            var users = await _store.GetAllUsersAsync();

            // Assert
            Assert.IsNotNull(users);
            Assert.AreEqual(0, users.Count());
        }

        [TestMethod]
        public async Task SaveUserAsync_ShouldPersistUser()
        {
            // Arrange
            _store = new EsentUserStore(_tempDataDirectory);
            var user = new User
            {
                UserId = "testuser",
                DisplayName = "Test User",
                Salt = PasswordHasher.GenerateSalt(),
                PasswordHash = PasswordHasher.HashPasswordBCrypt("password123"),
                HomeDirectory = "/home/testuser",
                Permissions = new List<Permission>
                {
                    new Permission { Path = "/home/testuser", CanRead = true, CanWrite = true }
                }
            };

            // Act
            await _store.SaveUserAsync(user);
            var foundUser = await _store.FindAsync("testuser");

            // Assert
            Assert.IsNotNull(foundUser);
            Assert.AreEqual("testuser", foundUser!.UserId);
            Assert.AreEqual("Test User", foundUser.DisplayName);
            Assert.AreEqual("/home/testuser", foundUser.HomeDirectory);
            Assert.AreEqual(1, foundUser.Permissions.Count);
            Assert.AreEqual("/home/testuser", foundUser.Permissions.First().Path);
        }

        [TestMethod]
        public async Task ValidateAsync_WithValidCredentials_ShouldReturnTrue()
        {
            // Arrange
            _store = new EsentUserStore(_tempDataDirectory);
            var password = "password123";
            var user = new User
            {
                UserId = "testuser",
                Salt = PasswordHasher.GenerateSalt(),
                PasswordHash = PasswordHasher.HashPasswordBCrypt(password)
            };
            await _store.SaveUserAsync(user);

            // Act
            bool isValid = await _store.ValidateAsync("testuser", password);

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public async Task ValidateAsync_WithInvalidCredentials_ShouldReturnFalse()
        {
            // Arrange
            _store = new EsentUserStore(_tempDataDirectory);
            var user = new User
            {
                UserId = "testuser",
                Salt = PasswordHasher.GenerateSalt(),
                PasswordHash = PasswordHasher.HashPasswordBCrypt("password123")
            };
            await _store.SaveUserAsync(user);

            // Act
            bool isValid = await _store.ValidateAsync("testuser", "wrongpassword");

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public async Task DeleteUserAsync_ShouldRemoveUser()
        {
            // Arrange
            _store = new EsentUserStore(_tempDataDirectory);
            var user = new User
            {
                UserId = "testuser",
                DisplayName = "Test User"
            };
            await _store.SaveUserAsync(user);

            // Act
            await _store.DeleteUserAsync("testuser");
            var foundUser = await _store.FindAsync("testuser");

            // Assert
            Assert.IsNull(foundUser);
        }

        [TestMethod]
        public async Task GetAllUsersAsync_ShouldReturnAllUsers()
        {
            // Arrange
            _store = new EsentUserStore(_tempDataDirectory);
            var user1 = new User { UserId = "user1", DisplayName = "User One" };
            var user2 = new User { UserId = "user2", DisplayName = "User Two" };
            
            await _store.SaveUserAsync(user1);
            await _store.SaveUserAsync(user2);

            // Act
            var users = await _store.GetAllUsersAsync();

            // Assert
            Assert.AreEqual(2, users.Count());
            Assert.IsTrue(users.Any(u => u.UserId == "user1"));
            Assert.IsTrue(users.Any(u => u.UserId == "user2"));
        }

        [TestMethod]
        public async Task AddPermissionAsync_ShouldAddPermission()
        {
            // Arrange
            _store = new EsentUserStore(_tempDataDirectory);
            var user = new User { UserId = "testuser", DisplayName = "Test User" };
            await _store.SaveUserAsync(user);
            var permission = new Permission { Path = "/shared", CanRead = true, CanWrite = false };

            // Act
            await _store.AddPermissionAsync("testuser", permission);
            var permissions = await _store.GetPermissionsAsync("testuser");

            // Assert
            Assert.AreEqual(1, permissions.Count());
            var addedPermission = permissions.First();
            Assert.AreEqual("/shared", addedPermission.Path);
            Assert.IsTrue(addedPermission.CanRead);
            Assert.IsFalse(addedPermission.CanWrite);
        }
    }
}