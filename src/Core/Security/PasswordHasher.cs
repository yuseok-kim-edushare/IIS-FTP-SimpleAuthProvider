using System;
using System.Security.Cryptography;

namespace IIS.Ftp.SimpleAuth.Core.Security
{
    /// <summary>
    /// Helper for hashing and verifying passwords with support for PBKDF2 and BCrypt algorithms.
    /// </summary>
    public static class PasswordHasher
    {
        private const int HashSizeBytes = 32; // 256 bit
        private const int DefaultBCryptWorkFactor = 12; // BCrypt work factor (cost)

        /// <summary>
        /// Generates a salt for PBKDF2. Not needed for BCrypt as it generates its own salt.
        /// </summary>
        public static string GenerateSalt(int sizeBytes = 16)
        {
            var salt = new byte[sizeBytes];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }

        /// <summary>
        /// Hashes a password using BCrypt.
        /// </summary>
        public static string HashPasswordBCrypt(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, DefaultBCryptWorkFactor);
        }

        /// <summary>
        /// Hashes a password using PBKDF2.
        /// </summary>
        public static string HashPasswordPBKDF2(string password, string saltBase64, int iterations = 100_000)
        {
            var salt = Convert.FromBase64String(saltBase64);
            byte[] hash;
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                hash = deriveBytes.GetBytes(HashSizeBytes);
            }
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Legacy method for backward compatibility with PBKDF2.
        /// </summary>
        public static string HashPassword(string password, string saltBase64, int iterations = 100_000)
        {
            return HashPasswordPBKDF2(password, saltBase64, iterations);
        }

        /// <summary>
        /// Verifies a password against a hash, auto-detecting the algorithm.
        /// For BCrypt: Uses the hash directly (salt is embedded).
        /// For PBKDF2: Uses separate salt and hash.
        /// </summary>
        public static bool Verify(string password, string saltBase64, string expectedHash, int iterations = 100_000)
        {
            var algorithm = DetectAlgorithm(expectedHash);
            
            switch (algorithm.ToUpperInvariant())
            {
                case "BCRYPT":
                    // For BCrypt, the expectedHash contains the full BCrypt hash including salt
                    // We ignore the saltBase64 parameter for BCrypt
                    return BCrypt.Net.BCrypt.Verify(password, expectedHash);

                case "PBKDF2":
                    var actualHash = HashPasswordPBKDF2(password, saltBase64, iterations);
                    var expected = Convert.FromBase64String(expectedHash);
                    var actual = Convert.FromBase64String(actualHash);

#if NET5_0_OR_GREATER
                    return CryptographicOperations.FixedTimeEquals(actual, expected);
#else
                    // Manual constant-time compare for .NET Framework
                    if (actual.Length != expected.Length) return false;
                    var diff = 0;
                    for (int i = 0; i < actual.Length; i++)
                    {
                        diff |= actual[i] ^ expected[i];
                    }
                    return diff == 0;
#endif

                default:
                    throw new ArgumentException($"Unsupported hashing algorithm: {algorithm}");
            }
        }

        /// <summary>
        /// Detects the algorithm used for a given hash.
        /// BCrypt hashes start with $2a$, $2b$, $2x$, or $2y$.
        /// Everything else is assumed to be PBKDF2.
        /// </summary>
        public static string DetectAlgorithm(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return "BCrypt"; // Default to BCrypt for new hashes

            // BCrypt hashes start with $2a$, $2b$, $2x$, or $2y$
            if (hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || 
                hash.StartsWith("$2x$") || hash.StartsWith("$2y$"))
            {
                return "BCrypt";
            }

            // Everything else is assumed to be PBKDF2 (Base64 encoded)
            return "PBKDF2";
        }
    }
} 