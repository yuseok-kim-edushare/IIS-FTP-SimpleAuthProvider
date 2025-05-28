using System.Collections.Generic;
using System.Threading.Tasks;
using IIS.Ftp.SimpleAuth.Core.Domain;

namespace IIS.Ftp.SimpleAuth.Core.Stores
{
    public interface IUserStore
    {
        Task<User?> FindAsync(string userId);

        Task<bool> ValidateAsync(string userId, string password);

        Task<IEnumerable<Permission>> GetPermissionsAsync(string userId);

        Task SaveUserAsync(User user);

        Task DeleteUserAsync(string userId);

        Task<IEnumerable<User>> GetAllUsersAsync();

        Task AddPermissionAsync(string userId, Permission permission);

        Task DeletePermissionAsync(string userId, Permission permission);
    }
} 