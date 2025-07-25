using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Security;
using IIS.Ftp.SimpleAuth.Core.Stores;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IIS.Ftp.SimpleAuth.Core.Tests.Stores
{
    [TestFixture]
    public class EsentUserStoreTests
    {
        private string _tempDataDirectory = null!;
        private EsentUserStore _store = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDataDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDataDirectory);
        }

        [TearDown]
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

        [Test]
        public async Task Constructor_ShouldCreateStore()
        {
            // Act
            _store = new EsentUserStore(_tempDataDirectory);
            var users = await _store.GetAllUsersAsync();

            // Assert
            Assert.That(users, Is.Not.Null);
            Assert.That(users.Count(), Is.EqualTo(0));
        }

        [Test]
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
            Assert.That(foundUser, Is.Not.Null);
            Assert.That(foundUser!.UserId, Is.EqualTo("testuser"));
            Assert.That(foundUser.DisplayName, Is.EqualTo("Test User"));
            Assert.That(foundUser.HomeDirectory, Is.EqualTo("/home/testuser"));
            Assert.That(foundUser.Permissions.Count, Is.EqualTo(1));
            Assert.That(foundUser.Permissions.First().Path, Is.EqualTo("/home/testuser"));
        }

        [Test]
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
            Assert.That(isValid, Is.True);
        }

        [Test]
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
            Assert.That(isValid, Is.False);
        }

        [Test]
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
            Assert.That(foundUser, Is.Null);
        }

        [Test]
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
            Assert.That(users.Count(), Is.EqualTo(2));
            Assert.That(users.Any(u => u.UserId == "user1"), Is.True);
            Assert.That(users.Any(u => u.UserId == "user2"), Is.True);
        }

        [Test]
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
            Assert.That(permissions.Count(), Is.EqualTo(1));
            var addedPermission = permissions.First();
            Assert.That(addedPermission.Path, Is.EqualTo("/shared"));
            Assert.That(addedPermission.CanRead, Is.True);
            Assert.That(addedPermission.CanWrite, Is.False);
        }
    }
}