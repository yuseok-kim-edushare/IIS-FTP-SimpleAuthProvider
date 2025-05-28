using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Monitoring;

namespace IIS.Ftp.SimpleAuth.Core.Stores
{
    /// <summary>
    /// Decorator for IUserStore that records metrics for each operation.
    /// </summary>
    public class InstrumentedUserStore : IUserStore, IDisposable
    {
        private readonly IUserStore _inner;
        private readonly MetricsCollector _metrics;

        public InstrumentedUserStore(IUserStore inner, MetricsCollector metrics)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        }

        public async Task<User?> FindAsync(string userId)
        {
            var operation = "FindAsync";
            var success = true;
            try
            {
                var result = await _inner.FindAsync(userId);
                return result;
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                _metrics.RecordUserStoreOperation(operation, success);
            }
        }

        public async Task<bool> ValidateAsync(string userId, string password)
        {
            var operation = "ValidateAsync";
            var success = true;
            try
            {
                var result = await _inner.ValidateAsync(userId, password);
                return result;
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                _metrics.RecordUserStoreOperation(operation, success);
            }
        }

        public async Task<IEnumerable<Permission>> GetPermissionsAsync(string userId)
        {
            var operation = "GetPermissionsAsync";
            var success = true;
            try
            {
                var result = await _inner.GetPermissionsAsync(userId);
                return result;
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                _metrics.RecordUserStoreOperation(operation, success);
            }
        }

        public async Task SaveUserAsync(User user)
        {
            var operation = "SaveUserAsync";
            var success = true;
            try
            {
                await _inner.SaveUserAsync(user);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                _metrics.RecordUserStoreOperation(operation, success);
            }
        }

        public async Task DeleteUserAsync(string userId)
        {
            var operation = "DeleteUserAsync";
            var success = true;
            try
            {
                await _inner.DeleteUserAsync(userId);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                _metrics.RecordUserStoreOperation(operation, success);
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            var operation = "GetAllUsersAsync";
            var success = true;
            try
            {
                var result = await _inner.GetAllUsersAsync();
                return result;
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                _metrics.RecordUserStoreOperation(operation, success);
            }
        }

        public async Task AddPermissionAsync(string userId, Permission permission)
        {
            var operation = "AddPermissionAsync";
            var success = true;
            try
            {
                await _inner.AddPermissionAsync(userId, permission);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                _metrics.RecordUserStoreOperation(operation, success);
            }
        }

        public async Task DeletePermissionAsync(string userId, Permission permission)
        {
            var operation = "DeletePermissionAsync";
            var success = true;
            try
            {
                await _inner.DeletePermissionAsync(userId, permission);
            }
            catch (Exception)
            {
                success = false;
                throw;
            }
            finally
            {
                _metrics.RecordUserStoreOperation(operation, success);
            }
        }

        public void Dispose()
        {
            if (_inner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
} 