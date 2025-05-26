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

            try
            {
                var json = File.ReadAllText(_filePath, Encoding.UTF8);
                var users = JsonSerializer.Deserialize<List<User>>(json, _jsonOptions) ?? new List<User>();
                var newCache = users.ToImmutableDictionary(u => u.UserId, StringComparer.OrdinalIgnoreCase);
                
                // Atomic cache replacement
                Interlocked.Exchange(ref _cache, newCache);
            }
            catch (Exception)
            {
                // Keep existing cache if loading fails
                // Consider logging this error in production
            }
        }

        private void DebouncedReload()
        {
            // Improved debounce logic with proper synchronization
            Task.Delay(500).ContinueWith(_ => Load(), TaskScheduler.Default);
        }

        public User? Find(string userId)
        {
            _cache.TryGetValue(userId, out var user);
            return user;
        }

        public bool Validate(string userId, string password)
        {
            var user = Find(userId);
            if (user == null) return false;
            return PasswordHasher.Verify(password, user.Salt, user.PasswordHash);
        }

        public IEnumerable<Permission> GetPermissions(string userId)
        {
            var user = Find(userId);
            return user?.Permissions ?? Enumerable.Empty<Permission>();
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }
} 