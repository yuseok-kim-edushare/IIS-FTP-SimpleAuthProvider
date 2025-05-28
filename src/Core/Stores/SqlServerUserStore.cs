// Database Schema (Conceptual):
// Users Table:
// - UserId (NVARCHAR, PRIMARY KEY)
// - DisplayName (NVARCHAR)
// - Salt (VARBINARY)
// - PasswordHash (VARBINARY)
// - HomeDirectory (NVARCHAR)

// Permissions Table:
// - PermissionId (INT, PRIMARY KEY, IDENTITY)
// - UserId (NVARCHAR, FOREIGN KEY to Users.UserId)
// - Path (NVARCHAR)
// - CanRead (BIT)
// - CanWrite (BIT)

using System.Collections.Generic;
using System.Threading.Tasks;
using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Stores;
using System.Data.SqlClient; // Assuming System.Data.SqlClient for .NET Framework
using System; // Added for Exception and Console
using IIS.Ftp.SimpleAuth.Core.Security; // Added for PasswordHasher
using IIS.Ftp.SimpleAuth.Core.Logging; // Added for AuditLogger

namespace IIS.Ftp.SimpleAuth.Core.Stores
{
    public class SqlServerUserStore : IUserStore
    {
        private readonly string _connectionString;
        private readonly AuditLogger? _auditLogger;

