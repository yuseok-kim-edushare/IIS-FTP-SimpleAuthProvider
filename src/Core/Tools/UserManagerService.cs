using System.Collections.Generic;
using System.Threading.Tasks;
using IIS.Ftp.SimpleAuth.Core.Domain;
using IIS.Ftp.SimpleAuth.Core.Security;
using IIS.Ftp.SimpleAuth.Core.Stores;

namespace IIS.Ftp.SimpleAuth.Core.Tools
{
    /// <summary>
    /// Instance-based user management service that wraps the static UserManager functionality.
    /// </summary>
    public class UserManagerService
    {
        private readonly IUserStore _userStore;
        private readonly IPasswordHasher _passwordHasher;

        public UserManagerService(IUserStore userStore, IPasswordHasher passwordHasher)
        {
            _userStore = userStore;
            _passwordHasher = passwordHasher;
        }

        /// <summary>
        /// Creates a new user asynchronously.
        /// </summary>
        public async Task<bool> CreateUserAsync(string userId, string displayName, string password, 
            string homeDirectory, List<Permission>? permissions = null)
        {
            try
            {
                var hashedPassword = _passwordHasher.HashPassword(password, out var salt);

                var user = new User
                {
                    UserId = userId,
                    DisplayName = displayName,
                    HomeDirectory = homeDirectory,
                    PasswordHash = hashedPassword,
                    Salt = salt,
                    Permissions = permissions ?? new List<Permission>()
                };

                await _userStore.SaveUserAsync(user);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Changes a user's password asynchronously.
        /// </summary>
        public async Task<bool> ChangePasswordAsync(string userId, string newPassword)
        {
            try
            {
                var user = await _userStore.FindAsync(userId);
                if (user == null) return false;

                user.PasswordHash = _passwordHasher.HashPassword(newPassword, out var salt);
                user.Salt = salt;

                await _userStore.SaveUserAsync(user);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}