using System;

namespace MultiFactor.Radius.Adapter.Configuration.Features.PreAuthModeFeature;

public class PreAuthModeSettings
{
    public int OtpCodeLength { get; }
    public string OtpCodeRegex { get; }

    public PreAuthModeSettings(int otpCodeLength)
    {
        if (otpCodeLength < 1 || otpCodeLength > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(otpCodeLength), "Value should not be less than 1 and should not be more than 20");
        }
        OtpCodeLength = otpCodeLength;
        OtpCodeRegex = $"^[0-9]{{{otpCodeLength}}}$";
    }

    public static PreAuthModeSettings Default => new(6);
}
