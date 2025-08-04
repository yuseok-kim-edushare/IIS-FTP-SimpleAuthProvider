using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace IIS.Ftp.SimpleAuth.Core.Security
{
    /// <summary>
    /// Provides encryption/decryption for user store files using DPAPI or AES-GCM via BCrypt.
    /// </summary>
    public static class FileEncryption
    {
        public static void EncryptFile(string plainTextPath, string encryptedPath, string? keyEnvVar = null)
        {
            var plainText = File.ReadAllText(plainTextPath, Encoding.UTF8);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            
            if (!string.IsNullOrEmpty(keyEnvVar))
            {
                // Use AES-GCM with provided key
                var keyBase64 = Environment.GetEnvironmentVariable(keyEnvVar);
                if (string.IsNullOrEmpty(keyBase64))
                    throw new InvalidOperationException($"Encryption key environment variable '{keyEnvVar}' not found");
                
                var keyBytes = Convert.FromBase64String(keyBase64);
                try
                {
                    // Protect encryption key in memory
                    var protectedKey = SecureMemoryHelper.CreateProtectedCopy(keyBytes);
                    
                    var encryptedBytes = EncryptWithAesGcm(plainBytes, protectedKey, keyBytes.Length);
                    File.WriteAllBytes(encryptedPath, encryptedBytes);
                }
                finally
                {
                    // Clear the original key from memory
                    SecureMemoryHelper.ClearMemory(keyBytes);
                }
            }
            else
            {
                // Use DPAPI for current user/machine
                var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.LocalMachine);
                File.WriteAllBytes(encryptedPath, encryptedBytes);
            }
        }

        public static string DecryptFile(string encryptedPath, string? keyEnvVar = null)
        {
            var encryptedBytes = File.ReadAllBytes(encryptedPath);
            
            byte[] plainBytes;
            if (!string.IsNullOrEmpty(keyEnvVar))
            {
                // Use AES-GCM with provided key
                var keyBase64 = Environment.GetEnvironmentVariable(keyEnvVar);
                if (string.IsNullOrEmpty(keyBase64))
                    throw new InvalidOperationException($"Encryption key environment variable '{keyEnvVar}' not found");
                
                var keyBytes = Convert.FromBase64String(keyBase64);
                try
                {
                    // Protect decryption key in memory
                    var protectedKey = SecureMemoryHelper.CreateProtectedCopy(keyBytes);
                    
                    plainBytes = DecryptWithAesGcm(encryptedBytes, protectedKey, keyBytes.Length);
                }
                finally
                {
                    // Clear the original key from memory
                    SecureMemoryHelper.ClearMemory(keyBytes);
                }
            }
            else
            {
                // Use DPAPI
                plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.LocalMachine);
            }
            
            return Encoding.UTF8.GetString(plainBytes);
        }

        /// <summary>
        /// Generates a new 256-bit AES key for encryption.
        /// </summary>
        public static string GenerateKey()
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                
                // Create a copy of the key and protect it in memory temporarily
                var keyBytes = new byte[aes.Key.Length];
                Array.Copy(aes.Key, keyBytes, aes.Key.Length);
                
                try
                {
                    // Clear the AES key from memory immediately
                    SecureMemoryHelper.ClearMemory(aes.Key);
                    
                    var result = Convert.ToBase64String(keyBytes);
                    return result;
                }
                finally
                {
                    // Clear our copy of the key
                    SecureMemoryHelper.ClearMemory(keyBytes);
                }
            }
        }

        private static byte[] EncryptWithAesGcm(byte[] plainText, byte[] protectedKey, int originalKeyLength)
        {
            // Extract the key safely from protected memory
            var key = SecureMemoryHelper.ExtractProtectedData(protectedKey, originalKeyLength);
            if (key == null)
            {
                // Fallback to unprotected operation if ProtectedMemory is not supported
                var unprotectedKey = new byte[originalKeyLength];
                Array.Copy(protectedKey, unprotectedKey, originalKeyLength);
                try
                {
                    return EncryptWithAesGcm(plainText, unprotectedKey);
                }
                finally
                {
                    SecureMemoryHelper.ClearMemory(unprotectedKey);
                }
            }

            try
            {
                return EncryptWithAesGcm(plainText, key);
            }
            finally
            {
                // Clear the unprotected key immediately
                SecureMemoryHelper.ClearMemory(key);
            }
        }

        private static byte[] EncryptWithAesGcm(byte[] plainText, byte[] key)
        {
            if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes (256-bit)", nameof(key));

            // Generate random nonce
            byte[] nonce = new byte[12];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(nonce);
            }

            string base64Nonce = Convert.ToBase64String(nonce);
            string plainTextString = Encoding.UTF8.GetString(plainText);
            string base64Key = Convert.ToBase64String(key);

            // Use BCrypt AES-GCM encryption
            string encryptedBase64 = BcryptAesGcm.EncryptAesGcm(plainTextString, base64Key, base64Nonce);
            
            // Combine nonce and encrypted data: [nonce_length][nonce][encrypted_data]
            byte[] encryptedData = Convert.FromBase64String(encryptedBase64);
            byte[] result = new byte[4 + nonce.Length + encryptedData.Length];
            
            // Store nonce length as first 4 bytes
            BitConverter.GetBytes(nonce.Length).CopyTo(result, 0);
            // Store nonce
            nonce.CopyTo(result, 4);
            // Store encrypted data
            encryptedData.CopyTo(result, 4 + nonce.Length);
            
            return result;
        }

        private static byte[] DecryptWithAesGcm(byte[] encryptedData, byte[] protectedKey, int originalKeyLength)
        {
            // Extract the key safely from protected memory
            var key = SecureMemoryHelper.ExtractProtectedData(protectedKey, originalKeyLength);
            if (key == null)
            {
                // Fallback to unprotected operation if ProtectedMemory is not supported
                // Ensure the key is exactly 32 bytes
                var key32 = protectedKey.Length >= 32 ? protectedKey.AsSpan(0, 32).ToArray() : throw new ArgumentException("protectedKey must be at least 32 bytes for AES-GCM decryption fallback");
                return DecryptWithAesGcm(encryptedData, key32);
            }

            try
            {
                return DecryptWithAesGcm(encryptedData, key);
            }
            finally
            {
                // Clear the unprotected key immediately
                SecureMemoryHelper.ClearMemory(key);
            }
        }

        private static byte[] DecryptWithAesGcm(byte[] encryptedData, byte[] key)
        {
            if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes (256-bit)", nameof(key));
            if (encryptedData.Length < 16) throw new ArgumentException("Encrypted data too short", nameof(encryptedData));

            // Extract nonce length
            int nonceLength = BitConverter.ToInt32(encryptedData, 0);
            if (nonceLength != 12) throw new ArgumentException("Invalid nonce length", nameof(encryptedData));

            // Extract nonce
            byte[] nonce = new byte[nonceLength];
            Array.Copy(encryptedData, 4, nonce, 0, nonceLength);

            // Extract encrypted payload
            byte[] payload = new byte[encryptedData.Length - 4 - nonceLength];
            Array.Copy(encryptedData, 4 + nonceLength, payload, 0, payload.Length);

            string base64Nonce = Convert.ToBase64String(nonce);
            string base64Payload = Convert.ToBase64String(payload);
            string base64Key = Convert.ToBase64String(key);

            // Decrypt using BCrypt AES-GCM
            string decryptedText = BcryptAesGcm.DecryptAesGcm(base64Payload, base64Key, base64Nonce);
            return Encoding.UTF8.GetBytes(decryptedText);
        }
    }

    /// <summary>
    /// AES-GCM encryption using BCrypt APIs for .NET Framework compatibility.
    /// </summary>
    internal static class BcryptAesGcm
    {
        private const string BCRYPT_AES_ALGORITHM = "AES";
        private const string BCRYPT_CHAINING_MODE = "ChainingMode";
        private const string BCRYPT_CHAIN_MODE_GCM = "ChainingModeGCM";
        private const int STATUS_SUCCESS = 0;

        [DllImport("bcrypt.dll")]
        private static extern int BCryptOpenAlgorithmProvider(
            out IntPtr phAlgorithm,
            [MarshalAs(UnmanagedType.LPWStr)] string pszAlgId,
            [MarshalAs(UnmanagedType.LPWStr)] string? pszImplementation,
            uint dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptSetProperty(
            IntPtr hObject,
            [MarshalAs(UnmanagedType.LPWStr)] string pszProperty,
            byte[] pbInput,
            int cbInput,
            int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptGenerateSymmetricKey(
            IntPtr hAlgorithm,
            out IntPtr phKey,
            IntPtr pbKeyObject,
            int cbKeyObject,
            byte[] pbSecret,
            int cbSecret,
            int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptEncrypt(
            IntPtr hKey,
            byte[] pbInput,
            int cbInput,
            ref BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO pPaddingInfo,
            byte[]? pbIV,
            int cbIV,
            byte[]? pbOutput,
            int cbOutput,
            out int pcbResult,
            int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptDecrypt(
            IntPtr hKey,
            byte[] pbInput,
            int cbInput,
            ref BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO pPaddingInfo,
            byte[]? pbIV,
            int cbIV,
            byte[]? pbOutput,
            int cbOutput,
            out int pcbResult,
            int dwFlags);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptDestroyKey(IntPtr hKey);

        [DllImport("bcrypt.dll")]
        private static extern int BCryptCloseAlgorithmProvider(IntPtr hAlgorithm, int dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        private struct BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO
        {
            public int cbSize;
            public int dwInfoVersion;
            public IntPtr pbNonce;
            public int cbNonce;
            public IntPtr pbAuthData;
            public int cbAuthData;
            public IntPtr pbTag;
            public int cbTag;
            public IntPtr pbMacContext;
            public int cbMacContext;
            public int cbAAD;
            public long cbData;
            public int dwFlags;

            public static BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO Initialize()
            {
                return new BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO
                {
                    cbSize = Marshal.SizeOf(typeof(BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO)),
                    dwInfoVersion = 1
                };
            }
        }

        public static string EncryptAesGcm(string plainText, string base64Key, string base64Nonce)
        {
            if (plainText == null) throw new ArgumentNullException(nameof(plainText));
            if (string.IsNullOrEmpty(base64Key)) throw new ArgumentNullException(nameof(base64Key));
            if (string.IsNullOrEmpty(base64Nonce)) throw new ArgumentNullException(nameof(base64Nonce));

            // Convert Base64 strings to byte arrays
            byte[] key = Convert.FromBase64String(base64Key);
            byte[] nonce = Convert.FromBase64String(base64Nonce);

            if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes", nameof(base64Key));
            if (nonce.Length != 12) throw new ArgumentException("Nonce must be 12 bytes", nameof(base64Nonce));

            IntPtr hAlg = IntPtr.Zero;
            IntPtr hKey = IntPtr.Zero;

            try
            {
                // Initialize algorithm provider
                int status = BCryptOpenAlgorithmProvider(out hAlg, BCRYPT_AES_ALGORITHM, null, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptOpenAlgorithmProvider failed with status " + status);

                // Set GCM mode
                status = BCryptSetProperty(hAlg, BCRYPT_CHAINING_MODE, 
                    Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM), 
                    Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM).Length, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptSetProperty failed with status " + status);

                // Generate key
                status = BCryptGenerateSymmetricKey(hAlg, out hKey, IntPtr.Zero, 0, key, key.Length, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptGenerateSymmetricKey failed with status " + status);

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                const int tagLength = 16;  // GCM tag length

                var authInfo = BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.Initialize();
                var nonceHandle = GCHandle.Alloc(nonce, GCHandleType.Pinned);
                var tagBuffer = new byte[tagLength];
                var tagHandle = GCHandle.Alloc(tagBuffer, GCHandleType.Pinned);

                try
                {
                    authInfo.pbNonce = nonceHandle.AddrOfPinnedObject();
                    authInfo.cbNonce = nonce.Length;
                    authInfo.pbTag = tagHandle.AddrOfPinnedObject();
                    authInfo.cbTag = tagLength;

                    // Get required size
                    int cipherLength;
                    status = BCryptEncrypt(hKey, plainBytes, plainBytes.Length, ref authInfo,
                        null, 0, null, 0, out cipherLength, 0);
                    if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptEncrypt size failed with status " + status);

                    byte[] cipherText = new byte[cipherLength];

                    // Encrypt
                    int bytesWritten;
                    status = BCryptEncrypt(hKey, plainBytes, plainBytes.Length, ref authInfo,
                        null, 0, cipherText, cipherText.Length, out bytesWritten, 0);
                    if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptEncrypt failed with status " + status);

                    // Combine ciphertext and tag
                    byte[] result = new byte[bytesWritten + tagLength];
                    Buffer.BlockCopy(cipherText, 0, result, 0, bytesWritten);
                    Buffer.BlockCopy(tagBuffer, 0, result, bytesWritten, tagLength);

                    // Convert final result to Base64
                    return Convert.ToBase64String(result);
                }
                finally
                {
                    if (nonceHandle.IsAllocated) nonceHandle.Free();
                    if (tagHandle.IsAllocated) tagHandle.Free();
                }
            }
            finally
            {
                if (hKey != IntPtr.Zero) BCryptDestroyKey(hKey);
                if (hAlg != IntPtr.Zero) BCryptCloseAlgorithmProvider(hAlg, 0);
            }
        }

        public static string DecryptAesGcm(string base64CipherText, string base64Key, string base64Nonce)
        {
            if (string.IsNullOrEmpty(base64CipherText)) throw new ArgumentNullException(nameof(base64CipherText));
            if (string.IsNullOrEmpty(base64Key)) throw new ArgumentNullException(nameof(base64Key));
            if (string.IsNullOrEmpty(base64Nonce)) throw new ArgumentNullException(nameof(base64Nonce));

            // Convert Base64 strings to byte arrays
            byte[] cipherText = Convert.FromBase64String(base64CipherText);
            byte[] key = Convert.FromBase64String(base64Key);
            byte[] nonce = Convert.FromBase64String(base64Nonce);

            if (key.Length != 32) throw new ArgumentException("Key must be 32 bytes", nameof(base64Key));
            if (nonce.Length != 12) throw new ArgumentException("Nonce must be 12 bytes", nameof(base64Nonce));

            const int tagLength = 16;
            if (cipherText.Length < tagLength)
                throw new ArgumentException("Encrypted data too short", nameof(base64CipherText));

            IntPtr hAlg = IntPtr.Zero;
            IntPtr hKey = IntPtr.Zero;

            try
            {
                // Initialize algorithm provider
                int status = BCryptOpenAlgorithmProvider(out hAlg, BCRYPT_AES_ALGORITHM, null, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptOpenAlgorithmProvider failed with status " + status);

                // Set GCM mode
                status = BCryptSetProperty(hAlg, BCRYPT_CHAINING_MODE,
                    Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM),
                    Encoding.Unicode.GetBytes(BCRYPT_CHAIN_MODE_GCM).Length, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptSetProperty failed with status " + status);

                // Generate key
                status = BCryptGenerateSymmetricKey(hAlg, out hKey, IntPtr.Zero, 0, key, key.Length, 0);
                if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptGenerateSymmetricKey failed with status " + status);

                // Separate ciphertext and tag
                int encryptedDataLength = cipherText.Length - tagLength;
                byte[] encryptedData = new byte[encryptedDataLength];
                byte[] tag = new byte[tagLength];
                Buffer.BlockCopy(cipherText, 0, encryptedData, 0, encryptedDataLength);
                Buffer.BlockCopy(cipherText, encryptedDataLength, tag, 0, tagLength);

                var authInfo = BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO.Initialize();
                var nonceHandle = GCHandle.Alloc(nonce, GCHandleType.Pinned);
                var tagHandle = GCHandle.Alloc(tag, GCHandleType.Pinned);

                try
                {
                    authInfo.pbNonce = nonceHandle.AddrOfPinnedObject();
                    authInfo.cbNonce = nonce.Length;
                    authInfo.pbTag = tagHandle.AddrOfPinnedObject();
                    authInfo.cbTag = tagLength;

                    // Get required size
                    int plainTextLength;
                    status = BCryptDecrypt(hKey, encryptedData, encryptedData.Length, ref authInfo,
                        null, 0, null, 0, out plainTextLength, 0);
                    if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptDecrypt size failed with status " + status);

                    byte[] plainText = new byte[plainTextLength];

                    // Decrypt
                    int bytesWritten;
                    status = BCryptDecrypt(hKey, encryptedData, encryptedData.Length, ref authInfo,
                        null, 0, plainText, plainText.Length, out bytesWritten, 0);
                    if (status != STATUS_SUCCESS) throw new CryptographicException("BCryptDecrypt failed with status " + status);

                    return Encoding.UTF8.GetString(plainText, 0, bytesWritten);
                }
                finally
                {
                    if (nonceHandle.IsAllocated) nonceHandle.Free();
                    if (tagHandle.IsAllocated) tagHandle.Free();
                }
            }
            finally
            {
                if (hKey != IntPtr.Zero) BCryptDestroyKey(hKey);
                if (hAlg != IntPtr.Zero) BCryptCloseAlgorithmProvider(hAlg, 0);
            }
        }
    }
} 