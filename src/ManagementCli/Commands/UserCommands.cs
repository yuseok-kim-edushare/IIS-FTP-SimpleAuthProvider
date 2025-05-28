using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Tools;

namespace IIS.Ftp.SimpleAuth.ManagementCli.Commands
{
    public static class UserCommands
    {
        public static int CreateUser(CreateUserOptions options)
        {
            try
            {
                var permissions = new List<Permission>
                {
                    new Permission
                    {
                        Path = options.HomeDirectory,
                        CanRead = options.CanRead,
                        CanWrite = options.CanWrite
                    }
                };

                UserManager.CreateUser(
                    options.FilePath,
                    options.UserId,
                    options.Password,
                    options.DisplayName,
                    options.HomeDirectory,
                    permissions,
                    options.Iterations);

                Console.WriteLine($"✓ User '{options.UserId}' created successfully");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"✗ Error creating user: {ex.Message}");
                return 1;
            }
        }

        public static int ChangePassword(ChangePasswordOptions options)
        {
            try
            {
                UserManager.ChangePassword(
                    options.FilePath,
                    options.UserId,
                    options.NewPassword,
                    options.Iterations);

                Console.WriteLine($"✓ Password changed successfully for user '{options.UserId}'");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"✗ Error changing password: {ex.Message}");
                return 1;
            }
        }

        public static int ListUsers(ListUsersOptions options)
        {
            try
            {
                UserManager.ListUsers(options.FilePath);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"✗ Error listing users: {ex.Message}");
                return 1;
            }
        }

        public static int AddPermission(AddPermissionOptions options)
        {
            try
            {
                UserManager.AddPermission(
                    options.FilePath,
                    options.UserId,
                    options.Path,
                    options.CanRead,
                    options.CanWrite);

                var accessType = options.CanWrite ? "read/write" : "read-only";
                Console.WriteLine($"✓ Added {accessType} permission for '{options.Path}' to user '{options.UserId}'");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"✗ Error adding permission: {ex.Message}");
                return 1;
            }
        }

        public static int RemoveUser(RemoveUserOptions options)
        {
            try
            {
                // Load existing users
                var users = LoadUsers(options.FilePath);
                
                // Find and remove the user
                var userToRemove = users.FirstOrDefault(u => 
                    string.Equals(u.UserId, options.UserId, StringComparison.OrdinalIgnoreCase));
                
                if (userToRemove == null)
                {
                    Console.Error.WriteLine($"✗ User '{options.UserId}' not found");
                    return 1;
                }

                users.Remove(userToRemove);
                
                // Save back to file
                SaveUsers(options.FilePath, users);
                
                Console.WriteLine($"✓ User '{options.UserId}' removed successfully");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"✗ Error removing user: {ex.Message}");
                return 1;
            }
        }

        private static List<User> LoadUsers(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new List<User>();
            }

            var json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<User>>(json, options) ?? new List<User>();
        }

        private static void SaveUsers(string filePath, List<User> users)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(users, options);
            
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
            
            File.WriteAllText(filePath, json);
        }
    }
} 