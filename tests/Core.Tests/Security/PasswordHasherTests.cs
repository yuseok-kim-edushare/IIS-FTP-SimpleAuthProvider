using IIS.Ftp.SimpleAuth.Core.Security;
using NUnit.Framework;
using System;

namespace IIS.Ftp.SimpleAuth.Core.Tests.Security
{
    [TestFixture]
    public class PasswordHasherTests
    {
        #region BCrypt Tests

        [Test]
        public void HashPasswordBCrypt_ValidPassword_ShouldReturnBCryptHash()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash = PasswordHasher.HashPasswordBCrypt(password);

            // Assert
            Assert.That(hash, Is.Not.Null);
            Assert.That(!string.IsNullOrEmpty(hash), Is.True);
            Assert.That(hash.StartsWith("$2a$") || hash.StartsWith("$2b$"), Is.True, "BCrypt hash should start with $2a$ or $2b$");
        }

        [Test]
        public void HashPasswordBCrypt_SamePassword_ShouldReturnDifferentHashes()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash1 = PasswordHasher.HashPasswordBCrypt(password);
            var hash2 = PasswordHasher.HashPasswordBCrypt(password);

            // Assert
            Assert.That(hash1, Is.Not.EqualTo(hash2), "BCrypt should generate different hashes for same password (due to random salt)");
        }

        [Test]
        public void Verify_BCryptHash_CorrectPassword_ShouldReturnTrue()
        {
            // Arrange
            var password = "TestPassword123!";
            var hash = PasswordHasher.HashPasswordBCrypt(password);

            // Act
            var isValid = PasswordHasher.Verify(password, "", hash); // Salt is empty for BCrypt

            // Assert
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void Verify_BCryptHash_IncorrectPassword_ShouldReturnFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var wrongPassword = "WrongPassword456!";
            var hash = PasswordHasher.HashPasswordBCrypt(password);

            // Act
            var isValid = PasswordHasher.Verify(wrongPassword, "", hash); // Salt is empty for BCrypt

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void DetectAlgorithm_BCryptHash_ShouldReturnBCrypt()
        {
            // Arrange
            var password = "TestPassword123!";
            var hash = PasswordHasher.HashPasswordBCrypt(password);

            // Act
            var algorithm = PasswordHasher.DetectAlgorithm(hash);

            // Assert
            Assert.That(algorithm, Is.EqualTo("BCrypt"));
        }

        [Test]
        [TestCase("$2a$12$R9h/cIPz0gi.URNNX3kh2OPST9/PgBkqquzi.Ss7KIUgO2t0jWMUW")]
        [TestCase("$2b$10$N9qo8uLOickgx2ZMRZoMye8YjEj2e0T/F8KJnN9P.jBJ8JEOKyJ.K")]
        [TestCase("$2x$10$3VEMj9fTKS5FdGqJJ/aJq.yDgJ9vJ6d9sJdZ.aF2.fE2eQ2sJ8yTK")]
        [TestCase("$2y$12$8J9lOQjJ7A8O8Q8A8A8A8.8A8A8A8A8A8A8A8A8A8A8A8A8A8A8A8A")]
        public void DetectAlgorithm_VariousBCryptFormats_ShouldReturnBCrypt(string hash)
        {
            // Act
            var algorithm = PasswordHasher.DetectAlgorithm(hash);

            // Assert
            Assert.That(algorithm, Is.EqualTo("BCrypt"));
        }

        #endregion

        #region PBKDF2 Tests (Legacy Support)

        [Test]
        public void GenerateSalt_DefaultSize_ShouldReturnBase64String()
        {
            // Act
            var salt = PasswordHasher.GenerateSalt();

            // Assert
            Assert.That(salt, Is.Not.Null);
            Assert.That(!string.IsNullOrEmpty(salt), Is.True);
            
            // Should be valid base64
            var saltBytes = Convert.FromBase64String(salt);
            Assert.That(saltBytes.Length, Is.EqualTo(16)); // Default size is 16 bytes
        }

        [Test]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(64)]
        public void GenerateSalt_CustomSize_ShouldReturnCorrectSize(int sizeBytes)
        {
            // Act
            var salt = PasswordHasher.GenerateSalt(sizeBytes);

            // Assert
            Assert.That(salt, Is.Not.Null);
            Assert.That(!string.IsNullOrEmpty(salt), Is.True);
            
            var saltBytes = Convert.FromBase64String(salt);
            Assert.That(saltBytes.Length, Is.EqualTo(sizeBytes));
        }

        [Test]
        public void GenerateSalt_MultipleCalls_ShouldReturnDifferentValues()
        {
            // Act
            var salt1 = PasswordHasher.GenerateSalt();
            var salt2 = PasswordHasher.GenerateSalt();
            var salt3 = PasswordHasher.GenerateSalt();

            // Assert
            Assert.That(salt1, Is.Not.EqualTo(salt2));
            Assert.That(salt2, Is.Not.EqualTo(salt3));
            Assert.That(salt1, Is.Not.EqualTo(salt3));
        }

        [Test]
        public void HashPasswordPBKDF2_ValidInputs_ShouldReturnBase64Hash()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();

            // Act
            var hash = PasswordHasher.HashPasswordPBKDF2(password, salt);

            // Assert
            Assert.That(hash, Is.Not.Null);
            Assert.That(!string.IsNullOrEmpty(hash), Is.True);
            
            // Should be valid base64
            var hashBytes = Convert.FromBase64String(hash);
            Assert.That(hashBytes.Length, Is.EqualTo(32)); // 256 bits = 32 bytes
        }

        [Test]
        public void HashPasswordPBKDF2_SameInputs_ShouldReturnSameHash()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();

            // Act
            var hash1 = PasswordHasher.HashPasswordPBKDF2(password, salt);
            var hash2 = PasswordHasher.HashPasswordPBKDF2(password, salt);

            // Assert
            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
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
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
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
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        [TestCase(1000)]
        [TestCase(10000)]
        [TestCase(100000)]
        public void HashPasswordPBKDF2_DifferentIterations_ShouldReturnDifferentHashes(int iterations)
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();

            // Act
            var hash1 = PasswordHasher.HashPasswordPBKDF2(password, salt, iterations);
            var hash2 = PasswordHasher.HashPasswordPBKDF2(password, salt, iterations + 1000);

            // Assert
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void Verify_PBKDF2Hash_CorrectPassword_ShouldReturnTrue()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPasswordPBKDF2(password, salt);

            // Act
            var isValid = PasswordHasher.Verify(password, salt, hash);

            // Assert
            Assert.That(isValid, Is.True);
        }

        [Test]
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
            Assert.That(isValid, Is.False);
        }

        [Test]
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
            Assert.That(isValid, Is.False);
        }

        [Test]
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
            Assert.That(isValid, Is.False);
        }

        [Test]
        [TestCase(1000)]
        [TestCase(50000)]
        [TestCase(200000)]
        public void Verify_PBKDF2Hash_CustomIterations_ShouldWork(int iterations)
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPasswordPBKDF2(password, salt, iterations);

            // Act
            var isValid = PasswordHasher.Verify(password, salt, hash, iterations);

            // Assert
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void Verify_PBKDF2Hash_MismatchedIterations_ShouldReturnFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPasswordPBKDF2(password, salt, 100000);

            // Act
            var isValid = PasswordHasher.Verify(password, salt, hash, 50000);

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void DetectAlgorithm_PBKDF2Hash_ShouldReturnPBKDF2()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPasswordPBKDF2(password, salt);

            // Act
            var algorithm = PasswordHasher.DetectAlgorithm(hash);

            // Assert
            Assert.That(algorithm, Is.EqualTo("PBKDF2"));
        }

        [Test]
        public void DetectAlgorithm_EmptyHash_ShouldReturnBCryptAsDefault()
        {
            // Act
            var algorithm = PasswordHasher.DetectAlgorithm("");

            // Assert
            Assert.That(algorithm, Is.EqualTo("BCrypt"));
        }

        [Test]
        public void DetectAlgorithm_NullHash_ShouldReturnBCryptAsDefault()
        {
            // Act
            var algorithm = PasswordHasher.DetectAlgorithm(null);

            // Assert
            Assert.That(algorithm, Is.EqualTo("BCrypt"));
        }

        #endregion

        #region Legacy Method Tests

        [Test]
        public void HashPassword_LegacyMethod_ShouldWorkWithPBKDF2()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();

            // Act
            var hash = PasswordHasher.HashPassword(password, salt);

            // Assert
            Assert.That(hash, Is.Not.Null);
            Assert.That(!string.IsNullOrEmpty(hash), Is.True);
            var hashBytes = Convert.FromBase64String(hash);
            Assert.That(hashBytes.Length, Is.EqualTo(32));
        }

        [Test]
        public void HashPassword_EmptyPassword_ShouldNotThrow()
        {
            // Arrange
            var password = string.Empty;
            var salt = PasswordHasher.GenerateSalt();

            // Act & Assert
            var hash = PasswordHasher.HashPassword(password, salt);
            Assert.That(hash, Is.Not.Null);
            Assert.That(!string.IsNullOrEmpty(hash), Is.True);
        }

        [Test]
        public void Verify_EmptyPassword_ShouldWork()
        {
            // Arrange
            var password = string.Empty;
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPassword(password, salt);

            // Act
            var isValid = PasswordHasher.Verify(password, salt, hash);

            // Assert
            Assert.That(isValid, Is.True);
        }

        #endregion

        #region Mixed Algorithm Tests

        [Test]
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
            Assert.That(PasswordHasher.Verify(password, "", bcryptHash), Is.True, "BCrypt verification should work");
            Assert.That(PasswordHasher.Verify(password, salt, pbkdf2Hash), Is.True, "PBKDF2 verification should work");
            
            Assert.That(PasswordHasher.Verify("wrong", "", bcryptHash), Is.False, "BCrypt verification should fail with wrong password");
            Assert.That(PasswordHasher.Verify("wrong", salt, pbkdf2Hash), Is.False, "PBKDF2 verification should fail with wrong password");
        }

        #endregion
    }
} 
