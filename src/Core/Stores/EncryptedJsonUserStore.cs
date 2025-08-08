using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Logging;
using IIS.Ftp.SimpleAuth.Core.Security;

namespace IIS.Ftp.SimpleAuth.Core.Stores
{
    /// <summary>
    /// Encrypted JSON-file backed user store. Uses AES-256-GCM when EncryptionKeyEnv is provided; 
    /// otherwise falls back to DPAPI. Thread-safe via immutable snapshot replacement.
    /// </summary>
    public sealed class EncryptedJsonUserStore : IUserStore, IDisposable
    {
        private readonly string _filePath;
        private readonly string? _keyEnvVar;
        private readonly FileSystemWatcher? _watcher;
        private readonly AuditLogger? _auditLogger;
        private ImmutableDictionary<string, User> _cache = ImmutableDictionary<string, User>.Empty;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public EncryptedJsonUserStore(string filePath, bool enableHotReload = true, string? encryptionKeyEnv = null, AuditLogger? auditLogger = null)
        {
            _filePath = filePath;
            _keyEnvVar = encryptionKeyEnv;
            _auditLogger = auditLogger;
            Load();

            if (enableHotReload)
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    _watcher = new FileSystemWatcher(directory, Path.GetFileName(_filePath))
                    {
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.FileName
                    };
                    _watcher.Changed += (s, e) => DebouncedReload();
                    _watcher.Created += (s, e) => DebouncedReload();
                    _watcher.Renamed += (s, e) => DebouncedReload();
                    _watcher.EnableRaisingEvents = true;
                }
            }
        }

        private void Load()
        {
            if (!File.Exists(_filePath))
            {
                _cache = ImmutableDictionary<string, User>.Empty;
                _auditLogger?.LogConfigurationChange("EncryptedJsonUserStore", $"User file not found: {_filePath}. Starting with empty store.");
                return;
            }

            try
            {
                string json;
                try
                {
                    json = FileEncryption.DecryptFile(_filePath, _keyEnvVar);
                }
                catch (Exception ex)
                {
                    _auditLogger?.LogUserStoreError("Load", $"Decryption failed for {_filePath}: {ex.Message}");
                    throw;
                }

                var users = JsonSerializer.Deserialize<List<User>>(json, _jsonOptions) ?? new List<User>();
                var newCache = users.ToImmutableDictionary(u => u.UserId, StringComparer.OrdinalIgnoreCase);

                var oldCount = _cache.Count;
                Interlocked.Exchange(ref _cache, newCache);
                _auditLogger?.LogConfigurationChange("EncryptedJsonUserStore", $"Loaded {newCache.Count} users from {_filePath} (previously {oldCount})");
            }
            catch (Exception ex)
            {
                _auditLogger?.LogUserStoreError("Load", $"Error loading encrypted store {_filePath}: {ex.GetType().Name} - {ex.Message}");
            }
        }

        private void DebouncedReload()
        {
            Task.Delay(500).ContinueWith(_ =>
            {
                try { Load(); }
                catch (Exception ex)
                {
                    _auditLogger?.LogUserStoreError("DebouncedReload", $"Error during hot-reload: {ex.Message}");
                }
            }, TaskScheduler.Default);
        }

        public Task<User?> FindAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return Task.FromResult<User?>(null);
            _cache.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        public async Task<bool> ValidateAsync(string userId, string password)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password)) return false;
            var user = await FindAsync(userId);
            if (user == null) return false;
            try
            {
                return await Task.Run(() => PasswordHasher.Verify(password, user.Salt, user.PasswordHash));
            }
            catch (Exception ex)
            {
                _auditLogger?.LogUserStoreError("ValidateAsync", $"Error validating password for user {userId}: {ex.Message}");
                return false;
            }
        }

        public async Task<IEnumerable<Permission>> GetPermissionsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return Enumerable.Empty<Permission>();
            var user = await FindAsync(userId);
            return user?.Permissions ?? Enumerable.Empty<Permission>();
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }

        public async Task SaveUserAsync(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            await Task.Run(() =>
            {
                var newCache = _cache.SetItem(user.UserId, user);
                Interlocked.Exchange(ref _cache, newCache);
                Save();
                _auditLogger?.LogConfigurationChange("EncryptedJsonUserStore", $"User {user.UserId} saved");
            });
        }

        public async Task DeleteUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;
            await Task.Run(() =>
            {
                if (_cache.ContainsKey(userId))
                {
                    var newCache = _cache.Remove(userId);
                    Interlocked.Exchange(ref _cache, newCache);
                    Save();
                    _auditLogger?.LogConfigurationChange("EncryptedJsonUserStore", $"User {userId} deleted");
                }
            });
        }

        public Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return Task.FromResult<IEnumerable<User>>(_cache.Values.ToList());
        }

        public async Task AddPermissionAsync(string userId, Permission permission)
        {
            if (string.IsNullOrEmpty(userId) || permission == null) return;
            await Task.Run(() =>
            {
                if (_cache.TryGetValue(userId, out var user))
                {
                    var permissions = user.Permissions?.ToList() ?? new List<Permission>();
                    var existingIndex = permissions.FindIndex(p => string.Equals(p.Path, permission.Path, StringComparison.OrdinalIgnoreCase));
                    if (existingIndex != -1) permissions[existingIndex] = permission; else permissions.Add(permission);

                    var updatedUser = new User
                    {
                        UserId = user.UserId,
                        DisplayName = user.DisplayName,
                        Salt = user.Salt,
                        PasswordHash = user.PasswordHash,
                        HomeDirectory = user.HomeDirectory,
                        Permissions = permissions
                    };

                    var newCache = _cache.SetItem(userId, updatedUser);
                    Interlocked.Exchange(ref _cache, newCache);
                    Save();
                }
                else
                {
                    _auditLogger?.LogUserStoreError("AddPermissionAsync", $"User {userId} not found when trying to add permission");
                }
            });
        }

        public async Task DeletePermissionAsync(string userId, Permission permission)
        {
            if (string.IsNullOrEmpty(userId) || permission == null) return;
            await Task.Run(() =>
            {
                if (_cache.TryGetValue(userId, out var user))
                {
                    var permissions = user.Permissions?.ToList() ?? new List<Permission>();
                    var before = permissions.Count;
                    permissions.RemoveAll(p => string.Equals(p.Path, permission.Path, StringComparison.OrdinalIgnoreCase));
                    if (permissions.Count < before)
                    {
                        var updatedUser = new User
                        {
                            UserId = user.UserId,
                            DisplayName = user.DisplayName,
                            Salt = user.Salt,
                            PasswordHash = user.PasswordHash,
                            HomeDirectory = user.HomeDirectory,
                            Permissions = permissions
                        };
                        var newCache = _cache.SetItem(userId, updatedUser);
                        Interlocked.Exchange(ref _cache, newCache);
                        Save();
                    }
                    else
                    {
                        _auditLogger?.LogUserStoreError("DeletePermissionAsync", $"Permission for path '{permission.Path}' not found for user '{userId}'");
                    }
                }
                else
                {
                    _auditLogger?.LogUserStoreError("DeletePermissionAsync", $"User {userId} not found when trying to delete permission");
                }
            });
        }

        private void Save()
        {
            var json = JsonSerializer.Serialize(_cache.Values.ToList(), _jsonOptions);
            var tmpPath = _filePath + ".tmp";
            FileEncryption.EncryptFile(CreateTempPlainFile(json, tmpPath), _filePath, _keyEnvVar);
            TryDeleteTemp(tmpPath);
        }

        private static string CreateTempPlainFile(string content, string tmpPath)
        {
            File.WriteAllText(tmpPath, content, Encoding.UTF8);
            return tmpPath;
        }

        private static void TryDeleteTemp(string tmpPath)
        {
            try { if (File.Exists(tmpPath)) File.Delete(tmpPath); }
            catch { /* ignore */ }
        }
    }
}


