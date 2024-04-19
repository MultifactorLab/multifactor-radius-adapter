using MultiFactor.Radius.Adapter.Configuration.Features.PreAuthModeFeature;
using MultiFactor.Radius.Adapter.Framework.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Tests
{
    public class UserPassphraseTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Qwerty")]
        public void Parse_ShoulNotBeNull(string src)
        {
            var descr = PreAuthModeDescriptor.Create(string.Empty, new PreAuthModeSettings(6));
            var pass = UserPassphrase.Parse(src, descr);

            Assert.NotNull(pass);
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Qwerty")]
        [InlineData("Qwerty123456")]
        [InlineData("Йцукен789123")]
        public void Parse_OtpMode_RawShoulBeEqualToSource(string src)
        {
            var descr = PreAuthModeDescriptor.Create("otp", new PreAuthModeSettings(6));
            var pass = UserPassphrase.Parse(src, descr);

            Assert.Equal(src, pass.Raw);
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Qwerty")]
        [InlineData("Qwerty123456")]
        [InlineData("Йцукен789123")]
        public void Parse_DefaultMode_RawShoulBeEqualToSource(string src)
        {
            var descr = PreAuthModeDescriptor.Create(string.Empty, new PreAuthModeSettings(6));
            var pass = UserPassphrase.Parse(src, descr);

            Assert.Equal(src, pass.Raw);
        }
        
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Qwerty")]
        [InlineData("Qwerty12345")]
        public void Parse_OtpModeAndEmptyOrShortOtp_OtpShouldBeNull(string src)
        {
            var descr = PreAuthModeDescriptor.Create("otp", new PreAuthModeSettings(6));
            var pass = UserPassphrase.Parse(src, descr);

            Assert.Null(pass.Otp);
        }
        
        [Theory]
        [InlineData("Qwerty123456")]
        [InlineData("123456")]
        public void Parse_OtpMode_OtpShouldBeCorrect(string src)
        {
            var descr = PreAuthModeDescriptor.Create("otp", new PreAuthModeSettings(6));
            var pass = UserPassphrase.Parse(src, descr);

            Assert.Equal("123456", pass.Otp);
        }
        
        [Theory]
        [InlineData("123456789")]
        [InlineData("Qwerty123456789")]
        public void Parse_OtpMode_OtpShouldBeEqualToLast5Digits(string src)
        {
            var descr = PreAuthModeDescriptor.Create("otp", new PreAuthModeSettings(5));
            var pass = UserPassphrase.Parse(src, descr);

            Assert.Equal("56789", pass.Otp);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("123456")]
        public void Parse_OtpModeAndEmptyPass_PwdShoulBeNull(string src)
        {
            var descr = PreAuthModeDescriptor.Create("otp", new PreAuthModeSettings(6));
            var pass = UserPassphrase.Parse(src, descr);

            Assert.Null(pass.Password);
        }
    }
}
