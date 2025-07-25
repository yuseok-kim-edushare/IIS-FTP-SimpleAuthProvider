namespace IIS.Ftp.SimpleAuth.Core.Security
{
    /// <summary>
    /// Interface for password hashing and verification functionality.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hashes a password using the default algorithm.
        /// </summary>
        string HashPassword(string password);

        /// <summary>
        /// Verifies a password against a hash.
        /// </summary>
        bool VerifyPassword(string password, string hash, string salt = "");

        /// <summary>
        /// Generates a salt for algorithms that require separate salt.
        /// </summary>
        string GenerateSalt();
    }

    /// <summary>
    /// Implementation of IPasswordHasher using the static PasswordHasher class.
    /// </summary>
    public class PasswordHasherService : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            return PasswordHasher.HashPasswordBCrypt(password);
        }

        public bool VerifyPassword(string password, string hash, string salt = "")
        {
            return PasswordHasher.Verify(password, salt, hash);
        }

        public string GenerateSalt()
        {
            return PasswordHasher.GenerateSalt();
        }
    }
}