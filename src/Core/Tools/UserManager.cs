using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Security;

namespace IIS.Ftp.SimpleAuth.Core.Tools
{
    /// <summary>
    /// Utility class for managing FTP users and encryption keys.
    /// </summary>
    public static class UserManager
    {
        /// <summary>
        /// Creates a new user with hashed password and saves to JSON file.
        /// </summary>
        public static void CreateUser(string filePath, string userId, string password, string displayName, 
            string homeDirectory = "/", List<Permission>? permissions = null, string algorithm = "BCrypt", int iterations = 100_000)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be empty", nameof(userId));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password cannot be empty", nameof(password));

            // Load existing users
            var users = LoadUsers(filePath);

            // Check if user already exists
            if (users.Any(u => string.Equals(u.UserId, userId, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"User '{userId}' already exists");
            }

            // Generate salt and hash password based on algorithm
            string salt = "";
            string passwordHash = "";
            
            if (algorithm.ToUpperInvariant() == "BCRYPT")
            {
                // BCrypt generates its own salt internally
                salt = ""; // Empty for BCrypt
                passwordHash = PasswordHasher.HashPasswordBCrypt(password);
            }
            else
            {
                // PBKDF2 requires separate salt
                salt = PasswordHasher.GenerateSalt();
                passwordHash = PasswordHasher.HashPasswordPBKDF2(password, salt, iterations);
            }

            // Create user object
            var newUser = new User
            {
                UserId = userId,
                DisplayName = displayName,
                Salt = salt,
                PasswordHash = passwordHash,
                HomeDirectory = homeDirectory,
                Permissions = permissions ?? new List<Permission>
                {
                    new Permission { Path = "/", CanRead = true, CanWrite = false }
                }
            };

            // Add to users list
            users.Add(newUser);

            // Save back to file
            SaveUsers(filePath, users);

            Console.WriteLine($"User '{userId}' created successfully using {algorithm} hashing");
        }

        /// <summary>
        /// Updates a user's password.
        /// </summary>
        public static void ChangePassword(string filePath, string userId, string newPassword, string algorithm = "BCrypt", int iterations = 100_000)
        {
            if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be empty", nameof(userId));
            if (string.IsNullOrWhiteSpace(newPassword)) throw new ArgumentException("Password cannot be empty", nameof(newPassword));

            var users = LoadUsers(filePath);
            var user = users.FirstOrDefault(u => string.Equals(u.UserId, userId, StringComparison.OrdinalIgnoreCase));
            
            if (user == null)
            {
                throw new InvalidOperationException($"User '{userId}' not found");
            }

            // Update password using specified algorithm
            if (algorithm.ToUpperInvariant() == "BCRYPT")
            {
                // BCrypt generates its own salt internally
                user.Salt = ""; // Empty for BCrypt
                user.PasswordHash = PasswordHasher.HashPasswordBCrypt(newPassword);
            }
            else
            {
                // PBKDF2 requires separate salt
                user.Salt = PasswordHasher.GenerateSalt();
                user.PasswordHash = PasswordHasher.HashPasswordPBKDF2(newPassword, user.Salt, iterations);
            }

            SaveUsers(filePath, users);
            Console.WriteLine($"Password for user '{userId}' updated successfully using {algorithm} hashing");
        }

        /// <summary>
        /// Lists all users in the store.
        /// </summary>
        public static void ListUsers(string filePath)
        {
            var users = LoadUsers(filePath);
            
            if (!users.Any())
            {
                Console.WriteLine("No users found");
                return;
            }

            Console.WriteLine($"{"User ID",-20} {"Display Name",-30} {"Home Directory",-20} {"Permissions",-10}");
            Console.WriteLine(new string('-', 85));
            
            foreach (var user in users)
            {
                var permCount = user.Permissions?.Count ?? 0;
                Console.WriteLine($"{user.UserId,-20} {user.DisplayName,-30} {user.HomeDirectory,-20} {permCount,-10}");
            }
        }

