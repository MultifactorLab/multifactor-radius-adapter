namespace Multifactor.Radius.Adapter.v2.Domain.Auth;

public class PreAuthModeSettings
{
    public int OtpCodeLength { get; }
    public string OtpCodeRegex { get; }

    private const int MinOtpLength = 1;
    private const int MaxOtpLength = 20;

    public PreAuthModeSettings(int otpCodeLength)
    {
        if (otpCodeLength < MinOtpLength || otpCodeLength > MaxOtpLength)
            throw new ArgumentOutOfRangeException(nameof(otpCodeLength), 
                $"Value must be between {MinOtpLength} and {MaxOtpLength}");

        OtpCodeLength = otpCodeLength;
        OtpCodeRegex = $"^[0-9]{{{otpCodeLength}}}$";
    }

    public static PreAuthModeSettings Default => new(6);
}