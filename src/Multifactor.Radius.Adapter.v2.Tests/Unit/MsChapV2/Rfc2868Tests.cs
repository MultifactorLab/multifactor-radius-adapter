using System.Security.Cryptography;
using System.Text;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor.MsChapV2;

namespace Multifactor.Radius.Adapter.v2.Tests.Unit.MsChapV2;

public class Rfc2868Tests
{
    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("Hello")]
    [InlineData("0123456789abcde")]
    [InlineData("0123456789abcdef")]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef")]
    [InlineData("Qwerty123!")]
    public void TestTunnelPassword(string password)
    {
        //var salt = new byte[] { 0x83, 0x45 };
        var salt = Rfc2868.GenerateSalt();
        
        var secret = Encoding.ASCII.GetBytes("secret");
        var requestAuthenticator = new byte[16];
        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetNonZeroBytes(requestAuthenticator);
        
        var pwd = Encoding.ASCII.GetBytes(password);
        var newTunnelPassword = Rfc2868.NewTunnelPassword(pwd, salt, secret, requestAuthenticator);
        var decryptedPassword = Rfc2868.TunnelPassword(newTunnelPassword, secret, requestAuthenticator, out var decryptedSalt);
        Assert.True(pwd.SequenceEqual(decryptedPassword));
    }
}