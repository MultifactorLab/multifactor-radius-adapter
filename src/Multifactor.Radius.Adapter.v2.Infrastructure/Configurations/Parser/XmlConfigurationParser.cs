using System.Net;
using System.Xml.Linq;
using Multifactor.Radius.Adapter.v2.Application.Configuration.Models;
using Multifactor.Radius.Adapter.v2.Application.Models.Enum;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Dictionary;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Dictionary.Attributes;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser.ValueParser;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Reader;
using Multifactor.Radius.Adapter.v2.Shared;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser;

public class XmlConfigurationParser : IConfigurationParser
{
    private readonly IXmlReader _xmlReader;
    private readonly IValueParser _valueParser;
    private readonly IRadiusDictionary _dictionary;
    
    public XmlConfigurationParser(
        IXmlReader xmlReader,
        IValueParser valueParser,
        IRadiusDictionary dictionary)
    {
        _xmlReader = xmlReader;
        _valueParser = valueParser;
        _dictionary = dictionary;
    }
    
    public async Task<RootConfiguration> ParseRootConfigAsync(string filePath, CancellationToken ct)
    {
        var xml = await _xmlReader.ReadAsync(filePath, ct);
        var settings = _xmlReader.ExtractAppSettings(xml);
        
        return new RootConfiguration()
        {
            AdapterServerEndpoint = _valueParser.ParseEndpoint(settings.GetValueOrDefault("adapter-server-endpoint"), required: true),
            MultifactorApiUrls = _valueParser.ParseUrls(settings.GetValueOrDefault("multifactor-api-url"), required: true),
            MultifactorApiTimeout = _valueParser.ParseTimeout(settings.GetValueOrDefault("multifactor-api-timeout"), 
                TimeSpan.FromSeconds(65)),
            MultifactorApiProxy = settings.GetValueOrDefault("multifactor-api-proxy"),
            LoggingFormat = settings.GetValueOrDefault("logging-format") ?? string.Empty,
            SyslogUseTls = _valueParser.ParseBool("syslog-use-tls", false),
            SyslogServer = settings.GetValueOrDefault("syslog-server") ?? string.Empty,
            SyslogFormat = settings.GetValueOrDefault("syslog-format") ?? string.Empty,
            SyslogFacility = settings.GetValueOrDefault("syslog-facility") ?? string.Empty,
            SyslogAppName = settings.GetValueOrDefault("syslog-app-name") ?? "multifactor-radius",
            SyslogFramer = settings.GetValueOrDefault("syslog-framer") ?? string.Empty,
            SyslogOutputTemplate = settings.GetValueOrDefault("syslog-output-template") ?? string.Empty,
            ConsoleLogOutputTemplate = settings.GetValueOrDefault("console-log-output-template") ?? string.Empty,
            FileLogOutputTemplate = settings.GetValueOrDefault("file-log-output-template") ?? string.Empty,
            LogFileMaxSizeBytes = _valueParser.ParseInt("log-file-max-size-bytes", 1073741824),
        };
    }
    
