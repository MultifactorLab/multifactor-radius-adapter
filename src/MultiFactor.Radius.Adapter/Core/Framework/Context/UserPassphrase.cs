﻿//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Features.PreAuthModeFeature;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace MultiFactor.Radius.Adapter.Core.Framework.Context
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
        public string Otp { get; }

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

        public static UserPassphrase Parse(string rawPwd, PreAuthModeDescriptor preAuthnMode)
        {
            if (preAuthnMode is null)
            {
                throw new ArgumentNullException(nameof(preAuthnMode));
            }

            var hasOtp = TryGetOtpCode(rawPwd, preAuthnMode, out var otp);
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

        private static string GetPassword(string rawPwd, PreAuthModeDescriptor preAuthnMode, bool hasOtp)
        {
            var passwordAndOtp = rawPwd?.Trim() ?? string.Empty;
            switch (preAuthnMode.Mode)
            {
                case PreAuthMode.Otp:
                    var length = preAuthnMode.Settings.OtpCodeLength;
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

        private static bool TryGetOtpCode(string rawPwd, PreAuthModeDescriptor preAuthnMode, out string code)
        {
            var passwordAndOtp = rawPwd?.Trim() ?? string.Empty;
            var length = preAuthnMode.Settings.OtpCodeLength;
            if (passwordAndOtp.Length < length)
            {
                code = null;
                return false;
            }

            code = passwordAndOtp[^length..];
            if (!Regex.IsMatch(code, preAuthnMode.Settings.OtpCodeRegex))
            {
                code = null;
                return false;
            }

            return true;
        }
    }
}
