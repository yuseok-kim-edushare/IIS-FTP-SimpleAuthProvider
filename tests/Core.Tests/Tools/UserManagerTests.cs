using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Security;
using IIS.Ftp.SimpleAuth.Core.Tools;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace IIS.Ftp.SimpleAuth.Core.Tests.Tools
{
    [TestFixture]
    public class UserManagerTests
    {
        private string _tempFilePath = null!;
        private string _tempDirectory = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            _tempFilePath = Path.Combine(_tempDirectory, "users.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [Test]
        public void CreateUser_ValidData_ShouldCreateUserSuccessfully()
        {
            // Arrange
            var userId = "testuser";
            var password = "TestPassword123!";
            var displayName = "Test User";
            var homeDirectory = "/home/testuser";

            // Act
            UserManager.CreateUser(_tempFilePath, userId, password, displayName, homeDirectory);

            // Assert
            var users = LoadUsersFromFile();
            Assert.That(users, Has.Count.EqualTo(1));

            var user = users.First();
            Assert.That(user.UserId, Is.EqualTo(userId));
            Assert.That(user.DisplayName, Is.EqualTo(displayName));
            Assert.That(user.HomeDirectory, Is.EqualTo(homeDirectory));
            Assert.That(user.Salt, Is.Not.Null.And.Not.Empty);
            Assert.That(user.PasswordHash, Is.Not.Null.And.Not.Empty);
            Assert.That(user.Permissions, Has.Count.EqualTo(1));
            Assert.That(user.Permissions.First().Path, Is.EqualTo("/"));
            Assert.That(user.Permissions.First().CanRead, Is.True);
            Assert.That(user.Permissions.First().CanWrite, Is.False);
        }

        [Test]
        public void CreateUser_WithCustomPermissions_ShouldUseProvidedPermissions()
        {
            // Arrange
            var userId = "testuser";
            var password = "TestPassword123!";
            var displayName = "Test User";
            var permissions = new List<Permission>
            {
                new Permission { Path = "/home/testuser", CanRead = true, CanWrite = true },
                new Permission { Path = "/shared", CanRead = true, CanWrite = false }
            };

            // Act
            UserManager.CreateUser(_tempFilePath, userId, password, displayName, permissions: permissions);

            // Assert
            var users = LoadUsersFromFile();
            var user = users.First();
            Assert.That(user.Permissions, Is.EquivalentTo(permissions));
        }

        [Test]
        public void CreateUser_EmptyUserId_ShouldThrowArgumentException()
        {
            // Act & Assert
            var action = () => UserManager.CreateUser(_tempFilePath, "", "password", "displayName");
            Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.StartsWith("User ID cannot be empty"));
        }

        [Test]
        public void CreateUser_NullUserId_ShouldThrowArgumentException()
        {
            // Act & Assert
            var action = () => UserManager.CreateUser(_tempFilePath, null!, "password", "displayName");
            Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.StartsWith("User ID cannot be empty"));
        }

        [Test]
        public void CreateUser_EmptyPassword_ShouldThrowArgumentException()
        {
            // Act & Assert
            var action = () => UserManager.CreateUser(_tempFilePath, "userid", "", "displayName");
            Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.StartsWith("Password cannot be empty"));
        }

        [Test]
        public void CreateUser_NullPassword_ShouldThrowArgumentException()
        {
            // Act & Assert
            var action = () => UserManager.CreateUser(_tempFilePath, "userid", null!, "displayName");
            Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.StartsWith("Password cannot be empty"));
        }

        [Test]
        public void CreateUser_DuplicateUserId_ShouldThrowInvalidOperationException()
        {
            // Arrange
            UserManager.CreateUser(_tempFilePath, "testuser", "password1", "User 1");

            // Act & Assert
            var action = () => UserManager.CreateUser(_tempFilePath, "testuser", "password2", "User 2");
            Assert.That(action, Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("User 'testuser' already exists"));
        }

        [Test]
        public void CreateUser_CaseInsensitiveDuplicate_ShouldThrowInvalidOperationException()
        {
            // Arrange
            UserManager.CreateUser(_tempFilePath, "TestUser", "password1", "User 1");

            // Act & Assert
            var action = () => UserManager.CreateUser(_tempFilePath, "testuser", "password2", "User 2");
            Assert.That(action, Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("User 'testuser' already exists"));
        }

        [Test]
        public void CreateUser_NonExistentDirectory_ShouldCreateDirectoryAndFile()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_tempDirectory, "subfolder", "users.json");

            // Act
            UserManager.CreateUser(nonExistentPath, "testuser", "password", "Test User");

            // Assert
            Assert.That(File.Exists(nonExistentPath), Is.True);
            var users = LoadUsersFromFile(nonExistentPath);
            Assert.That(users, Has.Count.EqualTo(1));
        }

        [Test]
        public void ChangePassword_ExistingUser_ShouldUpdatePassword()
        {
            // Arrange
            var userId = "testuser";
            var originalPassword = "OriginalPassword123!";
            var newPassword = "NewPassword456!";
            
            UserManager.CreateUser(_tempFilePath, userId, originalPassword, "Test User");
            
            var usersBefore = LoadUsersFromFile();
            var originalSalt = usersBefore.First().Salt;
            var originalHash = usersBefore.First().PasswordHash;

            // Act
            UserManager.ChangePassword(_tempFilePath, userId, newPassword);

            // Assert
            var usersAfter = LoadUsersFromFile();
            var user = usersAfter.First();
            
            Assert.That(user.Salt, Is.Not.EqualTo(originalSalt));
            Assert.That(user.PasswordHash, Is.Not.EqualTo(originalHash));
            
            // Verify password works
            Assert.That(PasswordHasher.Verify(newPassword, user.Salt, user.PasswordHash), Is.True);
            Assert.That(PasswordHasher.Verify(originalPassword, user.Salt, user.PasswordHash), Is.False);
        }

        [Test]
        public void ChangePassword_NonExistentUser_ShouldThrowInvalidOperationException()
        {
            // Act & Assert
            var action = () => UserManager.ChangePassword(_tempFilePath, "nonexistent", "newpassword");
            Assert.That(action, Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("User 'nonexistent' not found"));
        }

        [Test]
        public void ChangePassword_EmptyUserId_ShouldThrowArgumentException()
        {
            // Act & Assert
            var action = () => UserManager.ChangePassword(_tempFilePath, "", "newpassword");
            Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.StartsWith("User ID cannot be empty"));
        }

        [Test]
        public void ChangePassword_EmptyPassword_ShouldThrowArgumentException()
        {
            // Act & Assert
            var action = () => UserManager.ChangePassword(_tempFilePath, "userid", "");
            Assert.That(action, Throws.TypeOf<ArgumentException>().With.Message.StartsWith("Password cannot be empty"));
        }

        [Test]
        public void AddPermission_ExistingUser_ShouldAddNewPermission()
        {
            // Arrange
            UserManager.CreateUser(_tempFilePath, "testuser", "password", "Test User");

            // Act
            UserManager.AddPermission(_tempFilePath, "testuser", "/shared", true, true);

            // Assert
            var users = LoadUsersFromFile();
            var user = users.First();
            Assert.That(user.Permissions, Has.Count.EqualTo(2));
            
            var sharedPermission = user.Permissions.FirstOrDefault(p => p.Path == "/shared");
            Assert.That(sharedPermission, Is.Not.Null);
            Assert.That(sharedPermission!.CanRead, Is.True);
            Assert.That(sharedPermission.CanWrite, Is.True);
        }

        [Test]
        public void AddPermission_ExistingPath_ShouldUpdatePermission()
        {
            // Arrange
            UserManager.CreateUser(_tempFilePath, "testuser", "password", "Test User");
            UserManager.AddPermission(_tempFilePath, "testuser", "/shared", true, false);

            // Act
            UserManager.AddPermission(_tempFilePath, "testuser", "/shared", false, true);

            // Assert
            var users = LoadUsersFromFile();
            var user = users.First();
            Assert.That(user.Permissions, Has.Count.EqualTo(2)); // Default "/" + "/shared"
            
            var sharedPermission = user.Permissions.FirstOrDefault(p => p.Path == "/shared");
            Assert.That(sharedPermission, Is.Not.Null);
            Assert.That(sharedPermission!.CanRead, Is.False);
            Assert.That(sharedPermission.CanWrite, Is.True);
        }

        [Test]
        public void AddPermission_NonExistentUser_ShouldThrowInvalidOperationException()
        {
            // Act & Assert
            var action = () => UserManager.AddPermission(_tempFilePath, "nonexistent", "/path", true, false);
            Assert.That(action, Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("User 'nonexistent' not found"));
        }

        [Test]
        public void AddPermission_UserWithNullPermissions_ShouldInitializeAndAddPermission()
        {
            // Arrange - Create user and manually set permissions to null to test edge case
            UserManager.CreateUser(_tempFilePath, "testuser", "password", "Test User");
            var users = LoadUsersFromFile();
            users.First().Permissions = null!;
            SaveUsersToFile(users);

            // Act
            UserManager.AddPermission(_tempFilePath, "testuser", "/test", true, false);

            // Assert
            var updatedUsers = LoadUsersFromFile();
            var user = updatedUsers.First();
            Assert.That(user.Permissions, Is.Not.Null);
            Assert.That(user.Permissions, Has.Count.EqualTo(1));
            Assert.That(user.Permissions.First().Path, Is.EqualTo("/test"));
        }

        [Test]
        public void GenerateEncryptionKey_ShouldReturnValidBase64Key()
        {
            // Act
            var key = UserManager.GenerateEncryptionKey();

            // Assert
            Assert.That(key, Is.Not.Null.And.Not.Empty);
            
            // Should be valid base64
            var keyBytes = Convert.FromBase64String(key);
            Assert.That(keyBytes, Has.Length.EqualTo(32)); // 256 bits = 32 bytes
        }

        [Test]
        public void GenerateEncryptionKey_MultipleCalls_ShouldReturnDifferentKeys()
        {
            // Act
            var key1 = UserManager.GenerateEncryptionKey();
            var key2 = UserManager.GenerateEncryptionKey();
            var key3 = UserManager.GenerateEncryptionKey();

            // Assert
            Assert.That(key2, Is.Not.EqualTo(key1));
            Assert.That(key3, Is.Not.EqualTo(key2));
            Assert.That(key3, Is.Not.EqualTo(key1));
        }

        [Test]
        public void EncryptUserFile_NonExistentSourceFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.json");
            var encryptedPath = Path.Combine(_tempDirectory, "encrypted.dat");

            // Act & Assert
            var action = () => UserManager.EncryptUserFile(nonExistentPath, encryptedPath);
            Assert.That(action, Throws.TypeOf<FileNotFoundException>().With.Message.EqualTo($"Source file not found: {nonExistentPath}"));
        }

        [Test]
        public void DecryptUserFile_NonExistentSourceFile_ShouldThrowFileNotFoundException()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.dat");
            var decryptedPath = Path.Combine(_tempDirectory, "decrypted.json");

            // Act & Assert
            var action = () => UserManager.DecryptUserFile(nonExistentPath, decryptedPath);
            Assert.That(action, Throws.TypeOf<FileNotFoundException>().With.Message.EqualTo($"Source file not found: {nonExistentPath}"));
        }

        [Test]
        public void LoadUsers_InvalidJsonFile_ShouldThrowInvalidOperationException()
        {
            // Arrange
            File.WriteAllText(_tempFilePath, "invalid json content");

            // Act & Assert
            var action = () => UserManager.CreateUser(_tempFilePath, "testuser", "password", "Test User");
            Assert.That(action, Throws.TypeOf<InvalidOperationException>().With.Message.StartsWith($"Failed to load users from {_tempFilePath}"));
        }

        [Test]
        public void CreateUser_CustomIterations_ShouldUseSpecifiedIterations()
        {
            // Arrange
            var iterations = 50000;

            // Act
            UserManager.CreateUser(_tempFilePath, "testuser", "password", "Test User", iterations: iterations);

            // Assert
            var users = LoadUsersFromFile();
            var user = users.First();
            
            // Verify password works with custom iterations
            Assert.That(PasswordHasher.Verify("password", user.Salt, user.PasswordHash, iterations), Is.True);
            Assert.That(PasswordHasher.Verify("password", user.Salt, user.PasswordHash, 100000), Is.False);
        }

        [Test]
        public void ChangePassword_CustomIterations_ShouldUseSpecifiedIterations()
        {
            // Arrange
            UserManager.CreateUser(_tempFilePath, "testuser", "password", "Test User");
            var iterations = 75000;

            // Act
            UserManager.ChangePassword(_tempFilePath, "testuser", "newpassword", "PBKDF2", iterations);

            // Assert
            var users = LoadUsersFromFile();
            var user = users.First();
            
            // Verify password works with custom iterations
            Assert.That(PasswordHasher.Verify("newpassword", user.Salt, user.PasswordHash, iterations), Is.True);
            Assert.That(PasswordHasher.Verify("newpassword", user.Salt, user.PasswordHash, 100000), Is.False);
        }

        private List<User> LoadUsersFromFile(string? filePath = null)
        {
            var path = filePath ?? _tempFilePath;
            if (!File.Exists(path))
            {
                return new List<User>();
            }

            var json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<User>>(json, options) ?? new List<User>();
        }

        private void SaveUsersToFile(List<User> users, string? filePath = null)
        {
            var path = filePath ?? _tempFilePath;
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(users, options);
            File.WriteAllText(path, json);
        }
    }
} 
