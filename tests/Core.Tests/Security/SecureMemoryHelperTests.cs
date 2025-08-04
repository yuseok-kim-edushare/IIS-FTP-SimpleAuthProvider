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
        public void ProtectMemory_WithValidLength_WorksCorrectly()
        {
            var validData = new byte[16]; // Multiple of 16
            for (int i = 0; i < validData.Length; i++)
            {
                validData[i] = (byte)i;
            }

            var originalData = new byte[16];
            Array.Copy(validData, originalData, 16);

            var protectResult = SecureMemoryHelper.ProtectMemory(validData);
            
            if (SecureMemoryHelper.IsSupported)
            {
                Assert.IsTrue(protectResult);
                // Data should be modified (encrypted)
                Assert.IsFalse(ArraysEqual(validData, originalData));
                
                // Unprotect should restore original data
                var unprotectResult = SecureMemoryHelper.UnprotectMemory(validData);
                Assert.IsTrue(unprotectResult);
                Assert.IsTrue(ArraysEqual(validData, originalData));
            }
            else
            {
                // On unsupported platforms, protect should return false
                Assert.IsFalse(protectResult);
            }
        }

        [Test]
        public void CreateProtectedCopy_WithValidData_ReturnsProtectedCopy()
        {
            var originalData = new byte[] { 1, 2, 3, 4, 5 };
            var protectedCopy = SecureMemoryHelper.CreateProtectedCopy(originalData);
            
            Assert.IsNotNull(protectedCopy);
            
            if (SecureMemoryHelper.IsSupported)
            {
                // Should be padded to multiple of 16
                Assert.AreEqual(16, protectedCopy.Length);
                // Should not equal original data (it's protected)
                Assert.IsFalse(ArraysEqual(protectedCopy, originalData));
            }
            else
            {
                // On unsupported platforms, should return original
                Assert.AreSame(originalData, protectedCopy);
            }
        }

        [Test]
        public void ExtractProtectedData_WithValidData_ReturnsOriginalData()
        {
            var originalData = new byte[] { 1, 2, 3, 4, 5 };
            var protectedCopy = SecureMemoryHelper.CreateProtectedCopy(originalData);
            
            var extractedData = SecureMemoryHelper.ExtractProtectedData(protectedCopy, originalData.Length);
            
            Assert.IsNotNull(extractedData);
            Assert.AreEqual(originalData.Length, extractedData.Length);
            
            if (SecureMemoryHelper.IsSupported)
            {
                Assert.IsTrue(ArraysEqual(extractedData, originalData));
            }
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