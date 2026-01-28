using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Dictionary;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Dictionary.Attributes;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Reader;
using Multifactor.Radius.Adapter.v2.Shared.Extensions;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser;

public class XmlConfigurationParser : IConfigurationParser
{
    private readonly IRadiusDictionary _dictionary;
    const string _commonPrefix = "RAD_";
    
    public XmlConfigurationParser(
        IRadiusDictionary dictionary)
    {
        _dictionary = dictionary;
    }
    
    public async Task<RootConfiguration> ParseRootConfigAsync(string filePath, CancellationToken ct)
    {
        var xml = await XmlReader.ReadAsync(filePath, ct);
        var settingsXml = XmlReader.ExtractAppSettings(xml);

        var settingsEnv = EnvironmentReader.ReadEnvironments("RAD_APPSETTINGS");
        
        return new RootConfiguration
        {
            MultifactorApiUrls = ValueParser.ParseUrls(GetRawValue("multifactor-api-url", settingsEnv, settingsXml, true), required: true),
            MultifactorApiProxy = GetRawValue("multifactor-api-proxy", settingsEnv, settingsXml),
            MultifactorApiTimeout = ValueParser.ParseTimeout(GetRawValue("multifactor-api-timeout", settingsEnv, settingsXml), 
                TimeSpan.FromSeconds(65)),
            AdapterServerEndpoint = ValueParser.ParseEndpoint(GetRawValue("adapter-server-endpoint", settingsEnv, settingsXml, true), required: true),
            LoggingFormat = GetRawValue("logging-format", settingsEnv, settingsXml),
            SyslogUseTls = ValueParser.ParseBool(GetRawValue("syslog-use-tls", settingsEnv, settingsXml), false),
            SyslogServer = GetRawValue("syslog-server", settingsEnv, settingsXml),
            SyslogFormat = GetRawValue("syslog-format", settingsEnv, settingsXml), 
            SyslogFacility = GetRawValue("syslog-facility", settingsEnv, settingsXml),
            SyslogAppName = GetRawValue("syslog-app-name", settingsEnv, settingsXml) ?? "multifactor-radius",
            SyslogFramer = GetRawValue("syslog-framer", settingsEnv, settingsXml),
            SyslogOutputTemplate = GetRawValue("syslog-output-template", settingsEnv, settingsXml),
            ConsoleLogOutputTemplate = GetRawValue("console-log-output-template", settingsEnv, settingsXml),
            FileLogOutputTemplate = GetRawValue("file-log-output-template", settingsEnv, settingsXml),
            LogFileMaxSizeBytes = ValueParser.ParseInt(GetRawValue("log-file-max-size-bytes", settingsEnv, settingsXml), 1073741824),
            LoggingLevel = GetRawValue("logging-level", settingsEnv, settingsXml)
        };
    }
    