        public SqlServerUserStore(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SqlServerUserStore(string connectionString, AuditLogger? auditLogger = null)
        {
            _connectionString = connectionString;
            _auditLogger = auditLogger;
        }

        public async Task<User?> FindAsync(string userId)
        {
            User? user = null;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var userQuery = "SELECT UserId, DisplayName, Salt, PasswordHash, HomeDirectory FROM Users WHERE UserId = @UserId";
                    using (var command = new SqlCommand(userQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                user = new User
                                {
                                    UserId = reader["UserId"].ToString()!,
                                    DisplayName = reader["DisplayName"].ToString()!,
                                    Salt = Convert.ToBase64String((byte[])reader["Salt"]),
                                    PasswordHash = Convert.ToBase64String((byte[])reader["PasswordHash"]),
                                    HomeDirectory = reader["HomeDirectory"].ToString()!,
                                    Permissions = new List<Permission>() // Permissions will be fetched separately
                                };
                            }
                        }
                    }

                    if (user != null)
                    {
                        // Implement permission fetching logic here
                        // Example: SELECT Path, CanRead, CanWrite FROM Permissions WHERE UserId = @UserId
                        // Add permissions to user.Permissions list

                        var permissionsQuery = "SELECT Path, CanRead, CanWrite FROM Permissions WHERE UserId = @UserId";
                        using (var permissionsCommand = new SqlCommand(permissionsQuery, connection))
                        {
                            permissionsCommand.Parameters.AddWithValue("@UserId", userId);

                            using (var permissionsReader = await permissionsCommand.ExecuteReaderAsync())
                            {
                                while (await permissionsReader.ReadAsync())
                                {
                                    user.Permissions.Add(new Permission
                                    {
                                        Path = permissionsReader["Path"].ToString()!,
                                        CanRead = (bool)permissionsReader["CanRead"],
                                        CanWrite = (bool)permissionsReader["CanWrite"]
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _auditLogger?.LogUserStoreError("FindAsync", $"Error in FindAsync for user {userId}: {ex.Message}");
                throw; // Re-throw the exception for now
            }

            return user;
        }

        public async Task<IEnumerable<Permission>> GetPermissionsAsync(string userId)
        {
            var permissions = new List<Permission>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var permissionsQuery = "SELECT Path, CanRead, CanWrite FROM Permissions WHERE UserId = @UserId";
                    using (var command = new SqlCommand(permissionsQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                permissions.Add(new Permission
                                {
                                    Path = reader["Path"].ToString()!,
                                    CanRead = (bool)reader["CanRead"],
                                    CanWrite = (bool)reader["CanWrite"]
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _auditLogger?.LogUserStoreError("GetPermissionsAsync", $"Error in GetPermissionsAsync for user {userId}: {ex.Message}");
                throw; // Re-throw the exception for now
            }

            return permissions;
        }

        public async Task<bool> ValidateAsync(string userId, string password)
        {
            byte[]? salt = null;
            byte[]? passwordHash = null;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var validationQuery = "SELECT Salt, PasswordHash FROM Users WHERE UserId = @UserId";
                    using (var command = new SqlCommand(validationQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", userId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                salt = (byte[])reader["Salt"];
                                passwordHash = (byte[])reader["PasswordHash"];
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _auditLogger?.LogUserStoreError("ValidateAsync", $"Error in ValidateAsync for user {userId}: {ex.Message}");
                throw; // Re-throw the exception for now
            }

            if (salt != null && passwordHash != null)
            {
                return PasswordHasher.Verify(password, Convert.ToBase64String(salt), Convert.ToBase64String(passwordHash));
            }

            return false; // User not found or data incomplete
        }

        public async Task SaveUserAsync(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Use a transaction for atomicity
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert or Update User
                            var upsertUserQuery = @"MERGE INTO Users AS target
                                               USING (SELECT @UserId AS UserId) AS source ON target.UserId = source.UserId
                                               WHEN MATCHED THEN
                                                   UPDATE SET DisplayName = @DisplayName, Salt = @Salt, PasswordHash = @PasswordHash, HomeDirectory = @HomeDirectory
                                               WHEN NOT MATCHED THEN
                                                   INSERT (UserId, DisplayName, Salt, PasswordHash, HomeDirectory)
                                                   VALUES (@UserId, @DisplayName, @Salt, @PasswordHash, @HomeDirectory);";

                            using (var command = new SqlCommand(upsertUserQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserId", user.UserId);
                                command.Parameters.AddWithValue("@DisplayName", user.DisplayName);
                                command.Parameters.AddWithValue("@Salt", Convert.FromBase64String(user.Salt));
                                command.Parameters.AddWithValue("@PasswordHash", Convert.FromBase64String(user.PasswordHash));
                                command.Parameters.AddWithValue("@HomeDirectory", user.HomeDirectory);
                                await command.ExecuteNonQueryAsync();
                            }

                            // Delete existing permissions for the user
                            var deletePermissionsQuery = "DELETE FROM Permissions WHERE UserId = @UserId";
                            using (var command = new SqlCommand(deletePermissionsQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserId", user.UserId);
                                await command.ExecuteNonQueryAsync();
                            }

                            // Insert new permissions
                            var insertPermissionQuery = "INSERT INTO Permissions (UserId, Path, CanRead, CanWrite) VALUES (@UserId, @Path, @CanRead, @CanWrite)";
                            foreach (var permission in user.Permissions ?? new List<Permission>())
                            {
                                using (var command = new SqlCommand(insertPermissionQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@UserId", user.UserId);
                                    command.Parameters.AddWithValue("@Path", permission.Path);
                                    command.Parameters.AddWithValue("@CanRead", permission.CanRead);
                                    command.Parameters.AddWithValue("@CanWrite", permission.CanWrite);
                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _auditLogger?.LogUserStoreError("SaveUserAsync", $"Error in SaveUserAsync transaction for user {user.UserId}: {ex.Message}");
                            throw; // Re-throw the exception
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _auditLogger?.LogUserStoreError("SaveUserAsync", $"Error in SaveUserAsync for user {user.UserId}: {ex.Message}");
                throw; // Re-throw the exception
            }
        }

        public async Task DeleteUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Use a transaction for atomicity
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Delete permissions first due to foreign key constraint
                            var deletePermissionsQuery = "DELETE FROM Permissions WHERE UserId = @UserId";
                            using (var command = new SqlCommand(deletePermissionsQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserId", userId);
                                await command.ExecuteNonQueryAsync();
                            }

                            // Delete user
                            var deleteUserQuery = "DELETE FROM Users WHERE UserId = @UserId";
                            using (var command = new SqlCommand(deleteUserQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserId", userId);
                                await command.ExecuteNonQueryAsync();
                            }

                            _auditLogger?.LogConfigurationChange("SqlServerUserStore", $"User {userId} deleted"); // Added delete success log

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _auditLogger?.LogUserStoreError("DeleteUserAsync", $"Error in DeleteUserAsync transaction for user {userId}: {ex.Message}");
                            throw; // Re-throw the exception
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _auditLogger?.LogUserStoreError("DeleteUserAsync", $"Error in DeleteUserAsync for user {userId}: {ex.Message}");
                throw; // Re-throw the exception
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            var users = new List<User>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var userQuery = "SELECT UserId, DisplayName, Salt, PasswordHash, HomeDirectory FROM Users";
                    using (var userCommand = new SqlCommand(userQuery, connection))
                    {
                        using (var userReader = await userCommand.ExecuteReaderAsync())
                        {
                            while (await userReader.ReadAsync())
                            {
                                users.Add(new User
                                {
                                    UserId = userReader["UserId"].ToString()!,
                                    DisplayName = userReader["DisplayName"].ToString()!,
                                    Salt = Convert.ToBase64String((byte[])userReader["Salt"])!, // Assuming Salt is stored as VARBINARY
                                    PasswordHash = Convert.ToBase64String((byte[])userReader["PasswordHash"])!, // Assuming PasswordHash is stored as VARBINARY
                                    HomeDirectory = userReader["HomeDirectory"].ToString()!,
                                    Permissions = new List<Permission>() // Permissions will be fetched separately for each user
                                });
                            }
                        }
                    }

                    // Fetch permissions for each user
                    var permissionsQuery = "SELECT Path, CanRead, CanWrite FROM Permissions WHERE UserId = @UserId";
                    foreach (var user in users)
                    {
                        using (var permissionsCommand = new SqlCommand(permissionsQuery, connection))
                        {
                            permissionsCommand.Parameters.AddWithValue("@UserId", user.UserId);

                            using (var permissionsReader = await permissionsCommand.ExecuteReaderAsync())
                            {
                                while (await permissionsReader.ReadAsync())
                                {
                                    user.Permissions.Add(new Permission
                                    {
                                        Path = permissionsReader["Path"].ToString()!,
                                        CanRead = (bool)permissionsReader["CanRead"],
                                        CanWrite = (bool)permissionsReader["CanWrite"]
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _auditLogger?.LogUserStoreError("GetAllUsersAsync", $"Error in GetAllUsersAsync: {ex.Message}");
                throw; // Re-throw the exception
            }

            return users;
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
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Use a transaction for atomicity
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Check if permission for this path already exists for the user
                            var checkExistingQuery = "SELECT COUNT(*) FROM Permissions WHERE UserId = @UserId AND Path = @Path";
                            using (var checkCommand = new SqlCommand(checkExistingQuery, connection, transaction))
                            {
                                checkCommand.Parameters.AddWithValue("@UserId", userId);
                                checkCommand.Parameters.AddWithValue("@Path", permission.Path);
                                var existingCount = (int)await checkCommand.ExecuteScalarAsync();

                                if (existingCount > 0)
                                {
                                    // Update existing permission
                                    var updatePermissionQuery = "UPDATE Permissions SET CanRead = @CanRead, CanWrite = @CanWrite WHERE UserId = @UserId AND Path = @Path";
                                    using (var updateCommand = new SqlCommand(updatePermissionQuery, connection, transaction))
                                    {
                                        updateCommand.Parameters.AddWithValue("@UserId", userId);
                                        updateCommand.Parameters.AddWithValue("@Path", permission.Path);
                                        updateCommand.Parameters.AddWithValue("@CanRead", permission.CanRead);
                                        updateCommand.Parameters.AddWithValue("@CanWrite", permission.CanWrite);
                                        await updateCommand.ExecuteNonQueryAsync();
                                    }
                                    _auditLogger?.LogConfigurationChange("SqlServerUserStore", $"Updated permission for path '{permission.Path}' for user '{userId}'");
                                }
                                else
                                {
                                    // Insert new permission
                                    var insertPermissionQuery = "INSERT INTO Permissions (UserId, Path, CanRead, CanWrite) VALUES (@UserId, @Path, @CanRead, @CanWrite)";
                                    using (var insertCommand = new SqlCommand(insertPermissionQuery, connection, transaction))
                                    {
                                        insertCommand.Parameters.AddWithValue("@UserId", userId);
                                        insertCommand.Parameters.AddWithValue("@Path", permission.Path);
                                        insertCommand.Parameters.AddWithValue("@CanRead", permission.CanRead);
                                        insertCommand.Parameters.AddWithValue("@CanWrite", permission.CanWrite);
                                        await insertCommand.ExecuteNonQueryAsync();
                                    }
                                    _auditLogger?.LogConfigurationChange("SqlServerUserStore", $"Added permission for path '{permission.Path}' for user '{userId}'");
                                }
                            }

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _auditLogger?.LogUserStoreError("AddPermissionAsync", $"Error in AddPermissionAsync transaction for user {userId}: {ex.Message}");
                            throw; // Re-throw the exception
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _auditLogger?.LogUserStoreError("AddPermissionAsync", $"Error in AddPermissionAsync for user {userId}: {ex.Message}");
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
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Use a transaction for atomicity
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Delete the permission for the user and path
                            var deletePermissionQuery = "DELETE FROM Permissions WHERE UserId = @UserId AND Path = @Path";
                            using (var command = new SqlCommand(deletePermissionQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@UserId", userId);
                                command.Parameters.AddWithValue("@Path", permission.Path);
                                var rowsAffected = await command.ExecuteNonQueryAsync();

                                if (rowsAffected > 0)
                                {
                                     _auditLogger?.LogConfigurationChange("SqlServerUserStore", $"Deleted permission for path '{permission.Path}' for user '{userId}' from SQL Server"); // Placeholder logging
                                }
                                else
                                {
                                    _auditLogger?.LogUserStoreError("DeletePermissionAsync", $"Permission for path '{permission.Path}' not found for user '{userId}' in SQL Server"); // Placeholder logging
                                }
                            }

                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _auditLogger?.LogUserStoreError("DeletePermissionAsync", $"Error in DeletePermissionAsync transaction for user {userId}: {ex.Message}");
                            throw; // Re-throw the exception
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _auditLogger?.LogUserStoreError("DeletePermissionAsync", $"Error in DeletePermissionAsync for user {userId}: {ex.Message}");
                throw; // Re-throw the exception
            }
        }
    }
} 