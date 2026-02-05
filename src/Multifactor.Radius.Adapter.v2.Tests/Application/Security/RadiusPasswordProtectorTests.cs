using System.Text;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Security;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Security
{
    public class RadiusPasswordProtectorTests
    {
        private readonly SharedSecret _sharedSecret;
        private readonly RadiusAuthenticator _authenticator;

        public RadiusPasswordProtectorTests()
        {
            _sharedSecret = new SharedSecret("test-secret-123");
            _authenticator = new RadiusAuthenticator(new byte[16] { 
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 
            });
        }

        [Fact]
        public void EncryptAndDecrypt_ShouldReturnOriginalPassword_ForShortPassword()
        {
            // Arrange
            var password = "test123";
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            // Act
            var encrypted = RadiusPasswordProtector.Encrypt(_sharedSecret, _authenticator, passwordBytes);
            var decrypted = RadiusPasswordProtector.Decrypt(_sharedSecret, _authenticator, encrypted);

            // Assert
            Assert.Equal(password, decrypted);
        }

        [Fact]
        public void EncryptAndDecrypt_ShouldReturnOriginalPassword_ForPasswordExactly16Chars()
        {
            // Arrange
            var password = "1234567890123456"; // Exactly 16 chars
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            // Act
            var encrypted = RadiusPasswordProtector.Encrypt(_sharedSecret, _authenticator, passwordBytes);
            var decrypted = RadiusPasswordProtector.Decrypt(_sharedSecret, _authenticator, encrypted);

            // Assert
            Assert.Equal(password, decrypted);
        }

        [Fact]
        public void EncryptAndDecrypt_ShouldReturnOriginalPassword_ForPasswordLongerThan16Chars()
        {
            // Arrange
            var password = "ThisIsAVeryLongPasswordThatExceeds16Characters";
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            // Act
            var encrypted = RadiusPasswordProtector.Encrypt(_sharedSecret, _authenticator, passwordBytes);
            var decrypted = RadiusPasswordProtector.Decrypt(_sharedSecret, _authenticator, encrypted);

            // Assert
            Assert.Equal(password, decrypted);
        }

        [Fact]
        public void EncryptAndDecrypt_ShouldReturnOriginalPassword_ForPasswordWithSpecialCharacters()
        {
            // Arrange
            var password = "P@ssw0rd!123#$\t\n\r";
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            // Act
            var encrypted = RadiusPasswordProtector.Encrypt(_sharedSecret, _authenticator, passwordBytes);
            var decrypted = RadiusPasswordProtector.Decrypt(_sharedSecret, _authenticator, encrypted);

            // Assert
            Assert.Equal(password, decrypted);
        }

        [Fact]
        public void EncryptAndDecrypt_ShouldReturnOriginalPassword_ForUnicodePassword()
        {
            // Arrange
            var password = "密码🔑пароль🎯";
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            // Act
            var encrypted = RadiusPasswordProtector.Encrypt(_sharedSecret, _authenticator, passwordBytes);
            var decrypted = RadiusPasswordProtector.Decrypt(_sharedSecret, _authenticator, encrypted);

            // Assert
            Assert.Equal(password, decrypted);
        }

        [Fact]
        public void EncryptAndDecrypt_ShouldHandleEmptyPassword()
        {
            // Arrange
            var password = "";
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            // Act
            var encrypted = RadiusPasswordProtector.Encrypt(_sharedSecret, _authenticator, passwordBytes);
            var decrypted = RadiusPasswordProtector.Decrypt(_sharedSecret, _authenticator, encrypted);

            // Assert
            Assert.Equal(password, decrypted);
        }

        [Fact]
        public void Decrypt_ShouldRemoveNullCharacters()
        {
            // Arrange
            var password = "test";
            var passwordWithNulls = password + "\0\0\0\0\0";
            var passwordBytes = Encoding.UTF8.GetBytes(passwordWithNulls);

            // Act
            var encrypted = RadiusPasswordProtector.Encrypt(_sharedSecret, _authenticator, passwordBytes);
            var decrypted = RadiusPasswordProtector.Decrypt(_sharedSecret, _authenticator, encrypted);

            // Assert
            Assert.Equal(password, decrypted); // Nulls should be removed
            Assert.DoesNotContain(decrypted, "\0");
        }

        [Fact]
        public void Encrypt_ShouldPadTo16ByteBoundaries()
        {
            // Arrange
            var password = "short"; // 5 bytes
            var passwordBytes = Encoding.UTF8.GetBytes(password);

            // Act
            var encrypted = RadiusPasswordProtector.Encrypt(_sharedSecret, _authenticator, passwordBytes);

            // Assert
            Assert.True(encrypted.Length % 16 == 0); // Should be multiple of 16
            Assert.Equal(16, encrypted.Length); // 5 padded to 16
        }

        [Fact]
        public void Encrypt_ShouldProduceDifferentOutput_ForDifferentAuthenticators()
        {
            // Arrange
            var password = "same-password";
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            
            var auth1 = new RadiusAuthenticator(new byte[16]);
            var auth2 = new RadiusAuthenticator(new byte[16] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 });

            // Act
            var encrypted1 = RadiusPasswordProtector.Encrypt(_sharedSecret, auth1, passwordBytes);
            var encrypted2 = RadiusPasswordProtector.Encrypt(_sharedSecret, auth2, passwordBytes);

            // Assert
            Assert.NotEqual(encrypted1, encrypted2);
        }

        [Fact]
        public void Encrypt_ShouldProduceDifferentOutput_ForDifferentSharedSecrets()
        {
            // Arrange
            var password = "password";
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            
            var secret1 = new SharedSecret("secret1");
            var secret2 = new SharedSecret("secret2");
            var auth = new RadiusAuthenticator(new byte[16]);

            // Act
            var encrypted1 = RadiusPasswordProtector.Encrypt(secret1, auth, passwordBytes);
            var encrypted2 = RadiusPasswordProtector.Encrypt(secret2, auth, passwordBytes);

            // Assert
            Assert.NotEqual(encrypted1, encrypted2);
        }
    }
}