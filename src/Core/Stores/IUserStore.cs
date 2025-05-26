using System.Collections.Generic;
using System.Threading.Tasks;
using IIS.Ftp.SimpleAuth.Core.Domain;

namespace IIS.Ftp.SimpleAuth.Core.Stores
{
    public interface IUserStore
    {
        User? Find(string userId);

        bool Validate(string userId, string password);

        IEnumerable<Permission> GetPermissions(string userId);
    }
} 