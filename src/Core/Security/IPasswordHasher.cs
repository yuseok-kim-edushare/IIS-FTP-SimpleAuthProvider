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
        /// Hashes a password and outputs the salt used (if applicable).
        /// For algorithms that embed salt (e.g., BCrypt), the salt will be an empty string.
        /// </summary>
        string HashPassword(string password, out string salt);

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
    /// Configurable implementation of IPasswordHasher that uses HashingConfig to select the algorithm.
    /// Defaults to BCrypt per project policy.
    /// </summary>
    public class PasswordHasherService : IPasswordHasher
    {
        private readonly string _algorithm;
        private readonly int _pbkdf2Iterations;
        private readonly int _bcryptWorkFactor;
        private readonly int _argon2MemorySizeKb;
        private readonly int _argon2Iterations;
        private readonly int _argon2Parallelism;

        public PasswordHasherService()
            : this("BCrypt", 100_000, 12, 32768, 3, 2)
        {
        }

        public PasswordHasherService(string algorithm, int pbkdf2Iterations, int bcryptWorkFactor)
            : this(algorithm, pbkdf2Iterations, bcryptWorkFactor, 32768, 3, 2)
        {
        }

        public PasswordHasherService(string algorithm, int pbkdf2Iterations, int bcryptWorkFactor, int argon2MemorySizeKb, int argon2Iterations, int argon2Parallelism)
        {
            _algorithm = (algorithm ?? "BCrypt").Trim();
            _pbkdf2Iterations = pbkdf2Iterations > 0 ? pbkdf2Iterations : 100_000;
            _bcryptWorkFactor = bcryptWorkFactor > 0 ? bcryptWorkFactor : 12;
            _argon2MemorySizeKb = argon2MemorySizeKb > 0 ? argon2MemorySizeKb : 32768;
            _argon2Iterations = argon2Iterations > 0 ? argon2Iterations : 3;
            _argon2Parallelism = argon2Parallelism > 0 ? argon2Parallelism : 2;
        }

        public string HashPassword(string password)
        {
            switch (_algorithm.ToUpperInvariant())
            {
                case "BCRYPT":
                    return BCrypt.Net.BCrypt.HashPassword(password, _bcryptWorkFactor);
                case "PBKDF2":
                    var salt = PasswordHasher.GenerateSalt();
                    var hash = PasswordHasher.HashPasswordPBKDF2(password, salt, _pbkdf2Iterations);
                    return hash; // Caller should persist salt separately
                case "ARGON2":
                    // PHC formatted Argon2id hash; salt embedded in hash
                    return PasswordHasher.HashPasswordArgon2(password, _argon2MemorySizeKb, _argon2Iterations, _argon2Parallelism);
                default:
                    return BCrypt.Net.BCrypt.HashPassword(password, _bcryptWorkFactor);
            }
        }

        public string HashPassword(string password, out string salt)
        {
            switch (_algorithm.ToUpperInvariant())
            {
                case "BCRYPT":
                    salt = string.Empty;
                    return BCrypt.Net.BCrypt.HashPassword(password, _bcryptWorkFactor);
                case "PBKDF2":
                    salt = PasswordHasher.GenerateSalt();
                    return PasswordHasher.HashPasswordPBKDF2(password, salt, _pbkdf2Iterations);
                case "ARGON2":
                    salt = string.Empty; // salt embedded in PHC
                    return PasswordHasher.HashPasswordArgon2(password, _argon2MemorySizeKb, _argon2Iterations, _argon2Parallelism);
                default:
                    salt = string.Empty;
                    return BCrypt.Net.BCrypt.HashPassword(password, _bcryptWorkFactor);
            }
        }

        public bool VerifyPassword(string password, string hash, string salt = "")
        {
            // Let PasswordHasher auto-detect when possible (handles BCrypt/PBKDF2)
            return PasswordHasher.Verify(password, salt, hash, _pbkdf2Iterations);
        }

        public string GenerateSalt()
        {
            return PasswordHasher.GenerateSalt();
        }
    }
}