using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace IIS.Ftp.SimpleAuth.Core.Security
{
    /// <summary>
    /// Helper class for protecting sensitive data in memory using ProtectedMemory.
    /// Only works on Windows platforms.
    /// </summary>
    public static class SecureMemoryHelper
    {
        /// <summary>
        /// Checks if the current platform supports ProtectedMemory.
        /// </summary>
        public static bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Protects a byte array in memory using ProtectedMemory.
        /// The array will be modified in place and must be a multiple of 16 bytes.
        /// </summary>
        /// <param name="data">The byte array to protect. Must be a multiple of 16 bytes.</param>
        /// <returns>True if protection was successful, false if not supported or failed.</returns>
        public static bool ProtectMemory(byte[] data)
        {
            if (!IsSupported || data == null)
                return false;

            try
            {
                // ProtectedMemory requires arrays to be multiples of 16 bytes
                if (data.Length % 16 != 0)
                    return false;

                ProtectedMemory.Protect(data, MemoryProtectionScope.SameProcess);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        /// <summary>
        /// Unprotects a byte array in memory using ProtectedMemory.
        /// The array will be modified in place.
        /// </summary>
        /// <param name="data">The byte array to unprotect.</param>
        /// <returns>True if unprotection was successful, false if not supported or failed.</returns>
        public static bool UnprotectMemory(byte[] data)
        {
            if (!IsSupported || data == null)
                return false;

            try
            {
                ProtectedMemory.Unprotect(data, MemoryProtectionScope.SameProcess);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a protected copy of the source data, padded to 16-byte alignment if necessary.
        /// </summary>
        /// <param name="source">The source data to protect.</param>
        /// <returns>A new protected byte array, or the original if protection is not supported.</returns>
        public static byte[] CreateProtectedCopy(byte[] source)
        {
            if (!IsSupported || source == null)
                return source;

            // Calculate required size (multiple of 16 bytes)
            int paddedLength = ((source.Length + 15) / 16) * 16;
            byte[] protectedData = new byte[paddedLength];
            
            // Copy source data
            Array.Copy(source, protectedData, source.Length);
            
            // Protect in memory
            if (ProtectMemory(protectedData))
            {
                return protectedData;
            }

            // If protection failed, return original
            return source;
        }

        /// <summary>
        /// Unprotects and extracts the original data from a protected array.
        /// </summary>
        /// <param name="protectedData">The protected data array.</param>
        /// <param name="originalLength">The original length of the data before padding.</param>
        /// <returns>The unprotected data with original length, or an empty array if unprotection failed.</returns>
        public static byte[] ExtractProtectedData(byte[] protectedData, int originalLength)
        {
            if (!IsSupported || protectedData == null)
                return Array.Empty<byte>();

            // Create a copy to avoid modifying the original protected data
            byte[] workingCopy = new byte[protectedData.Length];
            Array.Copy(protectedData, workingCopy, protectedData.Length);

            if (UnprotectMemory(workingCopy))
            {
                // Extract only the original data length
                byte[] result = new byte[originalLength];
                Array.Copy(workingCopy, result, Math.Min(originalLength, workingCopy.Length));
                
                // Clear the working copy
                Array.Clear(workingCopy, 0, workingCopy.Length);
                
                return result;
            }

            return null;
        }

        /// <summary>
        /// Safely clears a byte array from memory.
        /// </summary>
        /// <param name="data">The byte array to clear.</param>
        public static void ClearMemory(byte[] data)
        {
            if (data != null)
            {
                Array.Clear(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Executes an action with temporarily unprotected data, then re-protects it.
        /// </summary>
        /// <param name="protectedData">The protected data array.</param>
        /// <param name="action">Action to execute with unprotected data.</param>
        /// <returns>True if the operation was successful.</returns>
        public static bool WithUnprotectedData(byte[] protectedData, Action<byte[]> action)
        {
            if (!IsSupported || protectedData == null || action == null)
            {
                action?.Invoke(protectedData);
                return false;
            }

            try
            {
                // Unprotect
                if (!UnprotectMemory(protectedData))
                    return false;

                // Execute action
                action(protectedData);

                // Re-protect
                return ProtectMemory(protectedData);
            }
            catch
            {
                // Try to re-protect even if action failed
                try
                {
                    ProtectMemory(protectedData);
                }
                catch
                {
                    // Silent fail on re-protection attempt
                }
                return false;
            }
        }
    }
}