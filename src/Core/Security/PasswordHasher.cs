using System;
using System.Security.Cryptography;

namespace IIS.Ftp.SimpleAuth.Core.Security
{
    /// <summary>
    /// Helper for hashing and verifying passwords.
    /// </summary>
    public static class PasswordHasher
    {
        private const int HashSizeBytes = 32; // 256 bit

        public static string GenerateSalt(int sizeBytes = 16)
        {
            var salt = new byte[sizeBytes];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }

        public static string HashPassword(string password, string saltBase64, int iterations = 100_000)
        {
            var salt = Convert.FromBase64String(saltBase64);
            byte[] hash;
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                hash = deriveBytes.GetBytes(HashSizeBytes);
            }
            return Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string saltBase64, string expectedHashBase64, int iterations = 100_000)
        {
            var actualHash = HashPassword(password, saltBase64, iterations);
            var expected = Convert.FromBase64String(expectedHashBase64);
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
        }
    }
} 