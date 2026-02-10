//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System.Text.RegularExpressions;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models
{
    public class UserPassphrase
    {
        private static readonly string[] _providerCodes = { "t", "m", "s", "c" };

        /// <summary>
        /// User-Password attribute raw value.
        /// </summary>
        public string Raw { get; }

        /// <summary>
        /// User password.
        /// </summary>
        public string Password { get; }

        /// <summary>
        /// 6 digits.
        /// </summary>
        public string? Otp { get; }

        /// <summary>
        /// Maybe one of 't', 'm', 's' or 'c'.<br/> 
        /// t: Telegram<br/> 
        /// m: MobileApp<br/> 
        /// s: SMS<br/> 
        /// c: PhoneCall<br/> 
        /// Can be passed to the User-Password attribute in case of None first-factor-authentication-source or if challenge is executed.
        /// </summary>
        public string ProviderCode { get; }

        /// <summary>
        /// User-Password packet attribute is empty.
        /// </summary>
        public bool IsEmpty => Password == null && Otp == null && ProviderCode == null;

        private UserPassphrase(string raw, string password, string otp, string providerCode)
        {
            Raw = raw;
            Password = password;
            Otp = otp;
            ProviderCode = providerCode;
        }

        public static UserPassphrase Parse(string rawPwd, PreAuthMode? preAuthnMode)
        {
            var hasOtp = TryGetOtpCode(rawPwd, out var otp);
            if (!hasOtp)
            {
                otp = null;
            }

            var pwd = GetPassword(rawPwd, preAuthnMode, hasOtp);
            if (string.IsNullOrEmpty(pwd))
            {
                pwd = null;
            }

            var provCode = _providerCodes.FirstOrDefault(x => x == pwd?.ToLower());
            return new UserPassphrase(rawPwd, pwd, otp, provCode);
        }

        private static string GetPassword(string rawPwd, PreAuthMode? preAuthnMode, bool hasOtp)
        {
            var passwordAndOtp = rawPwd?.Trim() ?? string.Empty;
            switch (preAuthnMode)
            {
                case PreAuthMode.Otp:
                    var length = 6;
                    if (passwordAndOtp.Length < length)
                    {
                        return passwordAndOtp;
                    }

                    if (!hasOtp)
                    {
                        return passwordAndOtp;
                    }

                    var sub = passwordAndOtp[..^length];
                    return sub;

                case PreAuthMode.None:
                default:
                    return passwordAndOtp;
            }
        }

        private static bool TryGetOtpCode(string rawPwd, out string code)
        {
            var passwordAndOtp = rawPwd?.Trim() ?? string.Empty;
            var length = 10;
            if (passwordAndOtp.Length < length)
            {
                code = null;
                return false;
            }

            code = passwordAndOtp[^length..];
            var otpCodeRegex = $"^[0-9]{{{length}}}$";
            if (!Regex.IsMatch(code, otpCodeRegex))
            {
                code = null;
                return false;
            }

            return true;
        }
    }
}