        /// <summary>
        /// Adds a permission to a user.
        /// </summary>
        public static void AddPermission(string filePath, string userId, string path, bool canRead, bool canWrite)
        {
            var users = LoadUsers(filePath);
            var user = users.FirstOrDefault(u => string.Equals(u.UserId, userId, StringComparison.OrdinalIgnoreCase));
            
            if (user == null)
            {
                throw new InvalidOperationException($"User '{userId}' not found");
            }

            user.Permissions ??= new List<Permission>();
            
            // Check if permission already exists for this path
            var existingPerm = user.Permissions.FirstOrDefault(p => string.Equals(p.Path, path, StringComparison.OrdinalIgnoreCase));
            if (existingPerm != null)
            {
                existingPerm.CanRead = canRead;
                existingPerm.CanWrite = canWrite;
                Console.WriteLine($"Updated permission for path '{path}' for user '{userId}'");
            }
            else
            {
                user.Permissions.Add(new Permission { Path = path, CanRead = canRead, CanWrite = canWrite });
                Console.WriteLine($"Added permission for path '{path}' for user '{userId}'");
            }

            SaveUsers(filePath, users);
        }

        /// <summary>
        /// Generates a new 256-bit encryption key for AES-GCM.
        /// </summary>
        public static string GenerateEncryptionKey()
        {
            return FileEncryption.GenerateKey();
        }

        /// <summary>
        /// Encrypts a plain JSON user file.
        /// </summary>
        public static void EncryptUserFile(string plainFilePath, string encryptedFilePath, string? keyEnvVar = null)
        {
            if (!File.Exists(plainFilePath))
            {
                throw new FileNotFoundException($"Source file not found: {plainFilePath}");
            }

            FileEncryption.EncryptFile(plainFilePath, encryptedFilePath, keyEnvVar);
            Console.WriteLine($"File encrypted: {plainFilePath} -> {encryptedFilePath}");
        }

        /// <summary>
        /// Decrypts an encrypted user file to plain JSON.
        /// </summary>
        public static void DecryptUserFile(string encryptedFilePath, string plainFilePath, string? keyEnvVar = null)
        {
            if (!File.Exists(encryptedFilePath))
            {
                throw new FileNotFoundException($"Source file not found: {encryptedFilePath}");
            }

            var decryptedContent = FileEncryption.DecryptFile(encryptedFilePath, keyEnvVar);
            File.WriteAllText(plainFilePath, decryptedContent);
            Console.WriteLine($"File decrypted: {encryptedFilePath} -> {plainFilePath}");
        }

        /// <summary>
        /// Rotates the encryption key for an encrypted user file.
        /// </summary>
        public static void RotateEncryptionKey(string encryptedFilePath, string? oldKeyEnvVar = null, string? newKeyEnvVar = null)
        {
            if (!File.Exists(encryptedFilePath))
            {
                throw new FileNotFoundException($"Source file not found: {encryptedFilePath}");
            }

            // Create backup
            var backupPath = encryptedFilePath + ".backup-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            File.Copy(encryptedFilePath, backupPath);
            Console.WriteLine($"Created backup: {backupPath}");

            try
            {
                // Decrypt with old key
                var decryptedContent = FileEncryption.DecryptFile(encryptedFilePath, oldKeyEnvVar);
                
                // Create temporary file
                var tempPath = encryptedFilePath + ".tmp";
                File.WriteAllText(tempPath, decryptedContent);
                
                // Re-encrypt with new key
                FileEncryption.EncryptFile(tempPath, encryptedFilePath, newKeyEnvVar);
                
                // Clean up temp file
                File.Delete(tempPath);
                
                Console.WriteLine($"Key rotation completed successfully for: {encryptedFilePath}");
                Console.WriteLine($"Backup retained at: {backupPath}");
            }
            catch (Exception ex)
            {
                // Restore from backup on failure
                if (File.Exists(backupPath))
                {
                    File.Copy(backupPath, encryptedFilePath, overwrite: true);
                    Console.WriteLine($"Key rotation failed, restored from backup: {ex.Message}");
                }
                throw;
            }
        }

        private static List<User> LoadUsers(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new List<User>();
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<User>>(json, options) ?? new List<User>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load users from {filePath}: {ex.Message}", ex);
            }
        }

        private static void SaveUsers(string filePath, List<User> users)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(users, options);
                
                // Ensure directory exists if file path contains directory
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save users to {filePath}: {ex.Message}", ex);
            }
        }
    }
} 