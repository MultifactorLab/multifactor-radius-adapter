
using System.Text.RegularExpressions;
using Multifactor.Radius.Adapter.v2.Domain.Auth;

namespace Multifactor.Radius.Adapter.v2.Domain;

public class UserPassphrase
{
    private static readonly string[] ProviderCodes = ["t", "m", "s", "c"];

    public string? Raw { get; }
    public string? Password { get; }
    public string? Otp { get; }
    public string? ProviderCode { get; }
    public bool IsEmpty => Password == null && Otp == null && ProviderCode == null;

    private UserPassphrase(string? raw, string? password, string? otp, string? providerCode)
    {
        Raw = raw;
        Password = password;
        Otp = otp;
        ProviderCode = providerCode;
    }

    public static UserPassphrase Parse(string? rawPwd, PreAuthModeDescriptor preAuthMode)
    {
        ArgumentNullException.ThrowIfNull(preAuthMode);

        var hasOtp = TryGetOtpCode(rawPwd, preAuthMode, out var otp);
        var password = GetPassword(rawPwd, preAuthMode, hasOtp);
        var providerCode = GetProviderCode(password);

        return new UserPassphrase(rawPwd, password, otp, providerCode);
    }

    private static string? GetPassword(string? rawPwd, PreAuthModeDescriptor preAuthMode, bool hasOtp)
    {
        var passwordAndOtp = rawPwd?.Trim() ?? string.Empty;

        return preAuthMode.Mode switch
        {
            PreAuthMode.Otp when hasOtp && passwordAndOtp.Length > preAuthMode.Settings.OtpCodeLength 
                => passwordAndOtp[..^preAuthMode.Settings.OtpCodeLength],
            _ => !string.IsNullOrEmpty(passwordAndOtp) ? passwordAndOtp : null
        };
    }

    private static bool TryGetOtpCode(string? rawPwd, PreAuthModeDescriptor preAuthMode, out string? code)
    {
        var passwordAndOtp = rawPwd?.Trim() ?? string.Empty;
        var length = preAuthMode.Settings.OtpCodeLength;

        if (passwordAndOtp.Length < length)
        {
            code = null;
            return false;
        }

        code = passwordAndOtp[^length..];
        
        if (!Regex.IsMatch(code, preAuthMode.Settings.OtpCodeRegex))
        {
            code = null;
            return false;
        }

        return true;
    }

    private static string? GetProviderCode(string? password)
    {
        return ProviderCodes.FirstOrDefault(x => x == password?.ToLower());
    }
}