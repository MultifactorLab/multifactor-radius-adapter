//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using FluentValidation;
namespace MultiFactor.Radius.Adapter.Configuration.Models;

public class AppSettingsSection
{
    public string MultifactorApiUrl { get; set; }
    public string MultifactorApiProxy { get; set; }
    public string MultifactorApiTimeout { get; set; }
    public string MultifactorNasIdentifier { get; set; }
    public string MultifactorSharedSecret { get; set; }
    public string SignUpGroups { get; set; }
    public bool BypassSecondFactorWhenApiUnreachable { get; set; }


    public AuthenticationSource FirstFactorAuthenticationSource { get; set; }


    public string ActiveDirectoryDomain { get; set; }
    public string ActiveDirectory2faBypassGroup { get; set; }
    public string ActiveDirectory2faGroup { get; set; }
    public string ActiveDirectoryGroup { get; set; }
    public string LdapBindDn { get; set; }
    public bool LoadActiveDirectoryNestedGroups { get; set; }
    public bool UseActiveDirectoryMobileUserPhone { get; set; }
    public bool UseActiveDirectoryUserPhone { get; set; }
    public bool UseUpnAsIdentity { get; set; }
    public string UseAttributeAsIdentity { get; set; }
    public string PhoneAttribute { get; set; }
    public string ServiceAccountPassword { get; set; }
    public string ServiceAccountUser { get; set; }


    public string AdapterClientEndpoint { get; set; }
    public string AdapterServerEndpoint { get; set; }
    public string NpsServerEndpoint { get; set; }
    public string RadiusClientIp { get; set; }
    public string RadiusClientNasIdentifier { get; set; }
    public string RadiusSharedSecret { get; set; }


    public string PrivacyMode { get; set; }
    public string PreAuthenticationMethod { get; set; }
    public string AuthenticationCacheLifetime { get; set; }
    public bool AuthenticationCacheMinimalMatching { get; set; }
    public string InvalidCredentialDelay { get; set; }


    public string LoggingFormat { get; set; }
    public string LoggingLevel { get; set; }
    public string CallingStationIdAttribute { get; set; }
    public string ConsoleLogOutputTemplate { get; set; }
    public string FileLogOutputTemplate { get; set; }
}

internal class AppSettingsSectionValidator : AbstractValidator<AppSettingsSection>
{
    public AppSettingsSectionValidator()
    {
        RuleFor(x => x.MultifactorApiUrl).NotNull();
        RuleFor(x => x.LoggingLevel).NotNull();
    }
}