    public async Task<ClientConfiguration> ParseClientConfigAsync(string filePath, CancellationToken ct)
    {
        var xml = await XmlReader.ReadAsync(filePath, ct);
        var settingsXml = XmlReader.ExtractAppSettings(xml);

        var prefix = TransformName(filePath);
        var settingsEnv = EnvironmentReader.ReadEnvironments($"{_commonPrefix}{prefix}APPSETTINGS");
        
        var dto = new ClientConfiguration
        {
            Name = Path.GetFileNameWithoutExtension(filePath),
            MultifactorNasIdentifier = GetRawValue("multifactor-nas-identifier", settingsEnv, settingsXml, true),
            MultifactorSharedSecret = GetRawValue("multifactor-shared-secret", settingsEnv, settingsXml, true),
            SignUpGroups = ValueParser.ParseStringList(GetRawValue("sign-up-group", settingsEnv, settingsXml)),
            BypassSecondFactorWhenApiUnreachable = ValueParser.ParseBool(GetRawValue("bypass-second-factor-when-api-unreachable", settingsEnv, settingsXml), true),
            FirstFactorAuthenticationSource = ValueParser.ParseEnum<AuthenticationSource>(GetRawValue("first-factor-authentication-source", settingsEnv, settingsXml, true), required: true),
            AdapterClientEndpoint = ValueParser.ParseEndpoint(GetRawValue("adapter-client-endpoint", settingsEnv, settingsXml, true), required: true),
            RadiusClientIp = ValueParser.ParseIpAddress(GetRawValue("radius-client-ip", settingsEnv, settingsXml)),
            RadiusClientNasIdentifier = GetRawValue("radius-client-nas-identifier", settingsEnv, settingsXml),
            RadiusSharedSecret = GetRawValue("radius-shared-secret", settingsEnv, settingsXml, true),
            NpsServerEndpoints = ValueParser.ParseEndpoints(GetRawValue("nps-server-endpoint", settingsEnv, settingsXml, true), required: true),
            NpsServerTimeout = ValueParser.ParseTimeout(GetRawValue("nps-server-timeout", settingsEnv, settingsXml), TimeSpan.Parse("00:00:05")),
            Privacy = ValueParser.ParsePrivacyModeWithFields(GetRawValue("privacy-mode", settingsEnv, settingsXml)),
            PreAuthenticationMethod = ValueParser.ParseEnum<PreAuthMode>(GetRawValue("pre-authentication-method", settingsEnv, settingsXml), PreAuthMode.None),
            AuthenticationCacheLifetime = ValueParser.ParseTimeSpan(GetRawValue("authentication-cache-lifetime", settingsEnv, settingsXml)),
            CallingStationIdAttribute = GetRawValue("calling-station-id-attribute", settingsEnv, settingsXml),
            IpWhiteList = ValueParser.ParseIpRanges(GetRawValue("ip-white-list", settingsEnv, settingsXml)),
            InvalidCredentialDelay = ValueParser.ParseDelaySettings(GetRawValue("invalid-credential-delay", settingsEnv, settingsXml)),
            ReplyAttributes = ParseReplyAttributes(xml)
        };
        
        dto.LdapServers = ParseLdapServers(xml, dto.Name);
        
        return dto;
    }
    
    private List<LdapServerConfiguration> ParseLdapServers(XDocument xml, string configName)
    {
        var servers = new List<LdapServerConfiguration>();
        var ldapElements = XmlReader.GetLdapServerElements(xml);
        
        if (ldapElements == null || ldapElements.Count == 0)
            return servers;

        servers.AddRange(ldapElements.Select(element => new LdapServerConfiguration
        {
            ConnectionString = element.Attribute("connection-string")?.Value ?? throw new InvalidConfigurationException("LDAP username is required"),
            Username = element.Attribute("username")?.Value ?? throw new InvalidConfigurationException("LDAP username is required"),
            Password = element.Attribute("password")?.Value ?? throw new InvalidConfigurationException("LDAP password is required"),
            BindTimeoutSeconds = ValueParser.ParseInt(element.Attribute("bind-timeout-in-seconds")?.Value, 30),
            AccessGroups = ValueParser.ParseDistinguishedNames(element.Attribute("access-groups")?.Value),
            SecondFaGroups = ValueParser.ParseDistinguishedNames(element.Attribute("second-fa-groups")?.Value),
            SecondFaBypassGroups = ValueParser.ParseDistinguishedNames(element.Attribute("second-fa-bypass-groups")?.Value),
            LoadNestedGroups = ValueParser.ParseBool(element.Attribute("load-nested-groups")?.Value, true),
            NestedGroupsBaseDns = ValueParser.ParseDistinguishedNames(element.Attribute("nested-groups-base-dn")?.Value),
            AuthenticationCacheGroups = ValueParser.ParseDistinguishedNames(element.Attribute("authentication-cache-groups")?.Value),
            PhoneAttributes = ValueParser.ParseStringList(element.Attribute("phone-attributes")?.Value),
            IdentityAttribute = element.Attribute("identity-attribute")?.Value ?? "sAMAccountName",
            RequiresUpn = ValueParser.ParseBool(element.Attribute("requires-upn")?.Value, false),
            TrustedDomainsEnabled = ValueParser.ParseBool(element.Attribute("enable-trusted-domains")?.Value, false), 
            AlternativeSuffixesEnabled = ValueParser.ParseBool(element.Attribute("enable-alternative-suffixes")?.Value, false), 
            IncludedDomains = ValueParser.ParseStringList(element.Attribute("included-domains")?.Value), 
            ExcludedDomains = ValueParser.ParseStringList(element.Attribute("excluded-domains")?.Value), 
            IncludedSuffixes = ValueParser.ParseStringList(element.Attribute("included-suffixes")?.Value), 
            ExcludedSuffixes = ValueParser.ParseStringList(element.Attribute("excluded-suffixes")?.Value),
            BypassSecondFactorWhenApiUnreachableGroups = ValueParser.ParseStringList(element.Attribute("bypass-second-factor-when-api-unreachable-groups")?.Value)
        }));

        return servers;
    }

