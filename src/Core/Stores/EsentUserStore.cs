using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Security;
using IIS.Ftp.SimpleAuth.Core.Logging;
using WelsonJS.Esent;

namespace IIS.Ftp.SimpleAuth.Core.Stores
{
    /// <summary>
    /// ESENT database-backed user store using WelsonJS.Esent.
    /// This implementation is Windows-only and requires the availability of the ESENT database engine.
    /// Use this store when you need an embedded database solution on Windows without external dependencies.
    /// </summary>
    public sealed class EsentUserStore : IUserStore, IDisposable
    {
        private readonly EsentDatabase _database;
        private readonly AuditLogger? _auditLogger;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private bool _disposed = false;

        public EsentUserStore(string dataDirectory, AuditLogger? auditLogger = null)
        {
            _auditLogger = auditLogger;

            // Ensure the data directory exists
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }

            // Define the schema for our user table
            var schema = new Schema("Users", new List<Column>
            {
                new Column("UserId", typeof(string), 255),      // Primary key
                new Column("DisplayName", typeof(string), 255),
                new Column("Salt", typeof(string), 255),
                new Column("PasswordHash", typeof(string), 512),
                new Column("HomeDirectory", typeof(string), 1024),
                new Column("Permissions", typeof(string), 4096)  // JSON serialized permissions
            });
            schema.SetPrimaryKey("UserId");

            _database = new EsentDatabase(schema, dataDirectory);
        }

        public async Task<User?> FindAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            return await Task.Run(() =>
            {
                try
                {
                    var userRow = _database.FindById(userId);
                    
                    return userRow != null ? ConvertRowToUser(userRow) : null;
                }
                catch (Exception ex)
                {
                    _auditLogger?.LogUserStoreError("FindUser", $"Error finding user '{userId}': {ex.Message}");
                    return null;
                }
            });
        }

        public async Task<bool> ValidateAsync(string userId, string password)
        {
            var user = await FindAsync(userId);
            if (user == null)
            {
                _auditLogger?.LogAuthenticationFailure("", "", userId, "User not found");
                return false;
            }

            try
            {
                bool isValid = PasswordHasher.Verify(password, user.Salt, user.PasswordHash);
                
                if (isValid)
                {
                    _auditLogger?.LogAuthenticationSuccess("", "", userId);
                }
                else
                {
                    _auditLogger?.LogAuthenticationFailure("", "", userId, "Password validation failed");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _auditLogger?.LogUserStoreError("ValidatePassword", $"Error validating password for user '{userId}': {ex.Message}");
                return false;
            }
        }

        public async Task<IEnumerable<Permission>> GetPermissionsAsync(string userId)
        {
            var user = await FindAsync(userId);
            return user?.Permissions ?? Enumerable.Empty<Permission>();
        }

        public async Task SaveUserAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            await Task.Run(() =>
            {
                try
                {
                    var userData = new Dictionary<string, object>
                    {
                        ["UserId"] = user.UserId,
                        ["DisplayName"] = user.DisplayName ?? string.Empty,
                        ["Salt"] = user.Salt ?? string.Empty,
                        ["PasswordHash"] = user.PasswordHash ?? string.Empty,
                        ["HomeDirectory"] = user.HomeDirectory ?? string.Empty,
                        ["Permissions"] = JsonSerializer.Serialize(user.Permissions ?? new List<Permission>(), _jsonOptions)
                    };

                    // Try to update first, if that fails then insert
                    try
                    {
                        bool updated = _database.Update(userData);
                        if (!updated)
                        {
                            _database.Insert(userData, out _);
                        }
                    }
                    catch
                    {
                        _database.Insert(userData, out _);
                    }

                    _auditLogger?.LogConfigurationChange("EsentUserStore", $"User '{user.UserId}' saved successfully");
                }
                catch (Exception ex)
                {
                    _auditLogger?.LogUserStoreError("SaveUser", $"Error saving user '{user.UserId}': {ex.Message}");
                    throw;
                }
            });
        }

        public async Task DeleteUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

            await Task.Run(() =>
            {
                try
                {
                    _database.DeleteById(userId);
                    _auditLogger?.LogConfigurationChange("EsentUserStore", $"User '{userId}' deleted successfully");
                }
                catch (Exception ex)
                {
                    _auditLogger?.LogUserStoreError("DeleteUser", $"Error deleting user '{userId}': {ex.Message}");
                    throw;
                }
            });
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var allRows = _database.FindAll();
                    return allRows.Select(ConvertRowToUser).ToList();
                }
                catch (Exception ex)
                {
                    _auditLogger?.LogUserStoreError("GetAllUsers", $"Error retrieving all users: {ex.Message}");
                    return Enumerable.Empty<User>();
                }
            });
        }

        public async Task AddPermissionAsync(string userId, Permission permission)
        {
            var user = await FindAsync(userId);
            if (user == null)
                throw new InvalidOperationException($"User '{userId}' not found");

            user.Permissions ??= new List<Permission>();
            
            // Remove existing permission for this path
            user.Permissions.RemoveAll(p => string.Equals(p.Path, permission.Path, StringComparison.OrdinalIgnoreCase));
            
            // Add the new permission
            user.Permissions.Add(permission);

            await SaveUserAsync(user);
            _auditLogger?.LogConfigurationChange("EsentUserStore", $"Permission added for user '{userId}' on path '{permission.Path}'");
        }

        public async Task DeletePermissionAsync(string userId, Permission permission)
        {
            var user = await FindAsync(userId);
            if (user == null)
                throw new InvalidOperationException($"User '{userId}' not found");

            if (user.Permissions != null)
            {
                user.Permissions.RemoveAll(p => string.Equals(p.Path, permission.Path, StringComparison.OrdinalIgnoreCase));
                await SaveUserAsync(user);
                _auditLogger?.LogConfigurationChange("EsentUserStore", $"Permission removed for user '{userId}' on path '{permission.Path}'");
            }
        }

        private User ConvertRowToUser(Dictionary<string, object> row)
        {
            var user = new User
            {
                UserId = row["UserId"]?.ToString() ?? string.Empty,
                DisplayName = row["DisplayName"]?.ToString() ?? string.Empty,
                Salt = row["Salt"]?.ToString() ?? string.Empty,
                PasswordHash = row["PasswordHash"]?.ToString() ?? string.Empty,
                HomeDirectory = row["HomeDirectory"]?.ToString() ?? string.Empty
            };

            // Deserialize permissions
            var permissionsJson = row["Permissions"]?.ToString();
            if (!string.IsNullOrEmpty(permissionsJson))
            {
                try
                {
                    user.Permissions = JsonSerializer.Deserialize<List<Permission>>(permissionsJson!, _jsonOptions) ?? new List<Permission>();
                }
                catch (JsonException ex)
                {
                    _auditLogger?.LogUserStoreError("DeserializePermissions", $"Error deserializing permissions for user '{user.UserId}': {ex.Message}");
                    user.Permissions = new List<Permission>();
                }
            }

            return user;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _database?.Dispose();
                _disposed = true;
            }
        }
    }
}