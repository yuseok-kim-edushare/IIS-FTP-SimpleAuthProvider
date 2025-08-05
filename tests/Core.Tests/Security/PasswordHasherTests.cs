using IIS.Ftp.SimpleAuth.Core.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IIS.Ftp.SimpleAuth.Core.Tests.Security
{
    [TestClass]
    public class PasswordHasherTests
    {
        #region BCrypt Tests

        [TestMethod]
        public void HashPasswordBCrypt_ValidPassword_ShouldReturnBCryptHash()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash = PasswordHasher.HashPasswordBCrypt(password);

            // Assert
            Assert.IsNotNull(hash);
            Assert.IsTrue(!string.IsNullOrEmpty(hash));
            Assert.IsTrue(hash.StartsWith("$2a$") || hash.StartsWith("$2b$"), "BCrypt hash should start with $2a$ or $2b$");
        }

        [TestMethod]
        public void HashPasswordBCrypt_SamePassword_ShouldReturnDifferentHashes()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash1 = PasswordHasher.HashPasswordBCrypt(password);
            var hash2 = PasswordHasher.HashPasswordBCrypt(password);

            // Assert
            Assert.AreNotEqual(hash1, hash2, "BCrypt should generate different hashes for same password (due to random salt)");
        }

        [TestMethod]
        public void Verify_BCryptHash_CorrectPassword_ShouldReturnTrue()
        {
            // Arrange
            var password = "TestPassword123!";
            var hash = PasswordHasher.HashPasswordBCrypt(password);

            // Act
            var isValid = PasswordHasher.Verify(password, "", hash); // Salt is empty for BCrypt

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void Verify_BCryptHash_IncorrectPassword_ShouldReturnFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var wrongPassword = "WrongPassword456!";
            var hash = PasswordHasher.HashPasswordBCrypt(password);

            // Act
            var isValid = PasswordHasher.Verify(wrongPassword, "", hash); // Salt is empty for BCrypt

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void DetectAlgorithm_BCryptHash_ShouldReturnBCrypt()
        {
            // Arrange
            var password = "TestPassword123!";
            var hash = PasswordHasher.HashPasswordBCrypt(password);

            // Act
            var algorithm = PasswordHasher.DetectAlgorithm(hash);

            // Assert
            Assert.AreEqual("BCrypt", algorithm);
        }

        [TestMethod]
        [DataRow("$2a$12$R9h/cIPz0gi.URNNX3kh2OPST9/PgBkqquzi.Ss7KIUgO2t0jWMUW")]
        [DataRow("$2b$10$N9qo8uLOickgx2ZMRZoMye8YjEj2e0T/F8KJnN9P.jBJ8JEOKyJ.K")]
        [DataRow("$2x$10$3VEMj9fTKS5FdGqJJ/aJq.yDgJ9vJ6d9sJdZ.aF2.fE2eQ2sJ8yTK")]
        [DataRow("$2y$12$8J9lOQjJ7A8O8Q8A8A8A8.8A8A8A8A8A8A8A8A8A8A8A8A8A8A8A8A")]
        public void DetectAlgorithm_VariousBCryptFormats_ShouldReturnBCrypt(string hash)
        {
            // Act
            var algorithm = PasswordHasher.DetectAlgorithm(hash);

            // Assert
            Assert.AreEqual("BCrypt", algorithm);
        }

        #endregion

        #region PBKDF2 Tests (Legacy Support)

        [TestMethod]
        public void GenerateSalt_DefaultSize_ShouldReturnBase64String()
        {
            // Act
            var salt = PasswordHasher.GenerateSalt();

            // Assert
            Assert.IsNotNull(salt);
            Assert.IsTrue(!string.IsNullOrEmpty(salt));
            
            // Should be valid base64
            var saltBytes = Convert.FromBase64String(salt);
            Assert.AreEqual(16, saltBytes.Length); // Default size is 16 bytes
        }

        [TestMethod]
        [DataRow(8)]
        [DataRow(16)]
        [DataRow(32)]
        [DataRow(64)]
        public void GenerateSalt_CustomSize_ShouldReturnCorrectSize(int sizeBytes)
        {
            // Act
            var salt = PasswordHasher.GenerateSalt(sizeBytes);

            // Assert
            Assert.IsNotNull(salt);
            Assert.IsTrue(!string.IsNullOrEmpty(salt));
            
            var saltBytes = Convert.FromBase64String(salt);
            Assert.AreEqual(sizeBytes, saltBytes.Length);
        }

        [TestMethod]
        public void GenerateSalt_MultipleCalls_ShouldReturnDifferentValues()
        {
            // Act
            var salt1 = PasswordHasher.GenerateSalt();
            var salt2 = PasswordHasher.GenerateSalt();
            var salt3 = PasswordHasher.GenerateSalt();

            // Assert
            Assert.AreNotEqual(salt1, salt2);
            Assert.AreNotEqual(salt2, salt3);
            Assert.AreNotEqual(salt1, salt3);
        }

        [TestMethod]
        public void HashPasswordPBKDF2_ValidInputs_ShouldReturnBase64Hash()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();

            // Act
            var hash = PasswordHasher.HashPasswordPBKDF2(password, salt);

            // Assert
            Assert.IsNotNull(hash);
            Assert.IsTrue(!string.IsNullOrEmpty(hash));
            
            // Should be valid base64
            var hashBytes = Convert.FromBase64String(hash);
            Assert.AreEqual(32, hashBytes.Length); // 256 bits = 32 bytes
        }

        [TestMethod]
        public void HashPasswordPBKDF2_SameInputs_ShouldReturnSameHash()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();

            // Act
            var hash1 = PasswordHasher.HashPasswordPBKDF2(password, salt);
            var hash2 = PasswordHasher.HashPasswordPBKDF2(password, salt);

            // Assert
            Assert.AreEqual(hash1, hash2);
        }

        [TestMethod]
        public void HashPasswordPBKDF2_DifferentPasswords_ShouldReturnDifferentHashes()
        {
            // Arrange
            var password1 = "Password1";
            var password2 = "Password2";
            var salt = PasswordHasher.GenerateSalt();

            // Act
            var hash1 = PasswordHasher.HashPasswordPBKDF2(password1, salt);
            var hash2 = PasswordHasher.HashPasswordPBKDF2(password2, salt);

            // Assert
            Assert.AreNotEqual(hash1, hash2);
        }

        [TestMethod]
        public void HashPasswordPBKDF2_DifferentSalts_ShouldReturnDifferentHashes()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt1 = PasswordHasher.GenerateSalt();
            var salt2 = PasswordHasher.GenerateSalt();

            // Act
            var hash1 = PasswordHasher.HashPasswordPBKDF2(password, salt1);
            var hash2 = PasswordHasher.HashPasswordPBKDF2(password, salt2);

            // Assert
            Assert.AreNotEqual(hash1, hash2);
        }

        [TestMethod]
        [DataRow(1000)]
        [DataRow(10000)]
        [DataRow(100000)]
        public void HashPasswordPBKDF2_DifferentIterations_ShouldReturnDifferentHashes(int iterations)
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();

            // Act
            var hash1 = PasswordHasher.HashPasswordPBKDF2(password, salt, iterations);
            var hash2 = PasswordHasher.HashPasswordPBKDF2(password, salt, iterations + 1000);

            // Assert
            Assert.AreNotEqual(hash1, hash2);
        }

        [TestMethod]
        public void Verify_PBKDF2Hash_CorrectPassword_ShouldReturnTrue()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPasswordPBKDF2(password, salt);

            // Act
            var isValid = PasswordHasher.Verify(password, salt, hash);

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void Verify_PBKDF2Hash_IncorrectPassword_ShouldReturnFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var wrongPassword = "WrongPassword456!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPasswordPBKDF2(password, salt);

            // Act
            var isValid = PasswordHasher.Verify(wrongPassword, salt, hash);

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void Verify_PBKDF2Hash_IncorrectSalt_ShouldReturnFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();
            var wrongSalt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPasswordPBKDF2(password, salt);

            // Act
            var isValid = PasswordHasher.Verify(password, wrongSalt, hash);

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void Verify_PBKDF2Hash_IncorrectHash_ShouldReturnFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPasswordPBKDF2(password, salt);
            var wrongHash = PasswordHasher.HashPasswordPBKDF2("WrongPassword", salt);

            // Act
            var isValid = PasswordHasher.Verify(password, salt, wrongHash);

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        [DataRow(1000)]
        [DataRow(50000)]
        [DataRow(200000)]
        public void Verify_PBKDF2Hash_CustomIterations_ShouldWork(int iterations)
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPasswordPBKDF2(password, salt, iterations);

            // Act
            var isValid = PasswordHasher.Verify(password, salt, hash, iterations);

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public void Verify_PBKDF2Hash_MismatchedIterations_ShouldReturnFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPasswordPBKDF2(password, salt, 100000);

            // Act
            var isValid = PasswordHasher.Verify(password, salt, hash, 50000);

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void DetectAlgorithm_PBKDF2Hash_ShouldReturnPBKDF2()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPasswordPBKDF2(password, salt);

            // Act
            var algorithm = PasswordHasher.DetectAlgorithm(hash);

            // Assert
            Assert.AreEqual("PBKDF2", algorithm);
        }

        [TestMethod]
        public void DetectAlgorithm_EmptyHash_ShouldReturnBCryptAsDefault()
        {
            // Act
            var algorithm = PasswordHasher.DetectAlgorithm("");

            // Assert
            Assert.AreEqual("BCrypt", algorithm);
        }

        [TestMethod]
        public void DetectAlgorithm_NullHash_ShouldReturnBCryptAsDefault()
        {
            // Act
            var algorithm = PasswordHasher.DetectAlgorithm(null);

            // Assert
            Assert.AreEqual("BCrypt", algorithm);
        }

        #endregion

        #region Legacy Method Tests

        [TestMethod]
        public void HashPassword_LegacyMethod_ShouldWorkWithPBKDF2()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();

            // Act
            var hash = PasswordHasher.HashPassword(password, salt);

            // Assert
            Assert.IsNotNull(hash);
            Assert.IsTrue(!string.IsNullOrEmpty(hash));
            var hashBytes = Convert.FromBase64String(hash);
            Assert.AreEqual(32, hashBytes.Length);
        }

        [TestMethod]
        public void HashPassword_EmptyPassword_ShouldNotThrow()
        {
            // Arrange
            var password = string.Empty;
            var salt = PasswordHasher.GenerateSalt();

            // Act & Assert
            var hash = PasswordHasher.HashPassword(password, salt);
            Assert.IsNotNull(hash);
            Assert.IsTrue(!string.IsNullOrEmpty(hash));
        }

        [TestMethod]
        public void Verify_EmptyPassword_ShouldWork()
        {
            // Arrange
            var password = string.Empty;
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPassword(password, salt);

            // Act
            var isValid = PasswordHasher.Verify(password, salt, hash);

            // Assert
            Assert.IsTrue(isValid);
        }

        #endregion

        #region Mixed Algorithm Tests

        [TestMethod]
        public void Verify_AutoDetection_ShouldWorkForBothAlgorithms()
        {
            // Arrange
            var password = "TestPassword123!";
            
            // BCrypt
            var bcryptHash = PasswordHasher.HashPasswordBCrypt(password);
            
            // PBKDF2  
            var salt = PasswordHasher.GenerateSalt();
            var pbkdf2Hash = PasswordHasher.HashPasswordPBKDF2(password, salt);

            // Act & Assert
            Assert.IsTrue(PasswordHasher.Verify(password, "", bcryptHash), "BCrypt verification should work");
            Assert.IsTrue(PasswordHasher.Verify(password, salt, pbkdf2Hash), "PBKDF2 verification should work");
            
            Assert.IsFalse(PasswordHasher.Verify("wrong", "", bcryptHash), "BCrypt verification should fail with wrong password");
            Assert.IsFalse(PasswordHasher.Verify("wrong", salt, pbkdf2Hash), "PBKDF2 verification should fail with wrong password");
        }

        #endregion
    }
} 