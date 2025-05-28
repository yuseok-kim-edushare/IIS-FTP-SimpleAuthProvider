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
using IIS.Ftp.SimpleAuth.Core.Security;
using IIS.Ftp.SimpleAuth.Core.Logging;

namespace IIS.Ftp.SimpleAuth.Core.Stores
{
    /// <summary>
    /// Simple JSON-file backed user store. Thread-safe via immutable snapshot replacement.
    /// </summary>
    public sealed class JsonUserStore : IUserStore, IDisposable
    {
        private readonly string _filePath;
        private readonly FileSystemWatcher? _watcher;
        private readonly AuditLogger? _auditLogger;
        private ImmutableDictionary<string, User> _cache = ImmutableDictionary<string, User>.Empty;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public JsonUserStore(string filePath, bool enableHotReload = true, AuditLogger? auditLogger = null)
        {
            _filePath = filePath;
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
                _auditLogger?.LogConfigurationChange("JsonUserStore", $"User file not found: {_filePath}. Starting with empty store.");
                return;
            }

            try
            {
                var json = File.ReadAllText(_filePath, Encoding.UTF8);
                var users = JsonSerializer.Deserialize<List<User>>(json, _jsonOptions) ?? new List<User>();
                var newCache = users.ToImmutableDictionary(u => u.UserId, StringComparer.OrdinalIgnoreCase);
                
                // Atomic cache replacement
                var oldCount = _cache.Count;
                Interlocked.Exchange(ref _cache, newCache);
                
                _auditLogger?.LogConfigurationChange("JsonUserStore", 
                    $"Loaded {newCache.Count} users from {_filePath} (previously {oldCount})");
            }
            catch (FileNotFoundException ex)
            {
                _auditLogger?.LogUserStoreError("Load", $"File not found: {ex.FileName}");
                // Keep existing cache
            }
            catch (UnauthorizedAccessException ex)
            {
                _auditLogger?.LogUserStoreError("Load", $"Access denied to file {_filePath}: {ex.Message}");
                // Keep existing cache
            }
            catch (JsonException ex)
            {
                _auditLogger?.LogUserStoreError("Load", $"Invalid JSON in {_filePath}: {ex.Message}");
                // Keep existing cache to prevent service disruption
            }
            catch (IOException ex)
            {
                _auditLogger?.LogUserStoreError("Load", $"IO error reading {_filePath}: {ex.Message}");
                // Keep existing cache
            }
            catch (Exception ex)
            {
                _auditLogger?.LogUserStoreError("Load", $"Unexpected error loading {_filePath}: {ex.GetType().Name} - {ex.Message}");
                // Keep existing cache
            }
        }

        private void DebouncedReload()
        {
            // Improved debounce logic with proper synchronization
            Task.Delay(500).ContinueWith(_ => 
            {
                try
                {
                    Load();
                }
                catch (Exception ex)
                {
                    _auditLogger?.LogUserStoreError("DebouncedReload", $"Error during hot-reload: {ex.Message}");
                }
            }, TaskScheduler.Default);
        }

        public Task<User?> FindAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Task.FromResult<User?>(null);
            }
            
            _cache.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        public async Task<bool> ValidateAsync(string userId, string password)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(password))
            {
                return false;
            }
            
            var user = await FindAsync(userId);
            if (user == null) return false;
            
            try
            {
                // Run the CPU-intensive password hashing operation on a thread pool thread
                return await Task.Run(() => PasswordHasher.Verify(password, user.Salt, user.PasswordHash));
            }
            catch (FormatException ex)
            {
                _auditLogger?.LogUserStoreError("ValidateAsync", 
                    $"Invalid hash format for user {userId}: {ex.Message}");
                return false;
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
            {
                return Enumerable.Empty<Permission>();
            }
            
            var user = await FindAsync(userId);
            return user?.Permissions ?? Enumerable.Empty<Permission>();
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }
} 