    private IReadOnlyDictionary<string, RadiusReplyAttribute[]> ParseReplyAttributes(
        XDocument xml)
    {
        var elements = XmlReader.GetRadiusReplyElements(xml);
        if (!elements.Any())
            return new Dictionary<string, RadiusReplyAttribute[]>();
        
        var attributeGroups = elements
            .Where(e => e.Attribute("name") != null)
            .GroupBy(e => e.Attribute("name")!.Value);
        
        var result = new Dictionary<string, RadiusReplyAttribute[]>();
        
        foreach (var group in attributeGroups)
        {
            var attributeName = group.Key;
            var attributes = new List<RadiusReplyAttribute>();
            
            foreach (var element in group)
            {
                var fromAttr = element.Attribute("from")?.Value;
                var valueAttr = element.Attribute("value")?.Value;
                var whenAttr = element.Attribute("when")?.Value;
                var sufficientAttr = element.Attribute("sufficient")?.Value;
                
                var sufficient = bool.TryParse(sufficientAttr, out var suff) && suff;
                
                if (!string.IsNullOrEmpty(fromAttr))
                {
                    attributes.Add(new RadiusReplyAttribute
                    {
                        Name = fromAttr,
                        Sufficient = sufficient
                    });
                }
                else if (!string.IsNullOrEmpty(valueAttr))
                {
                    var value = ParseRadiusReplyValue(attributeName, valueAttr);
                    var clauses = whenAttr.Split(['='], StringSplitOptions.RemoveEmptyEntries);
                    var conditions = clauses[0] switch
                    {
                        "UserGroup" or "UserName" => clauses[1]
                            .Split([';'], StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim()).ToList(),
                        _ => throw new Exception($"Unknown condition '{clauses}'")
                    };
                    var attribute = new RadiusReplyAttribute
                    {
                        Value = value,
                        Sufficient = sufficient
                    };
                    if(clauses[0]=="UserGroup")
                        attribute.UserGroupCondition = conditions;
                    else attribute.UserNameCondition = conditions;
                    attributes.Add(attribute);
                }
            }
            
            result[attributeName] = attributes.ToArray();
        }
        
        return result;
    }

    private object ParseRadiusReplyValue(string attributeName, string value)
    {
        var attribute = _dictionary.GetAttribute(attributeName);
        if (string.IsNullOrEmpty(value))
        {
            throw new Exception("Value must be specified");
        }

        return attribute.Type switch
        {
            DictionaryAttribute.TypeString or DictionaryAttribute.TypeTaggedString => value,
            DictionaryAttribute.TypeInteger or DictionaryAttribute.TypeTaggedInteger => uint.Parse(value),
            DictionaryAttribute.TypeIpAddr => IPAddress.Parse(value),
            DictionaryAttribute.TypeOctet => value.ToByteArray(),
            _ => throw new Exception($"Unknown type {attribute.Type}")
        };
    }
    
    private static string TransformName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }
        name = Regex.Replace(name, @"\s+", string.Empty);
        return name;
    }

    private static string GetRawValue(string key, IReadOnlyDictionary<string, string> primary, IReadOnlyDictionary<string, string> secondary, bool required = false)
    {
        return primary.TryGetValue(key, out var primaryValue) ? primaryValue 
            : secondary.TryGetValue(key, out var secondaryValue) ?  secondaryValue 
            : !required ? string.Empty : throw new InvalidConfigurationException($"{key} is required") ;
    }
}