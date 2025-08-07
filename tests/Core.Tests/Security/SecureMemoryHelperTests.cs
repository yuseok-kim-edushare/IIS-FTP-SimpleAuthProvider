using System;
using NUnit.Framework;
using IIS.Ftp.SimpleAuth.Core.Security;

namespace IIS.Ftp.SimpleAuth.Core.Tests.Security
{
    [TestFixture]
    public class SecureMemoryHelperTests
    {
        [Test]
        public void IsSupported_ReturnsTrue()
        {
            // On Windows .NET Framework, ProtectedMemory should be supported
            var isSupported = SecureMemoryHelper.IsSupported;
            Assert.That(isSupported, Is.True);
        }

        [Test]
        public void ProtectMemory_WithNullData_ReturnsFalse()
        {
            var result = SecureMemoryHelper.ProtectMemory(null);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ProtectMemory_WithInvalidLength_ReturnsFalse()
        {
            // ProtectedMemory requires arrays to be multiples of 16 bytes
            var invalidData = new byte[15]; // Not a multiple of 16
            var result = SecureMemoryHelper.ProtectMemory(invalidData);
            Assert.That(result, Is.False);
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
            Assert.That(protectResult, Is.True);
            
            // Data should be modified (encrypted)
            Assert.That(ArraysEqual(validData, originalData), Is.False);
            
            // Unprotect should restore original data
            var unprotectResult = SecureMemoryHelper.UnprotectMemory(validData);
            Assert.That(unprotectResult, Is.True);
            Assert.That(ArraysEqual(validData, originalData), Is.True);
        }

        [Test]
        public void CreateProtectedCopy_WithValidData_ReturnsProtectedCopy()
        {
            var originalData = new byte[] { 1, 2, 3, 4, 5 };
            var protectedCopy = SecureMemoryHelper.CreateProtectedCopy(originalData);
            
            Assert.That(protectedCopy, Is.Not.Null);
            // Should be padded to multiple of 16
            Assert.That(protectedCopy.Length, Is.EqualTo(16));
            // Should not equal original data (it's protected)
            Assert.That(ArraysEqual(protectedCopy, originalData), Is.False);
        }

        [Test]
        public void ExtractProtectedData_WithValidData_ReturnsOriginalData()
        {
            var originalData = new byte[] { 1, 2, 3, 4, 5 };
            var protectedCopy = SecureMemoryHelper.CreateProtectedCopy(originalData);
            
            var extractedData = SecureMemoryHelper.ExtractProtectedData(protectedCopy, originalData.Length);
            
            Assert.That(extractedData, Is.Not.Null);
            Assert.That(extractedData.Length, Is.EqualTo(originalData.Length));
            Assert.That(ArraysEqual(extractedData, originalData), Is.True);
        }

        [Test]
        public void ClearMemory_WithValidData_ClearsArray()
        {
            var data = new byte[] { 1, 2, 3, 4, 5 };
            SecureMemoryHelper.ClearMemory(data);
            
            // All bytes should be zero
            foreach (var b in data)
            {
                Assert.That(b, Is.EqualTo(0));
            }
        }

        [Test]
        public void ClearMemory_WithNullData_DoesNotThrow()
        {
            try
            {
                SecureMemoryHelper.ClearMemory(null);
                // If we reach here, no exception was thrown, which is expected
            }
            catch
            {
                Assert.Fail("ClearMemory should not throw when given null data");
            }
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

            Assert.That(actionExecuted, Is.True);
            Assert.That(dataSeenInAction, Is.Not.Null);
            Assert.That(dataSeenInAction.Length, Is.EqualTo(data.Length));
            Assert.That(result, Is.True); // Should return true on Windows
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
