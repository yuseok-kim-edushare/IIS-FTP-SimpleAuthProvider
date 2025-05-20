using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Security;

namespace IIS.Ftp.SimpleAuth.Core.Stores
{
    /// <summary>
    /// Simple JSON-file backed user store. Thread-safe via immutable snapshot replacement.
    /// </summary>
    public sealed class JsonUserStore : IUserStore, IDisposable
    {
        private readonly string _filePath;
        private readonly FileSystemWatcher? _watcher;
        private ImmutableDictionary<string, User> _cache = ImmutableDictionary<string, User>.Empty;
        private readonly object _sync = new object();
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        public JsonUserStore(string filePath, bool enableHotReload = true)
        {
            _filePath = filePath;
            Load();

            if (enableHotReload)
            {
                _watcher = new FileSystemWatcher(Path.GetDirectoryName(_filePath)!, Path.GetFileName(_filePath))
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.FileName
                };
                _watcher.Changed += (s, e) => DebouncedReload();
                _watcher.Created += (s, e) => DebouncedReload();
                _watcher.Renamed += (s, e) => DebouncedReload();
                _watcher.EnableRaisingEvents = true;
            }
        }

        private void Load()
        {
            if (!File.Exists(_filePath))
            {
                _cache = ImmutableDictionary<string, User>.Empty;
                return;
            }

            var json = File.ReadAllText(_filePath, Encoding.UTF8);
            var users = JsonSerializer.Deserialize<List<User>>(json, _jsonOptions) ?? new List<User>();
            _cache = users.ToImmutableDictionary(u => u.UserId, StringComparer.OrdinalIgnoreCase);
        }

        private void DebouncedReload()
        {
            // Simple 500 ms debounce
            lock (_sync)
            {
                _watcher!.EnableRaisingEvents = false;
            }
            Task.Delay(500).ContinueWith(_ =>
            {
                Load();
                lock (_sync)
                {
                    _watcher!.EnableRaisingEvents = true;
                }
            });
        }

        public Task<User?> FindAsync(string userId)
        {
            _cache.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        public async Task<bool> ValidateAsync(string userId, string password)
        {
            var user = await FindAsync(userId).ConfigureAwait(false);
            if (user == null) return false;
            return PasswordHasher.Verify(password, user.Salt, user.PasswordHash);
        }

        public async Task<IEnumerable<Permission>> GetPermissionsAsync(string userId)
        {
            var user = await FindAsync(userId).ConfigureAwait(false);
            return user?.Permissions ?? Enumerable.Empty<Permission>();
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }
} 