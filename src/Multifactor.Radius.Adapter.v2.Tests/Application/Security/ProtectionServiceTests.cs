using System.Security.Cryptography;
using System.Text;
using Multifactor.Radius.Adapter.v2.Application.Features.Security;

namespace Multifactor.Radius.Adapter.v2.Tests.Application.Security
{
    public class ProtectionServiceTests
    {
        private const string TestSecret = "test-secret-123";
        private const string TestData = "test-data-to-protect";

        [Fact]
        public void Protect_ShouldThrowArgumentException_WhenDataIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ProtectionService.Protect(TestSecret, null));
        }

        [Fact]
        public void Protect_ShouldThrowArgumentException_WhenDataIsEmpty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => ProtectionService.Protect(TestSecret, ""));
        }

        [Fact]
        public void Protect_ShouldThrowArgumentException_WhenDataIsWhitespace()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => ProtectionService.Protect(TestSecret, "   "));
        }

        [Fact]
        public void Unprotect_ShouldThrowArgumentException_WhenDataIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => ProtectionService.Unprotect(TestSecret, null));
        }

        [Fact]
        public void Unprotect_ShouldThrowArgumentException_WhenDataIsEmpty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => ProtectionService.Unprotect(TestSecret, ""));
        }

        [Fact]
        public void Unprotect_ShouldThrowArgumentException_WhenDataIsWhitespace()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => ProtectionService.Unprotect(TestSecret, "   "));
        }

        [Fact]
        public void ProtectAndUnprotect_ShouldReturnOriginalData_OnWindows()
        {
            // Only run this test on Windows where ProtectedData is actually used
            if (!OperatingSystem.IsWindows())
                return;

            // Arrange
            var originalData = "sensitive-password-123!@#";

            // Act
            var protectedData = ProtectionService.Protect(TestSecret, originalData);
            var unprotectResult = ProtectionService.Unprotect(TestSecret, protectedData);

            // Assert
            Assert.NotNull(protectedData);
            Assert.NotEmpty(protectedData);
            Assert.NotEqual(originalData, protectedData); // Protected data should be different
            Assert.Equal(originalData, unprotectResult); // Unprotect should return original
        }

        [Fact]
        public void Protect_ShouldReturnBase64String_OnNonWindows()
        {
            // Only run this test on non-Windows platforms
            if (OperatingSystem.IsWindows())
                return;

            // Arrange
            var originalData = "test-data";

            // Act
            var result = ProtectionService.Protect(TestSecret, originalData);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            
            // Should be valid base64
            var bytes = Convert.FromBase64String(result);
            var decoded = Encoding.UTF8.GetString(bytes);
            Assert.Equal(originalData, decoded);
        }

        [Fact]
        public void Unprotect_ShouldReturnOriginalData_OnNonWindows()
        {
            // Only run this test on non-Windows platforms
            if (OperatingSystem.IsWindows())
                return;

            // Arrange
            var originalData = "test-data";

            // Act
            var protectedData = ProtectionService.Protect(TestSecret, originalData);
            var result = ProtectionService.Unprotect(TestSecret, protectedData);

            // Assert
            Assert.Equal(originalData, result);
        }

        [Fact]
        public void ProtectAndUnprotect_ShouldHandleSpecialCharacters()
        {
            // Arrange
            var originalData = "Password123!@#$%^&*()\n\t\r\0";

            // Act
            var protectedData = ProtectionService.Protect(TestSecret, originalData);
            var result = ProtectionService.Unprotect(TestSecret, protectedData);

            // Assert
            Assert.Equal(originalData, result);
        }

        [Fact]
        public void ProtectAndUnprotect_ShouldHandleUnicodeCharacters()
        {
            // Arrange
            var originalData = "密码🔑пароль🎯";

            // Act
            var protectedData = ProtectionService.Protect(TestSecret, originalData);
            var result = ProtectionService.Unprotect(TestSecret, protectedData);

            // Assert
            Assert.Equal(originalData, result);
        }

        [Fact]
        public void ProtectAndUnprotect_ShouldHandleEmptySecret()
        {
            // Arrange
            var secret = "";
            var originalData = "test-data";

            // Act
            var protectedData = ProtectionService.Protect(secret, originalData);
            var result = ProtectionService.Unprotect(secret, protectedData);

            // Assert
            Assert.Equal(originalData, result);
        }

        [Fact]
        public void ProtectAndUnprotect_ShouldWorkWithDifferentSecrets()
        {
            // Only run this test on Windows where ProtectedData uses the secret
            if (!OperatingSystem.IsWindows())
                return;

            // Arrange
            var secret1 = "secret-one";
            var secret2 = "secret-two";
            var originalData = "test-data";

            // Act
            var protectedWithSecret1 = ProtectionService.Protect(secret1, originalData);
            
            // Assert - Should fail with wrong secret on Windows
            if (OperatingSystem.IsWindows())
            {
                Assert.Throws<CryptographicException>(() => 
                    ProtectionService.Unprotect(secret2, protectedWithSecret1));
            }
        }

        [Fact]
        public void Protect_ShouldReturnDifferentResultsForSameInput()
        {
            // Only run this test on Windows where ProtectedData adds entropy
            if (!OperatingSystem.IsWindows())
                return;

            // Arrange
            var data = "same-data";

            // Act
            var result1 = ProtectionService.Protect(TestSecret, data);
            var result2 = ProtectionService.Protect(TestSecret, data);

            // Assert
            Assert.NotEqual(result1, result2); // Should be different due to entropy
        }
    }
}