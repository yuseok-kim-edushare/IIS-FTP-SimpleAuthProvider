using System.Configuration;
using IIS.Ftp.SimpleAuth.Core.Stores;

namespace IIS.Ftp.SimpleAuth.Provider
{
    internal static class UserStoreFactory
    {
        public static IUserStore Create()
        {
            // Default path may be overridden in web.config/app.config
            var path = ConfigurationManager.AppSettings["UserStorePath"]
                       ?? "C:\\inetpub\\ftpusers\\users.json";

            return new JsonUserStore(path);
        }
    }
} 