using System.Collections.Generic;

namespace IIS.Ftp.SimpleAuth.Core.Domain
{
    /// <summary>
    /// Represents a single FTP user entry stored by the authentication provider.
    /// </summary>
    public class User
    {
        public string UserId { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Base-64 salt that was used when deriving <see cref="PasswordHash"/>.
        /// </summary>
        public string Salt { get; set; } = string.Empty;

        /// <summary>
        /// Base-64 encoded password hash (PBKDF2, bcrypt, …).
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        public string HomeDirectory { get; set; } = string.Empty;

        public List<Permission> Permissions { get; set; } = new List<Permission>();
    }
} 