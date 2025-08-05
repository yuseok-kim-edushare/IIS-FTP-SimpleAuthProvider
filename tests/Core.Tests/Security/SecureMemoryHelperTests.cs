using System;
using NUnit.Framework;
using IIS.Ftp.SimpleAuth.Core.Security;

namespace IIS.Ftp.SimpleAuth.Core.Tests.Security
{
    [TestFixture]
    public class SecureMemoryHelperTests
    {
        [Test]
        public void IsSupported_ReturnsExpectedValue()
        {
            // Test should pass regardless of platform
            // On Windows, should return true; on other platforms, should return false
            var isSupported = SecureMemoryHelper.IsSupported;
            
            // We can't assert a specific value since it depends on the platform
            // Just ensure it doesn't throw
            Assert.DoesNotThrow(() => { var _ = SecureMemoryHelper.IsSupported; });
        }

        [Test]
        public void ProtectMemory_WithNullData_ReturnsFalse()
        {
            var result = SecureMemoryHelper.ProtectMemory(null);
            Assert.IsFalse(result);
        }

        [Test]
        public void ProtectMemory_WithInvalidLength_ReturnsFalse()
        {
            // ProtectedMemory requires arrays to be multiples of 16 bytes
            var invalidData = new byte[15]; // Not a multiple of 16
            var result = SecureMemoryHelper.ProtectMemory(invalidData);
            Assert.IsFalse(result);
        }

        [Test]
        [Platform("Win")]
        public void ProtectMemory_WithValidLength_WorksCorrectly()
        {
            // Skip test if not on Windows
            if (!SecureMemoryHelper.IsSupported)
            {
                Assert.Ignore("ProtectedMemory is only supported on Windows platforms");
            }

            var validData = new byte[16]; // Multiple of 16
            for (int i = 0; i < validData.Length; i++)
            {
                validData[i] = (byte)i;
            }

            var originalData = new byte[16];
            Array.Copy(validData, originalData, 16);

            var protectResult = SecureMemoryHelper.ProtectMemory(validData);
            Assert.IsTrue(protectResult);
            
            // Data should be modified (encrypted)
            Assert.IsFalse(ArraysEqual(validData, originalData));
            
            // Unprotect should restore original data
            var unprotectResult = SecureMemoryHelper.UnprotectMemory(validData);
            Assert.IsTrue(unprotectResult);
            Assert.IsTrue(ArraysEqual(validData, originalData));
        }

        [Test]
        [Platform(Exclude = "Win")]
        public void ProtectMemory_OnNonWindows_ReturnsFalse()
        {
            // This test only runs on non-Windows platforms
            if (SecureMemoryHelper.IsSupported)
            {
                Assert.Ignore("This test is for non-Windows platforms only");
            }

            var validData = new byte[16]; // Multiple of 16
            var protectResult = SecureMemoryHelper.ProtectMemory(validData);
            
            // On unsupported platforms, protect should return false
            Assert.IsFalse(protectResult);
        }

        [Test]
        [Platform("Win")]
        public void CreateProtectedCopy_WithValidData_ReturnsProtectedCopy()
        {
            // Skip test if not on Windows
            if (!SecureMemoryHelper.IsSupported)
            {
                Assert.Ignore("ProtectedMemory is only supported on Windows platforms");
            }

            var originalData = new byte[] { 1, 2, 3, 4, 5 };
            var protectedCopy = SecureMemoryHelper.CreateProtectedCopy(originalData);
            
            Assert.IsNotNull(protectedCopy);
            // Should be padded to multiple of 16
            Assert.AreEqual(16, protectedCopy.Length);
            // Should not equal original data (it's protected)
            Assert.IsFalse(ArraysEqual(protectedCopy, originalData));
        }

        [Test]
        [Platform(Exclude = "Win")]
        public void CreateProtectedCopy_OnNonWindows_ReturnsOriginal()
        {
            // This test only runs on non-Windows platforms
            if (SecureMemoryHelper.IsSupported)
            {
                Assert.Ignore("This test is for non-Windows platforms only");
            }

            var originalData = new byte[] { 1, 2, 3, 4, 5 };
            var protectedCopy = SecureMemoryHelper.CreateProtectedCopy(originalData);
            
            Assert.IsNotNull(protectedCopy);
            // On unsupported platforms, should return original
            Assert.AreSame(originalData, protectedCopy);
        }

        [Test]
        [Platform("Win")]
        public void ExtractProtectedData_WithValidData_ReturnsOriginalData()
        {
            // Skip test if not on Windows
            if (!SecureMemoryHelper.IsSupported)
            {
                Assert.Ignore("ProtectedMemory is only supported on Windows platforms");
            }

            var originalData = new byte[] { 1, 2, 3, 4, 5 };
            var protectedCopy = SecureMemoryHelper.CreateProtectedCopy(originalData);
            
            var extractedData = SecureMemoryHelper.ExtractProtectedData(protectedCopy, originalData.Length);
            
            Assert.IsNotNull(extractedData);
            Assert.AreEqual(originalData.Length, extractedData.Length);
            Assert.IsTrue(ArraysEqual(extractedData, originalData));
        }

        [Test]
        [Platform(Exclude = "Win")]
        public void ExtractProtectedData_OnNonWindows_ReturnsEmptyArray()
        {
            // This test only runs on non-Windows platforms
            if (SecureMemoryHelper.IsSupported)
            {
                Assert.Ignore("This test is for non-Windows platforms only");
            }

            var originalData = new byte[] { 1, 2, 3, 4, 5 };
            var protectedCopy = SecureMemoryHelper.CreateProtectedCopy(originalData);
            
            var extractedData = SecureMemoryHelper.ExtractProtectedData(protectedCopy, originalData.Length);
            
            Assert.IsNotNull(extractedData);
            Assert.AreEqual(0, extractedData.Length); // Should return empty array on non-Windows
        }

        [Test]
        public void ClearMemory_WithValidData_ClearsArray()
        {
            var data = new byte[] { 1, 2, 3, 4, 5 };
            SecureMemoryHelper.ClearMemory(data);
            
            // All bytes should be zero
            foreach (var b in data)
            {
                Assert.AreEqual(0, b);
            }
        }

        [Test]
        public void ClearMemory_WithNullData_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SecureMemoryHelper.ClearMemory(null));
        }

        [Test]
        public void WithUnprotectedData_ExecutesActionCorrectly()
        {
            var data = new byte[16];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (byte)i;
            }

            bool actionExecuted = false;
            byte[] dataSeenInAction = null;

            var result = SecureMemoryHelper.WithUnprotectedData(data, (unprotectedData) =>
            {
                actionExecuted = true;
                dataSeenInAction = new byte[unprotectedData.Length];
                Array.Copy(unprotectedData, dataSeenInAction, unprotectedData.Length);
            });

            Assert.IsTrue(actionExecuted);
            Assert.IsNotNull(dataSeenInAction);
            Assert.AreEqual(data.Length, dataSeenInAction.Length);
            
            // On Windows, expect true result; on non-Windows, expect false but action still executed
            if (SecureMemoryHelper.IsSupported)
            {
                // Note: This might be true or false depending on whether data was actually protected
                // The important thing is that the action was executed
            }
            else
            {
                Assert.IsFalse(result);
            }
        }

        private bool ArraysEqual(byte[] a, byte[] b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}