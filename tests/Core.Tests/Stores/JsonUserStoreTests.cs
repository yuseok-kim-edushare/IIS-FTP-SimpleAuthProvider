using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Security;
using IIS.Ftp.SimpleAuth.Core.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IIS.Ftp.SimpleAuth.Core.Tests.Stores
{
    [TestClass]
    public class JsonUserStoreTests
    {
        private string _tempFilePath = null!;
        private JsonUserStore _store = null!;

        [TestInitialize]
        public void SetUp()
        {
            _tempFilePath = Path.GetTempFileName();
        }

        [TestCleanup]
        public void TearDown()
        {
            _store?.Dispose();
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }

        [TestMethod]
        public async Task Constructor_NonExistentFile_ShouldCreateEmptyStore()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");

            // Act
            _store = new JsonUserStore(nonExistentPath, enableHotReload: false);
            User? foundUser = await _store.FindAsync("anyuser");

            // Assert
            Assert.IsNull(foundUser);
        }

        [TestMethod]
        public async Task Constructor_ValidJsonFile_ShouldLoadUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User
                {
                    UserId = "testuser",
                    DisplayName = "Test User",
                    Salt = PasswordHasher.GenerateSalt(),
                    PasswordHash = PasswordHasher.HashPassword("password123", PasswordHasher.GenerateSalt()),
                    HomeDirectory = "/home/testuser",
                    Permissions = new List<Permission>
                    {
                        new Permission { Path = "/home/testuser", CanRead = true, CanWrite = true }
                    }
                }
            };

            CreateTestJsonFile(users);

            // Act
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);
            var foundUser = await _store.FindAsync("testuser");

            // Assert
            Assert.IsNotNull(foundUser);
            Assert.AreEqual("testuser", foundUser!.UserId);
            Assert.AreEqual("Test User", foundUser.DisplayName);
        }

        [TestMethod]
        public async Task Find_ExistingUser_ShouldReturnUser()
        {
            // Arrange
            var user = CreateTestUser("testuser", "Test User");
            CreateTestJsonFile(new List<User> { user });
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Act
            var foundUser = await _store.FindAsync("testuser");

            // Assert
            Assert.IsNotNull(foundUser);
            Assert.AreEqual("testuser", foundUser!.UserId);
            Assert.AreEqual("Test User", foundUser.DisplayName);
        }

        [TestMethod]
        public async Task Find_NonExistentUser_ShouldReturnNull()
        {
            // Arrange
            CreateTestJsonFile(new List<User>());
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Act
            var foundUser = await _store.FindAsync("nonexistent");

            // Assert
            Assert.IsNull(foundUser);
        }

        [TestMethod]
        public async Task Find_CaseInsensitive_ShouldReturnUser()
        {
            // Arrange
            var user = CreateTestUser("TestUser", "Test User");
            CreateTestJsonFile(new List<User> { user });
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Act
            var foundUser1 = await _store.FindAsync("testuser");
            var foundUser2 = await _store.FindAsync("TESTUSER");
            var foundUser3 = await _store.FindAsync("TestUser");

            // Assert
            Assert.IsNotNull(foundUser1);
            Assert.IsNotNull(foundUser2);
            Assert.IsNotNull(foundUser3);
            
            Assert.AreEqual("TestUser", foundUser1!.UserId);
            Assert.AreEqual("TestUser", foundUser2!.UserId);
            Assert.AreEqual("TestUser", foundUser3!.UserId);
        }

        [TestMethod]
        public async Task Validate_CorrectPassword_ShouldReturnTrue()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPassword(password, salt);
            
            var user = new User
            {
                UserId = "testuser",
                Salt = salt,
                PasswordHash = hash
            };

            CreateTestJsonFile(new List<User> { user });
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Act
            var isValid = await _store.ValidateAsync("testuser", password);

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public async Task Validate_IncorrectPassword_ShouldReturnFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var wrongPassword = "WrongPassword456!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPassword(password, salt);
            
            var user = new User
            {
                UserId = "testuser",
                Salt = salt,
                PasswordHash = hash
            };

            CreateTestJsonFile(new List<User> { user });
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Act
            var isValid = await _store.ValidateAsync("testuser", wrongPassword);

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public async Task Validate_NonExistentUser_ShouldReturnFalse()
        {
            // Arrange
            CreateTestJsonFile(new List<User>());
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Act
            var isValid = await _store.ValidateAsync("nonexistent", "anypassword");

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public async Task GetPermissions_ExistingUser_ShouldReturnPermissions()
        {
            // Arrange
            var permissions = new List<Permission>
            {
                new Permission { Path = "/home/testuser", CanRead = true, CanWrite = true },
                new Permission { Path = "/shared", CanRead = true, CanWrite = false }
            };

            var user = CreateTestUser("testuser", "Test User");
            user.Permissions = permissions;

            CreateTestJsonFile(new List<User> { user });
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Act
            var userPermissions = (await _store.GetPermissionsAsync("testuser")).ToList();

            // Assert
            Assert.That(userPermissions, Has.Count.EqualTo(2));
            Assert.That(userPermissions, Is.EquivalentTo(permissions));
        }

        [TestMethod]
        public async Task GetPermissions_NonExistentUser_ShouldReturnEmpty()
        {
            // Arrange
            CreateTestJsonFile(new List<User>());
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Act
            var permissions = (await _store.GetPermissionsAsync("nonexistent")).ToList();

            // Assert
            Assert.That(permissions, Is.Empty);
        }

        [TestMethod]
        public async Task GetPermissions_UserWithNoPermissions_ShouldReturnEmpty()
        {
            // Arrange
            var user = CreateTestUser("testuser", "Test User");
            user.Permissions = new List<Permission>();

            CreateTestJsonFile(new List<User> { user });
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Act
            var permissions = (await _store.GetPermissionsAsync("testuser")).ToList();

            // Assert
            Assert.That(permissions, Is.Empty);
        }

        [TestMethod]
        public async Task Constructor_InvalidJsonFile_ShouldCreateEmptyStore()
        {
            // Arrange
            File.WriteAllText(_tempFilePath, "invalid json content");

            // Act
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Assert
            var foundUser = await _store.FindAsync("anyuser");
            Assert.IsNull(foundUser);
        }

        [TestMethod]
        public async Task Find_MultipleUsers_ShouldReturnCorrectUser()
        {
            // Arrange
            var users = new List<User>
            {
                CreateTestUser("user1", "User One"),
                CreateTestUser("user2", "User Two"),
                CreateTestUser("user3", "User Three")
            };

            CreateTestJsonFile(users);
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Act
            var user1 = await _store.FindAsync("user1");
            var user2 = await _store.FindAsync("user2");
            var user3 = await _store.FindAsync("user3");

            // Assert
            Assert.IsNotNull(user1);
            Assert.AreEqual("User One", user1!.DisplayName);
            
            Assert.IsNotNull(user2);
            Assert.AreEqual("User Two", user2!.DisplayName);
            
            Assert.IsNotNull(user3);
            Assert.AreEqual("User Three", user3!.DisplayName);
        }

        [TestMethod]
        public async Task HotReload_FileModification_ShouldReloadUsers()
        {
            // Arrange
            var initialUsers = new List<User> { CreateTestUser("user1", "User One") };
            CreateTestJsonFile(initialUsers);
            _store = new JsonUserStore(_tempFilePath, enableHotReload: true);

            // Verify initial state
            var user1Initial = await _store.FindAsync("user1");
            Assert.IsNotNull(user1Initial);
            var user2Initial = await _store.FindAsync("user2");
            Assert.IsNull(user2Initial);

            // Act - Modify file
            var updatedUsers = new List<User>
            {
                CreateTestUser("user1", "User One Updated"),
                CreateTestUser("user2", "User Two")
            };
            CreateTestJsonFile(updatedUsers);

            // Wait for file watcher and debounce
            Thread.Sleep(1000);

            // Assert
            var user1 = await _store.FindAsync("user1");
            Assert.IsNotNull(user1);
            Assert.AreEqual("User One Updated", user1!.DisplayName);
            
            Assert.IsNotNull(await _store.FindAsync("user2"));
        }

        [TestMethod]
        public void Dispose_ShouldCleanupResources()
        {
            // Arrange
            CreateTestJsonFile(new List<User>());
            _store = new JsonUserStore(_tempFilePath, enableHotReload: true);

            // Act & Assert - Should not throw
            _store.Dispose();
            
            // Multiple disposes should be safe
            _store.Dispose();
        }

        private User CreateTestUser(string userId, string displayName)
        {
            var salt = PasswordHasher.GenerateSalt();
            return new User
            {
                UserId = userId,
                DisplayName = displayName,
                Salt = salt,
                PasswordHash = PasswordHasher.HashPassword("password123", salt),
                HomeDirectory = $"/home/{userId}",
                Permissions = new List<Permission>
                {
                    new Permission { Path = $"/home/{userId}", CanRead = true, CanWrite = true }
                }
            };
        }

        private void CreateTestJsonFile(List<User> users)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(users, options);
            File.WriteAllText(_tempFilePath, json);
        }
    }
} 