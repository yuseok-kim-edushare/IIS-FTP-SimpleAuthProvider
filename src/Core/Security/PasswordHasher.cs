using System;
using System.Security.Cryptography;
using Konscious.Security.Cryptography;

namespace IIS.Ftp.SimpleAuth.Core.Security
{
    /// <summary>
    /// Helper for hashing and verifying passwords with support for PBKDF2 and BCrypt algorithms.
    /// </summary>
    public static class PasswordHasher
    {
        private const int HashSizeBytes = 32; // 256 bit
        private const int DefaultBCryptWorkFactor = 12; // BCrypt work factor (cost)
        public const string DefaultAlgorithm = "BCrypt"; // Default algorithm for new passwords

        /// <summary>
        /// Generates a salt for PBKDF2. Not needed for BCrypt as it generates its own salt.
        /// </summary>
        public static string GenerateSalt(int sizeBytes = 16)
        {
            var salt = new byte[sizeBytes];
            try
            {
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(salt);
                }
                
                var result = Convert.ToBase64String(salt);
                return result;
            }
            finally
            {
                // Clear the salt from memory
                SecureMemoryHelper.ClearMemory(salt);
            }
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
            byte[] hash = null;
            try
            {
                using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                {
                    hash = deriveBytes.GetBytes(HashSizeBytes);
                }
                
                var result = Convert.ToBase64String(hash);
                return result;
            }
            finally
            {
                // Clear sensitive data from memory
                SecureMemoryHelper.ClearMemory(salt);
                if (hash != null)
                {
                    SecureMemoryHelper.ClearMemory(hash);
                }
            }
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
        /// For Argon2: Expects PHC format in expectedHash and ignores salt parameter.
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

                    try
                    {
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
                    }
                    finally
                    {
                        // Clear sensitive hash data from memory
                        SecureMemoryHelper.ClearMemory(expected);
                        SecureMemoryHelper.ClearMemory(actual);
                    }

                case "ARGON2":
                    return VerifyArgon2(password, expectedHash);

                default:
                    throw new ArgumentException($"Unsupported hashing algorithm: {algorithm}");
            }
        }

        /// <summary>
        /// Detects the algorithm used for a given hash.
        /// If the hash is null or empty, the default algorithm specified in <see cref="DefaultAlgorithm"/> is used.
        /// BCrypt hashes start with $2a$, $2b$, $2x$, or $2y$.
        /// Argon2 hashes start with $argon2i$, $argon2d$ or $argon2id$ (PHC format).
        /// Everything else is assumed to be PBKDF2.
        /// </summary>
        public static string DetectAlgorithm(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return DefaultAlgorithm; // Use the configurable default algorithm

            // BCrypt hashes start with $2a$, $2b$, $2x$, or $2y$
            if (hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || 
                hash.StartsWith("$2x$") || hash.StartsWith("$2y$"))
            {
                return "BCrypt";
            }

            // Argon2 hashes start with $argon2i$, $argon2d$ or $argon2id$ (PHC format)
            if (hash.StartsWith("$argon2i$") || hash.StartsWith("$argon2d$") || 
                hash.StartsWith("$argon2id$"))
            {
                return "ARGON2";
            }

            // Everything else is assumed to be PBKDF2 (Base64 encoded)
            return "PBKDF2";
        }
        
        /// <summary>
        /// Hashes a password using Argon2id and returns a PHC formatted string
        /// $argon2id$v=19$m=...,t=...,p=...$base64salt$base64hash
        /// </summary>
        public static string HashPasswordArgon2(string password, int memorySizeKb = 32768, int iterations = 3, int degreeOfParallelism = 2, int hashSizeBytes = HashSizeBytes)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            // Generate random 16-byte salt
            var salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = null;
            try
            {
                using (var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password)))
                {
                    argon2.Salt = salt;
                    argon2.DegreeOfParallelism = Math.Max(1, degreeOfParallelism);
                    argon2.MemorySize = Math.Max(8192, memorySizeKb);
                    argon2.Iterations = Math.Max(2, iterations);
                    hash = argon2.GetBytes(hashSizeBytes);
                }

                var saltB64 = Convert.ToBase64String(salt);
                var hashB64 = Convert.ToBase64String(hash);
                return $"$argon2id$v=19$m={Math.Max(8192, memorySizeKb)},t={Math.Max(2, iterations)},p={Math.Max(1, degreeOfParallelism)}${saltB64}${hashB64}";
            }
            finally
            {
                if (hash != null) SecureMemoryHelper.ClearMemory(hash);
                SecureMemoryHelper.ClearMemory(salt);
            }
        }

        private static bool VerifyArgon2(string password, string phc)
        {
            // Expect PHC: $argon2id$v=19$m=...,t=...,p=...$salt$hash
            try
            {
                var parts = phc.Split('$');
                // ['', 'argon2id', 'v=19', 'm=..,t=..,p=..', 'saltB64', 'hashB64']
                if (parts.Length < 6) return false;
                var paramsPart = parts[3];
                var saltB64 = parts[4];
                var hashB64 = parts[5];

                int m = 0, t = 0, p = 0;
                foreach (var kv in paramsPart.Split(','))
                {
                    var kvp = kv.Split('=');
                    if (kvp.Length != 2) continue;
                    if (kvp[0] == "m") int.TryParse(kvp[1], out m);
                    else if (kvp[0] == "t") int.TryParse(kvp[1], out t);
                    else if (kvp[0] == "p") int.TryParse(kvp[1], out p);
                }

                var salt = Convert.FromBase64String(saltB64);
                var expected = Convert.FromBase64String(hashB64);
                byte[] actual = null;
                try
                {
                    using (var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password)))
                    {
                        argon2.Salt = salt;
                        argon2.DegreeOfParallelism = Math.Max(1, p);
                        argon2.MemorySize = Math.Max(8192, m);
                        argon2.Iterations = Math.Max(2, t);
                        actual = argon2.GetBytes(expected.Length);
                    }

                    // Constant-time compare
                    if (actual.Length != expected.Length) return false;
                    var diff = 0;
                    for (int i = 0; i < actual.Length; i++) diff |= actual[i] ^ expected[i];
                    return diff == 0;
                }
                finally
                {
                    SecureMemoryHelper.ClearMemory(salt);
                    SecureMemoryHelper.ClearMemory(expected);
                    if (actual != null) SecureMemoryHelper.ClearMemory(actual);
                }
            }
            catch
            {
                return false;
            }
        }


    }
} 