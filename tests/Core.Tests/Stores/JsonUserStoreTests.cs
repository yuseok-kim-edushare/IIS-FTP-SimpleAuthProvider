using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Security;
using IIS.Ftp.SimpleAuth.Core.Stores;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace IIS.Ftp.SimpleAuth.Core.Tests.Stores
{
    [TestFixture]
    public class JsonUserStoreTests
    {
        private string _tempFilePath = null!;
        private JsonUserStore _store = null!;

        [SetUp]
        public void SetUp()
        {
            _tempFilePath = Path.GetTempFileName();
        }

        [TearDown]
        public void TearDown()
        {
            _store?.Dispose();
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }

        [Test]
        public void Constructor_NonExistentFile_ShouldCreateEmptyStore()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");

            // Act
            _store = new JsonUserStore(nonExistentPath, enableHotReload: false);

            // Assert
            Assert.That(_store.Find("anyuser"), Is.Null);
        }

        [Test]
        public void Constructor_ValidJsonFile_ShouldLoadUsers()
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

            // Assert
            var foundUser = _store.Find("testuser");
            Assert.That(foundUser, Is.Not.Null);
            Assert.That(foundUser!.UserId, Is.EqualTo("testuser"));
            Assert.That(foundUser.DisplayName, Is.EqualTo("Test User"));
        }

        [Test]
        public void Find_ExistingUser_ShouldReturnUser()
        {
            // Arrange
            var user = CreateTestUser("testuser", "Test User");
            CreateTestJsonFile(new List<User> { user });
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Act
            var foundUser = _store.Find("testuser");

            // Assert
            Assert.That(foundUser, Is.Not.Null);
            Assert.That(foundUser!.UserId, Is.EqualTo("testuser"));
            Assert.That(foundUser.DisplayName, Is.EqualTo("Test User"));
        }

        [Test]
        public void Find_NonExistentUser_ShouldReturnNull()
        {
            // Arrange
            CreateTestJsonFile(new List<User>());
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Act
            var foundUser = _store.Find("nonexistent");

            // Assert
            Assert.That(foundUser, Is.Null);
        }

        [Test]
        public void Find_CaseInsensitive_ShouldReturnUser()
        {
            // Arrange
            var user = CreateTestUser("TestUser", "Test User");
            CreateTestJsonFile(new List<User> { user });
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Act
            var foundUser1 = _store.Find("testuser");
            var foundUser2 = _store.Find("TESTUSER");
            var foundUser3 = _store.Find("TestUser");

            // Assert
            Assert.That(foundUser1, Is.Not.Null);
            Assert.That(foundUser2, Is.Not.Null);
            Assert.That(foundUser3, Is.Not.Null);
            
            Assert.That(foundUser1!.UserId, Is.EqualTo("TestUser"));
            Assert.That(foundUser2!.UserId, Is.EqualTo("TestUser"));
            Assert.That(foundUser3!.UserId, Is.EqualTo("TestUser"));
        }

        [Test]
        public void Validate_CorrectPassword_ShouldReturnTrue()
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
            var isValid = _store.Validate("testuser", password);

            // Assert
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void Validate_IncorrectPassword_ShouldReturnFalse()
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
            var isValid = _store.Validate("testuser", wrongPassword);

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void Validate_NonExistentUser_ShouldReturnFalse()
        {
            // Arrange
            CreateTestJsonFile(new List<User>());
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Act
            var isValid = _store.Validate("nonexistent", "anypassword");

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void GetPermissions_ExistingUser_ShouldReturnPermissions()
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
            var userPermissions = _store.GetPermissions("testuser").ToList();

            // Assert
            Assert.That(userPermissions, Has.Count.EqualTo(2));
            Assert.That(userPermissions, Is.EquivalentTo(permissions));
        }

        [Test]
        public void GetPermissions_NonExistentUser_ShouldReturnEmpty()
        {
            // Arrange
            CreateTestJsonFile(new List<User>());
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Act
            var permissions = _store.GetPermissions("nonexistent").ToList();

            // Assert
            Assert.That(permissions, Is.Empty);
        }

        [Test]
        public void GetPermissions_UserWithNoPermissions_ShouldReturnEmpty()
        {
            // Arrange
            var user = CreateTestUser("testuser", "Test User");
            user.Permissions = new List<Permission>();

            CreateTestJsonFile(new List<User> { user });
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Act
            var permissions = _store.GetPermissions("testuser").ToList();

            // Assert
            Assert.That(permissions, Is.Empty);
        }

        [Test]
        public void Constructor_InvalidJsonFile_ShouldCreateEmptyStore()
        {
            // Arrange
            File.WriteAllText(_tempFilePath, "invalid json content");

            // Act
            _store = new JsonUserStore(_tempFilePath, enableHotReload: false);

            // Assert
            Assert.That(_store.Find("anyuser"), Is.Null);
        }

        [Test]
        public void Find_MultipleUsers_ShouldReturnCorrectUser()
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
            var user1 = _store.Find("user1");
            var user2 = _store.Find("user2");
            var user3 = _store.Find("user3");

            // Assert
            Assert.That(user1, Is.Not.Null);
            Assert.That(user1!.DisplayName, Is.EqualTo("User One"));
            
            Assert.That(user2, Is.Not.Null);
            Assert.That(user2!.DisplayName, Is.EqualTo("User Two"));
            
            Assert.That(user3, Is.Not.Null);
            Assert.That(user3!.DisplayName, Is.EqualTo("User Three"));
        }

        [Test]
        public void HotReload_FileModification_ShouldReloadUsers()
        {
            // Arrange
            var initialUsers = new List<User> { CreateTestUser("user1", "User One") };
            CreateTestJsonFile(initialUsers);
            _store = new JsonUserStore(_tempFilePath, enableHotReload: true);

            // Verify initial state
            Assert.That(_store.Find("user1"), Is.Not.Null);
            Assert.That(_store.Find("user2"), Is.Null);

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
            var user1 = _store.Find("user1");
            Assert.That(user1, Is.Not.Null);
            Assert.That(user1!.DisplayName, Is.EqualTo("User One Updated"));
            
            Assert.That(_store.Find("user2"), Is.Not.Null);
        }

        [Test]
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