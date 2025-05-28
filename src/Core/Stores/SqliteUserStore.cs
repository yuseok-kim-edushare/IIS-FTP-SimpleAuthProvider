using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Logging;
using IIS.Ftp.SimpleAuth.Core.Security;

namespace IIS.Ftp.SimpleAuth.Core.Stores
{
    /// <summary>
    /// SQLite-based user store implementation.
    /// </summary>
    public sealed class SqliteUserStore : IUserStore, IDisposable
    {
        private readonly string _connectionString;
        private readonly AuditLogger? _auditLogger;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public SqliteUserStore(string databasePath, AuditLogger? auditLogger = null)
        {
            _connectionString = $"Data Source={databasePath};Version=3;";
            _auditLogger = auditLogger;
            
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                
                // Create Users table
                using (var cmd = new SQLiteCommand(@"
                    CREATE TABLE IF NOT EXISTS Users (
                        UserId TEXT PRIMARY KEY NOT NULL,
                        DisplayName TEXT NOT NULL,
                        Salt TEXT NOT NULL,
                        PasswordHash TEXT NOT NULL,
                        HomeDirectory TEXT NOT NULL,
                        Permissions TEXT NOT NULL
                    )", connection))
                {
                    cmd.ExecuteNonQuery();
                }
                
                _auditLogger?.LogConfigurationChange("SqliteUserStore", "Database initialized");
            }
        }

        public async Task<User?> FindAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            return await Task.Run(() =>
            {
                try
                {
                    using (var connection = new SQLiteConnection(_connectionString))
                    {
                        connection.Open();
                        
                        using (var cmd = new SQLiteCommand(
                            "SELECT UserId, DisplayName, Salt, PasswordHash, HomeDirectory, Permissions " +
                            "FROM Users WHERE UserId = @userId COLLATE NOCASE", connection))
                        {
                            cmd.Parameters.AddWithValue("@userId", userId);
                            
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    return new User
                                    {
                                        UserId = reader.GetString(0),
                                        DisplayName = reader.GetString(1),
                                        Salt = reader.GetString(2),
                                        PasswordHash = reader.GetString(3),
                                        HomeDirectory = reader.GetString(4),
                                        Permissions = DeserializePermissions(reader.GetString(5))
                                    };
                                }
                            }
                        }
                    }
                    
                    return null;
                }
                catch (Exception ex)
                {
                    _auditLogger?.LogUserStoreError("FindAsync", 
                        $"Error finding user {userId}: {ex.Message}");
                    return null;
                }
            });
        }

        public async Task<bool> ValidateAsync(string userId, string password)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
                return false;

            var user = await FindAsync(userId);
            if (user == null) return false;

            try
            {
                // Run the CPU-intensive password hashing operation on a thread pool thread
                return await Task.Run(() => PasswordHasher.Verify(password, user.Salt, user.PasswordHash));
            }
            catch (Exception ex)
            {
                _auditLogger?.LogUserStoreError("ValidateAsync", 
                    $"Error validating password for user {userId}: {ex.Message}");
                return false;
            }
        }

        public async Task<IEnumerable<Permission>> GetPermissionsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return Enumerable.Empty<Permission>();

            var user = await FindAsync(userId);
            return user?.Permissions ?? Enumerable.Empty<Permission>();
        }

        /// <summary>
        /// Adds or updates a user in the database.
        /// </summary>
        public async Task SaveUserAsync(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            await Task.Run(() =>
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (var cmd = new SQLiteCommand(@"
                        INSERT OR REPLACE INTO Users 
                        (UserId, DisplayName, Salt, PasswordHash, HomeDirectory, Permissions)
                        VALUES (@userId, @displayName, @salt, @passwordHash, @homeDirectory, @permissions)",
                        connection))
                    {
                        cmd.Parameters.AddWithValue("@userId", user.UserId);
                        cmd.Parameters.AddWithValue("@displayName", user.DisplayName);
                        cmd.Parameters.AddWithValue("@salt", user.Salt);
                        cmd.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
                        cmd.Parameters.AddWithValue("@homeDirectory", user.HomeDirectory);
                        cmd.Parameters.AddWithValue("@permissions", SerializePermissions(user.Permissions));
                        
                        cmd.ExecuteNonQuery();
                    }
                }
                
                _auditLogger?.LogConfigurationChange("SqliteUserStore", $"User {user.UserId} saved");
            });
        }

        /// <summary>
        /// Deletes a user from the database.
        /// </summary>
        public async Task DeleteUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return;

            await Task.Run(() =>
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (var cmd = new SQLiteCommand("DELETE FROM Users WHERE UserId = @userId", connection))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        var rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                        {
                            _auditLogger?.LogConfigurationChange("SqliteUserStore", $"User {userId} deleted");
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Gets all users from the database.
        /// </summary>
        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await Task.Run(() =>
            {
                var users = new List<User>();
                
                try
                {
                    using (var connection = new SQLiteConnection(_connectionString))
                    {
                        connection.Open();
                        
                        using (var cmd = new SQLiteCommand(
                            "SELECT UserId, DisplayName, Salt, PasswordHash, HomeDirectory, Permissions FROM Users",
                            connection))
                        {
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    users.Add(new User
                                    {
                                        UserId = reader.GetString(0),
                                        DisplayName = reader.GetString(1),
                                        Salt = reader.GetString(2),
                                        PasswordHash = reader.GetString(3),
                                        HomeDirectory = reader.GetString(4),
                                        Permissions = DeserializePermissions(reader.GetString(5))
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _auditLogger?.LogUserStoreError("GetAllUsersAsync", 
                        $"Error getting all users: {ex.Message}");
                }
                
                return users;
            });
        }

        public async Task AddPermissionAsync(string userId, Permission permission)
        {
            if (string.IsNullOrEmpty(userId) || permission == null) return;

            var user = await FindAsync(userId);
            if (user == null)
            {
                _auditLogger?.LogUserStoreError("AddPermissionAsync", $"User {userId} not found when trying to add permission");
                return;
            }

            try
            {
                // Create a mutable copy of permissions
                var permissions = user.Permissions?.ToList() ?? new List<Permission>();

                // Check if permission for this path already exists
                var existingPermIndex = permissions.FindIndex(p => string.Equals(p.Path, permission.Path, StringComparison.OrdinalIgnoreCase));

                if (existingPermIndex != -1)
                {
                    // Update existing permission
                    permissions[existingPermIndex] = permission; // Replace with the new permission object (might have different read/write flags)
                    _auditLogger?.LogConfigurationChange("SqliteUserStore", $"Updated permission for path '{permission.Path}' for user '{userId}'");
                }
                else
                {
                    // Add new permission
                    permissions.Add(permission);
                    _auditLogger?.LogConfigurationChange("SqliteUserStore", $"Added permission for path '{permission.Path}' for user '{userId}'");
                }

                // Update the user object with the modified permissions
                user.Permissions = permissions; // Since User is a class, we modify the list in place

                // Save the updated user back to the database
                await SaveUserAsync(user);
            }
            catch (Exception ex)
            {
                _auditLogger?.LogUserStoreError("AddPermissionAsync", $"Error adding permission for user {userId}: {ex.Message}");
                throw; // Re-throw the exception
            }
        }

        public async Task DeletePermissionAsync(string userId, Permission permission)
        {
            if (string.IsNullOrEmpty(userId) || permission == null) return;

            var user = await FindAsync(userId);
            if (user == null)
            {
                _auditLogger?.LogUserStoreError("DeletePermissionAsync", $"User {userId} not found when trying to delete permission");
                return;
            }

            try
            {
                // Create a mutable copy of permissions
                var permissions = user.Permissions?.ToList() ?? new List<Permission>();

                // Find and remove the permission by path (case-insensitive)
                var initialCount = permissions.Count;
                permissions.RemoveAll(p => string.Equals(p.Path, permission.Path, StringComparison.OrdinalIgnoreCase));

                if (permissions.Count < initialCount)
                {
                    _auditLogger?.LogConfigurationChange("SqliteUserStore", $"Deleted permission for path '{permission.Path}' for user '{userId}'");

                    // Update the user object with the modified permissions
                    user.Permissions = permissions; // Since User is a class, we modify the list in place

                    // Save the updated user back to the database
                    await SaveUserAsync(user);
                }
                else
                {
                     _auditLogger?.LogUserStoreError("DeletePermissionAsync", $"Permission for path '{permission.Path}' not found for user '{userId}'");
                }
            }
            catch (Exception ex)
            {
                _auditLogger?.LogUserStoreError("DeletePermissionAsync", $"Error deleting permission for user {userId}: {ex.Message}");
                throw; // Re-throw the exception
            }
        }

        private string SerializePermissions(List<Permission> permissions)
        {
            return JsonSerializer.Serialize(permissions, _jsonOptions);
        }

        private List<Permission> DeserializePermissions(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<Permission>>(json, _jsonOptions) ?? new List<Permission>();
            }
            catch
            {
                return new List<Permission>();
            }
        }

        public void Dispose()
        {
            // SQLite connections are managed per-operation, nothing to dispose
        }
    }
} 