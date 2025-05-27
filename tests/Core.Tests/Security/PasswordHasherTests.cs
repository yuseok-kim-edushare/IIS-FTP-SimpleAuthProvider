using IIS.Ftp.SimpleAuth.Core.Security;
using NUnit.Framework;
using System;

namespace IIS.Ftp.SimpleAuth.Core.Tests.Security
{
    [TestFixture]
    public class PasswordHasherTests
    {
        [Test]
        public void GenerateSalt_DefaultSize_ShouldReturnBase64String()
        {
            // Act
            var salt = PasswordHasher.GenerateSalt();

            // Assert
            Assert.That(salt, Is.Not.Null.And.Not.Empty);
            
            // Should be valid base64
            var saltBytes = Convert.FromBase64String(salt);
            Assert.That(saltBytes, Has.Length.EqualTo(16)); // Default size is 16 bytes
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
            Assert.That(salt, Is.Not.Null.And.Not.Empty);
            
            var saltBytes = Convert.FromBase64String(salt);
            Assert.That(saltBytes, Has.Length.EqualTo(sizeBytes));
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
        public void HashPassword_ValidInputs_ShouldReturnBase64Hash()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();

            // Act
            var hash = PasswordHasher.HashPassword(password, salt);

            // Assert
            Assert.That(hash, Is.Not.Null.And.Not.Empty);
            
            // Should be valid base64
            var hashBytes = Convert.FromBase64String(hash);
            Assert.That(hashBytes, Has.Length.EqualTo(32)); // 256 bits = 32 bytes
        }

        [Test]
        public void HashPassword_SameInputs_ShouldReturnSameHash()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();

            // Act
            var hash1 = PasswordHasher.HashPassword(password, salt);
            var hash2 = PasswordHasher.HashPassword(password, salt);

            // Assert
            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void HashPassword_DifferentPasswords_ShouldReturnDifferentHashes()
        {
            // Arrange
            var password1 = "Password1";
            var password2 = "Password2";
            var salt = PasswordHasher.GenerateSalt();

            // Act
            var hash1 = PasswordHasher.HashPassword(password1, salt);
            var hash2 = PasswordHasher.HashPassword(password2, salt);

            // Assert
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void HashPassword_DifferentSalts_ShouldReturnDifferentHashes()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt1 = PasswordHasher.GenerateSalt();
            var salt2 = PasswordHasher.GenerateSalt();

            // Act
            var hash1 = PasswordHasher.HashPassword(password, salt1);
            var hash2 = PasswordHasher.HashPassword(password, salt2);

            // Assert
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        [TestCase(1000)]
        [TestCase(10000)]
        [TestCase(100000)]
        public void HashPassword_DifferentIterations_ShouldReturnDifferentHashes(int iterations)
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();

            // Act
            var hash1 = PasswordHasher.HashPassword(password, salt, iterations);
            var hash2 = PasswordHasher.HashPassword(password, salt, iterations + 1000);

            // Assert
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void Verify_CorrectPassword_ShouldReturnTrue()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPassword(password, salt);

            // Act
            var isValid = PasswordHasher.Verify(password, salt, hash);

            // Assert
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void Verify_IncorrectPassword_ShouldReturnFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var wrongPassword = "WrongPassword456!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPassword(password, salt);

            // Act
            var isValid = PasswordHasher.Verify(wrongPassword, salt, hash);

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void Verify_IncorrectSalt_ShouldReturnFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();
            var wrongSalt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPassword(password, salt);

            // Act
            var isValid = PasswordHasher.Verify(password, wrongSalt, hash);

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void Verify_IncorrectHash_ShouldReturnFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPassword(password, salt);
            var wrongHash = PasswordHasher.HashPassword("WrongPassword", salt);

            // Act
            var isValid = PasswordHasher.Verify(password, salt, wrongHash);

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        [TestCase(1000)]
        [TestCase(50000)]
        [TestCase(200000)]
        public void Verify_CustomIterations_ShouldWork(int iterations)
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPassword(password, salt, iterations);

            // Act
            var isValid = PasswordHasher.Verify(password, salt, hash, iterations);

            // Assert
            Assert.That(isValid, Is.True);
        }

        [Test]
        public void Verify_MismatchedIterations_ShouldReturnFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var salt = PasswordHasher.GenerateSalt();
            var hash = PasswordHasher.HashPassword(password, salt, 100000);

            // Act
            var isValid = PasswordHasher.Verify(password, salt, hash, 50000);

            // Assert
            Assert.That(isValid, Is.False);
        }

        [Test]
        public void HashPassword_EmptyPassword_ShouldNotThrow()
        {
            // Arrange
            var password = string.Empty;
            var salt = PasswordHasher.GenerateSalt();

            // Act & Assert
            var hash = PasswordHasher.HashPassword(password, salt);
            Assert.That(hash, Is.Not.Null.And.Not.Empty);
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
    }
} 