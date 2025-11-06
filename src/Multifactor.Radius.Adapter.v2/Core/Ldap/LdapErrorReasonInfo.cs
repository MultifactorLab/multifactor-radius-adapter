using System.ComponentModel;
using System.Text.RegularExpressions;
using Multifactor.Core.Ldap.LangFeatures;

namespace Multifactor.Radius.Adapter.v2.Core.Ldap;

public class LdapErrorReasonInfo
{
    public LdapErrorFlag Flags { get; }
    public LdapErrorReason Reason { get; }
    public string ReasonText { get; }

    protected LdapErrorReasonInfo(LdapErrorReason reason, LdapErrorFlag flags, string reasonText)
    {
        Flags = flags;
        Reason = reason;
        ReasonText = reasonText;
    }

    public static LdapErrorReasonInfo Create(string serverErrorMessage)
    {
        Throw.IfNullOrWhiteSpace(serverErrorMessage, nameof(serverErrorMessage));

        var reason = GetErrorReason(serverErrorMessage);
        var flags = GetErrorFlags(reason);
        var text = GetReasonText(reason);

        return new LdapErrorReasonInfo(reason, flags, text);
    }

    private static LdapErrorReason GetErrorReason(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return LdapErrorReason.UnknownError;
        }

        var pattern = @"data ([0-9a-e]{3})";
        var match = Regex.Match(message, pattern);

        if (!match.Success || match.Groups.Count != 2)
        {
            return LdapErrorReason.UnknownError;
        }

        var data = match.Groups[1].Value;
        switch (data)
        {
            case "525": return LdapErrorReason.UserNotFound;
            case "52e": return LdapErrorReason.InvalidCredentials;
            case "530": return LdapErrorReason.NotPermittedToLogonAtThisTime;
            case "531": return LdapErrorReason.NotPermittedToLogonAtThisWorkstation;
            case "532": return LdapErrorReason.PasswordExpired;
            case "533": return LdapErrorReason.AccountDisabled;
            case "701": return LdapErrorReason.AccountExpired;
            case "773": return LdapErrorReason.UserMustChangePassword;
            case "775": return LdapErrorReason.UserAccountLocked;
            default: return LdapErrorReason.UnknownError;
        }
    }

    private static LdapErrorFlag GetErrorFlags(LdapErrorReason reason)
    {
        switch (reason)
        {
            case LdapErrorReason.PasswordExpired:
            case LdapErrorReason.UserMustChangePassword:
                return LdapErrorFlag.MustChangePassword;
            default:
                return LdapErrorFlag.None;
        }
    }

    private static string GetReasonText(LdapErrorReason reason)
    {
        // "SomeErrorText" -> ["some, "error", "text"]
        var splitted = Regex.Split(reason.ToString(), @"(?<!^)(?=[A-Z])").Select(x => x.ToLower());
        return string.Join(" ", splitted);
    }
}

public enum LdapErrorReason
{
    [Description("525")]
    UserNotFound,
    
    [Description("52e")]
    InvalidCredentials,

    [Description("530")]
    NotPermittedToLogonAtThisTime,

    [Description("531")]
    NotPermittedToLogonAtThisWorkstation,

    [Description("532")]
    PasswordExpired,

    [Description("533")]
    AccountDisabled,

    [Description("701")]
    AccountExpired,

    [Description("773")]
    UserMustChangePassword,

    [Description("775")]
    UserAccountLocked,

    UnknownError
}

public enum LdapErrorFlag
{
    None = 0,
    MustChangePassword = 1,
}