    public async Task<ClientConfiguration> ParseClientConfigAsync(string filePath, CancellationToken ct)
    {
        var xml = await _xmlReader.ReadAsync(filePath, ct);
        var settings = _xmlReader.ExtractAppSettings(xml);
        
        var dto = new ClientConfiguration
        {
            Name = Path.GetFileNameWithoutExtension(filePath),
            MultifactorNasIdentifier = settings.GetValueOrDefault("multifactor-nas-identifier")
                                       ?? throw new InvalidConfigurationException("multifactor-nas-identifier is required"),
            MultifactorSharedSecret = settings.GetValueOrDefault("multifactor-shared-secret")
                                      ?? throw new InvalidConfigurationException("multifactor-shared-secret is required"),
            SignUpGroups = _valueParser.ParseStringList(settings.GetValueOrDefault("sign-up-group")),
            BypassSecondFactorWhenApiUnreachable = _valueParser.ParseBool("bypass-second-factor-when-api-unreachable", true),
            FirstFactorAuthenticationSource = _valueParser.ParseEnum<AuthenticationSource>("first-factor-authentication-source", required: true),
            AdapterClientEndpoint = _valueParser.ParseEndpoint(settings.GetValueOrDefault("adapter-client-endpoint"), required: true),
            RadiusClientIp = _valueParser.ParseIpAddress(settings.GetValueOrDefault("radius-client-ip")),
            RadiusClientNasIdentifier = settings.GetValueOrDefault("radius-client-nas-identifier") ?? string.Empty,
            RadiusSharedSecret = settings.GetValueOrDefault("radius-shared-secret")
                ?? throw new InvalidConfigurationException("radius-shared-secret is required"),
            NpsServerEndpoints = _valueParser.ParseEndpoints(settings.GetValueOrDefault("nps-server-endpoint"), required: true),
            NpsServerTimeout = _valueParser.ParseTimeout("nps-server-timeout", TimeSpan.Parse("00:00:05")),
            PreAuthenticationMethod = _valueParser.ParseEnum<PreAuthMode>("pre-authentication-method", PreAuthMode.None),
            AuthenticationCacheLifetime = _valueParser.ParseTimeSpan(
                settings.GetValueOrDefault("authentication-cache-lifetime")),
            CallingStationIdAttribute = settings.GetValueOrDefault("calling-station-id-attribute"),
            IpWhiteList = _valueParser.ParseIpRanges(
                settings.GetValueOrDefault("ip-white-list")),
            LoggingLevel = settings.GetValueOrDefault("logging-level"),
            InvalidCredentialDelay = _valueParser.ParseDelaySettings(settings.GetValueOrDefault("invalid-credential-delay")),
        };

        var (mode, fields) = _valueParser.ParsePrivacyModeWithFields(settings.GetValueOrDefault("privacy-mode"));
        dto.PrivacyMode = mode;
        dto.PrivacyFields = fields;
        
        dto.LdapServers = ParseLdapServers(xml, dto.Name);
        
        dto.ReplyAttributes = ParseReplyAttributes(xml);
        
        return dto;
    }
    
    private List<LdapServerConfiguration> ParseLdapServers(XDocument xml, string configName)
    {
        var servers = new List<LdapServerConfiguration>();
        var ldapElements = xml.Root?.Element("ldapServers")?.Elements("ldapServer");
        
        if (ldapElements == null)
            return servers;

        servers.AddRange(ldapElements.Select(element => new LdapServerConfiguration
        {
            ConnectionString = element.Attribute("connection-string")?.Value ?? throw new InvalidConfigurationException("LDAP username is required"),
            Username = element.Attribute("username")?.Value ?? throw new InvalidConfigurationException("LDAP username is required"),
            Password = element.Attribute("password")?.Value ?? throw new InvalidConfigurationException("LDAP password is required"),
            BindTimeoutSeconds = _valueParser.ParseInt(element.Attribute("bind-timeout-in-seconds")?.Value, 30),
            AccessGroups = _valueParser.ParseDistinguishedNames(element.Attribute("access-groups")?.Value),
            SecondFaGroups = _valueParser.ParseDistinguishedNames(element.Attribute("second-fa-groups")?.Value),
            SecondFaBypassGroups = _valueParser.ParseDistinguishedNames(element.Attribute("second-fa-bypass-groups")?.Value),
            LoadNestedGroups = _valueParser.ParseBool(element.Attribute("load-nested-groups")?.Value, true),
            NestedGroupsBaseDns = _valueParser.ParseDistinguishedNames(element.Attribute("nested-groups-base-dn")?.Value),
            AuthenticationCacheGroups = _valueParser.ParseDistinguishedNames(element.Attribute("authentication-cache-groups")?.Value),
            PhoneAttributes = _valueParser.ParseStringList(element.Attribute("phone-attributes")?.Value),
            IdentityAttribute = element.Attribute("identity-attribute")?.Value ?? "sAMAccountName",
            RequiresUpn = _valueParser.ParseBool(element.Attribute("requires-upn")?.Value, false),
            TrustedDomainsEnabled = _valueParser.ParseBool(element.Attribute("enable-trusted-domains")?.Value, false), 
            AlternativeSuffixesEnabled = _valueParser.ParseBool(element.Attribute("enable-alternative-suffixes")?.Value, false), 
            IncludedDomains = _valueParser.ParseStringList(element.Attribute("included-domains")?.Value), 
            ExcludedDomains = _valueParser.ParseStringList(element.Attribute("excluded-domains")?.Value), 
            IncludedSuffixes = _valueParser.ParseStringList(element.Attribute("included-suffixes")?.Value), 
            ExcludedSuffixes = _valueParser.ParseStringList(element.Attribute("excluded-suffixes")?.Value)
        }));

        return servers;
    }

    private IReadOnlyDictionary<string, RadiusReplyAttribute[]> ParseReplyAttributes(
        XDocument xml)
    {
        var elements = _xmlReader.GetRadiusReplyElements(xml);
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
}