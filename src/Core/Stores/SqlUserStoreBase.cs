using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Logging;
using System.Collections.Generic;

namespace IIS.Ftp.SimpleAuth.Core.Stores
{
    public abstract class SqlUserStoreBase : IUserStore
    {
        protected readonly string _connectionString;
        protected readonly AuditLogger? _auditLogger;

        public SqlUserStoreBase(string connectionString, AuditLogger? auditLogger = null)
        {
            _connectionString = connectionString;
            _auditLogger = auditLogger;
        }

        // Abstract methods for database-specific operations
        protected abstract Task<IDbConnection> GetOpenConnectionAsync();
        protected abstract IDbCommand CreateCommand(string commandText, IDbConnection connection);

        // Implementations of IUserStore methods utilizing abstract methods
        public async Task<User?> FindAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            User? user = null;

            try
            {
                using (var connection = await GetOpenConnectionAsync())
                {
                    var userQuery = GetFindUserQuery(); // Abstract method for database-specific query
                    using (var command = CreateCommand(userQuery, connection))
                    {
                        AddUserIdParameter(command, userId); // Abstract method for database-specific parameter

                        // Explicitly cast to DbCommand to access ExecuteReaderAsync
                        using (var reader = await ((DbCommand)command).ExecuteReaderAsync())
                        {
                            // Explicitly cast to DbDataReader to access ReadAsync
                            if (await ((DbDataReader)reader).ReadAsync())
                            {
                                user = ReadUserFromReader(reader); // Abstract method for database-specific reading
                            }
                        }
                    }

                    if (user != null)
                    {
                        // Permissions are fetched separately and assigned here
                        // The concrete classes will implement GetPermissionsAsync
                         user.Permissions = (System.Collections.Generic.List<Permission>)(await GetPermissionsAsync(userId));
                    }
                }
            }
            catch (System.Exception ex)
            {
                _auditLogger?.LogUserStoreError("FindAsync", $"Error in FindAsync for user {userId}: {ex.Message}");
                throw; // Re-throw the exception
            }

            return user;
        }

        // Abstract methods required for FindAsync implementation
        protected abstract string GetFindUserQuery();
        protected abstract void AddUserIdParameter(IDbCommand command, string userId);
        protected abstract User ReadUserFromReader(IDataReader reader);

        // Abstract methods for the rest of IUserStore methods
        public abstract Task<bool> ValidateAsync(string userId, string password);
        public abstract Task<System.Collections.Generic.IEnumerable<Permission>> GetPermissionsAsync(string userId);
        public abstract Task SaveUserAsync(User user);
        public abstract Task DeleteUserAsync(string userId);
        public abstract Task<System.Collections.Generic.IEnumerable<User>> GetAllUsersAsync();
        public abstract Task AddPermissionAsync(string userId, Permission permission);
        public abstract Task DeletePermissionAsync(string userId, Permission permission);

        // Common helper method (can be overridden if needed)
         protected virtual string SerializePermissions(List<Permission> permissions)
        {
            // Default implementation (can be overridden)
            return System.Text.Json.JsonSerializer.Serialize(permissions);
        }

        protected virtual List<Permission> DeserializePermissions(string json)
        {
            // Default implementation (can be overridden)
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<Permission>>(json) ?? new List<Permission>();
            }
            catch
            {
                return new List<Permission>();
            }
        }
    }
} 