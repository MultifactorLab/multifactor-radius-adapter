using System;
using System.ComponentModel.DataAnnotations;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace MultiFactor.Radius.Adapter.Tests.IntegrationTests;

/// <summary>
/// LDAP connection string object.
/// </summary>
public sealed class LdapConnectionString
{
    /// <summary>
    /// LDAP or LDAPS
    /// </summary>
    public string Scheme { get; }

    /// <summary>
    /// Server host IP or name.
    /// </summary>
    public string Host { get; }

    /// <summary>
    /// Server port.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Container (base object search).
    /// </summary>
    public string Container { get; }

    public bool HasBaseDn => !string.IsNullOrWhiteSpace(Container);

    /// <summary>
    /// Well-formed LDAP url.
    /// For more information see <a href="http://www.rfc-editor.org/rfc/rfc2255">RFC2255</a>.
    /// </summary>
    public string WellFormedLdapUrl { get; }

    /// <summary>
    /// Tries to create a new LDAP connection string from any string.
    /// </summary>
    /// <param name="connectionString">String.</param>
    /// <exception cref="ArgumentException">If connectionString is null, empty or whitespaces.</exception>
    public LdapConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));
        }

        connectionString = connectionString.Trim();

        // parse URI-like string
        if (TryParseLdapUri(connectionString, out var uri))
        {
            // SCHEME
            if (uri.Scheme.Equals(LdapScheme.Ldap.Name, StringComparison.OrdinalIgnoreCase))
            {
                Scheme = LdapScheme.Ldap.Name;
            }
            else if (uri.Scheme.Equals(LdapScheme.Ldaps.Name, StringComparison.OrdinalIgnoreCase))
            {
                Scheme = LdapScheme.Ldaps.Name;
            }
            else
            {
                throw new ArgumentException("Unknown LDAP scheme");
            }

            // HOST
            Host = uri.DnsSafeHost;

            // PORT
            Port = Scheme == LdapScheme.Ldap.Name
                ? uri.Port
                : LdapScheme.Ldaps.Port;
            Scheme = AdjustScheme(Port);

            // CONTAINER
            Container = uri.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);

            // Build url
            WellFormedLdapUrl = BuildUrl(Host, Port, Container);

            return;
        }

        // Parse any other string...

        // SCHEME
        Scheme = LdapScheme.Ldap.Name;

        // HOST
        Host = GetHost(connectionString).ToString();

        // PORT
        Port = GetPort(connectionString);
        Scheme = AdjustScheme(Port);

        // CONTAINER
        var cont = GetContainer(connectionString);
        Container = cont.Length == 0
            ? string.Empty
            : cont.ToString();

        // Build url
        WellFormedLdapUrl = BuildUrl(Host, Port, Container);
    }

    private static string AdjustScheme(int port)
    {
        return port == LdapScheme.Ldaps.Port
            ? LdapScheme.Ldaps.Name
            : LdapScheme.Ldap.Name;
    }

    private static string GetHost(string str)
    {
        var idx = str.IndexOf(':');
        if (idx != -1)
        {
            return str.Substring(0, idx);
        }

        idx = str.IndexOf('/');
        if (idx != -1)
        {
            return str.Substring(0, idx);
        }

        return str;
    }

    private static int GetPort(string str)
    {
        var idx = str.IndexOf(':');
        if (idx == -1)
        {
            return LdapScheme.Ldap.Port;
        }

        var port = new StringBuilder();
        for (var i = idx + 1; i < str.Length && int.TryParse(str[i].ToString(), out var portDigit); i++)
        {
            port.Append(portDigit);
        }

        if (port.Length == 0)
        {
            throw new ArgumentException("Invalid Ldap url port definition");
        }

        return int.Parse(port.ToString());
    }

    private static bool TryParseLdapUri(string uriString, out Uri parsedUri)
    {
        if (!Uri.IsWellFormedUriString(uriString, UriKind.Absolute))
        {
            parsedUri = default;
            return false;
        }

        var uri = new Uri(uriString);
        if (!IsPossibleToFormLdapUrl(uri))
        {
            parsedUri = default;
            return false;
        }

        parsedUri = uri;
        return true;
    }

    private static bool IsPossibleToFormLdapUrl(Uri uri)
    {
        // 'System.Uri' class considers these to be well-formed URI 
        // with a 'domain.local' scheme, empty host and '-1' port:
        // 'domain.local:389'
        // 'domain.local:'
        // (-)_(-)
        return uri.Authority != string.Empty && uri.Host != string.Empty;
    }

    private static string GetContainer(string str)
    {
        var idx = str.IndexOf('/');
        if (idx != -1)
        {
            return str.Substring(idx + 1);
        }

        return string.Empty;
    }

    private static string BuildUrl(string host, int port, string container)
    {
        var containerPart = container != null
            ? $"/{container}"
            : string.Empty;
        return $"LDAP://{host}:{port}{containerPart}";
    }

    public class LdapScheme
    {
        /// <summary>
        /// Default scheme.
        /// </summary>
        public static LdapScheme Ldap { get; } = new LdapScheme("LDAP", 389);

        /// <summary>
        /// Scheme with secure connection (TLS/SSL).
        /// </summary>
        public static LdapScheme Ldaps { get; } = new LdapScheme("LDAPS", 636);

        /// <summary>
        /// Scheme name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Scheme port.
        /// </summary>
        public int Port { get; }

        private LdapScheme(string name, int port)
        {
            (Name, Port) = (name, port);
        }
    }
}

public class LdapOptions
{
    [Required]
    public string Path { get; set; }

    [Required]
    public string Username { get; set; }

    [Required]
    public string Password { get; set; }

    [Range(1, 5000)]
    public int PageSize { get; set; } = 500;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(20);
}

public sealed class LdapConnectionFactory
{
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _minTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan _maxTimeout = TimeSpan.FromMinutes(5);

    private readonly LdapConnectionString _connectionString;
    private readonly ILogger<LdapConnectionFactory> _logger;
    private readonly LdapOptions _options;

    public LdapConnectionFactory(LdapConnectionString connectionString,
        IOptions<LdapOptions> options,
        ILogger<LdapConnectionFactory> logger)
    {
        _connectionString = connectionString;
        _options = options.Value;
        _logger = logger;
    }

    internal LdapConnectionFactory(LdapConnectionString connectionString,
        IOptions<LdapOptions> options)
    {
        _connectionString = connectionString;
        _options = options.Value;
        _logger = null;
    }

    /// <summary>
    /// Establishes a LDAP connection and bind user.
    /// </summary>
    /// <returns>LdapConnection</returns>
    public LdapConnection CreateConnection()
    {
        _logger?.LogDebug("Establishing an LDAP connection...");

        var id = new LdapDirectoryIdentifier(_connectionString.Host, _connectionString.Port);
        var authType = AuthType.Basic;
        var connenction = new LdapConnection(id,
            new NetworkCredential(_options.Username, _options.Password),
            authType);

        connenction.SessionOptions.ProtocolVersion = 3;
        connenction.SessionOptions.VerifyServerCertificate = (connection, certificate) => true;
        connenction.Timeout = GetTimeout();

        connenction.Bind();

        _logger?.LogDebug("The LDAP connection to server '{LdapServer:l}' is established and the user '{Username:l}' is bound using the '{AuthType:l}' authentication type",
            _connectionString.WellFormedLdapUrl,
            _options.Username,
            authType.ToString());

        return connenction;
    }

    private TimeSpan GetTimeout()
    {
        if (_options.Timeout < _minTimeout)
        {
            return _defaultTimeout;
        }

        if (_options.Timeout > _maxTimeout) 
        {
            return _defaultTimeout;
        }

        return _options.Timeout;
    }
}

public class OpenLdapIntegrationTest
{
    [Fact]
    public async Task WithoutTls_ShouldBind()
    {
        var openLdap = BuildOpenLdapContainer("123pwd123!");
        await openLdap.StartAsync();

        var services = BuildServices(openLdap, 389);
        var factory = services.GetRequiredService<LdapConnectionFactory>();

        using (var _ = factory.CreateConnection())
        {
            Assert.True(true);
        }

        await openLdap.StopAsync();
    }    

    private static IContainer BuildOpenLdapContainer(string adminPwd)
    {
        var openLdap = new ContainerBuilder()
            
            .WithImage("osixia/openldap")
            
            .WithEnvironment("LDAP_ORGANISATION", "My Company")
            .WithEnvironment("LDAP_DOMAIN", "my-company.com")
            .WithEnvironment("LDAP_ADMIN_PASSWORD", adminPwd)
            
            .WithPortBinding(389, true)
            .WithPortBinding(636, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(389).UntilPortIsAvailable(636))
            
            .WithCleanUp(true)
            .Build();

        return openLdap;
    }

    private static ServiceProvider BuildServices(IContainer openLdap, ushort port)
    {
        return new ServiceCollection()
            .Configure<LdapOptions>(x =>
            {
                x.Path = $"ldap://{openLdap.Hostname}:{openLdap.GetMappedPublicPort(port)}";
                x.Username = "cn=admin,dc=my-company,dc=com";
                x.Password = "123pwd123!";
            })
            .AddSingleton(prov =>
            {
                var options = prov.GetRequiredService<IOptions<LdapOptions>>().Value;
                return new LdapConnectionString(options.Path);
            })
            .AddTransient<LdapConnectionFactory>()
            .AddTransient<BaseDnResolver>()
            .AddLogging()
            .BuildServiceProvider();
    }
}


internal sealed class BaseDnResolver
{
    const string _defaultNamingContextAttr = "defaultNamingContext";

    private readonly LdapConnectionFactory _connectionFactory;
    private readonly LdapConnectionString _connectionString;
    private readonly ILogger<BaseDnResolver> _logger;
    private readonly Lazy<string> _dn;

    public BaseDnResolver(LdapConnectionFactory connectionFactory,
        LdapConnectionString connectionString,
        ILogger<BaseDnResolver> logger)
    {
        _connectionFactory = connectionFactory;
        _connectionString = connectionString;
        _logger = logger;
        _dn = new Lazy<string>(() =>
        {
            using var conn = _connectionFactory.CreateConnection();
            var dn = GetBaseDnInternal(conn);
            return dn;
        });
    }

    /// <summary>
    /// Returns a Base DN from the LDAP connection string if presented. Otherwise, connects to a LDAP server and consumes Base DN from the RootDSE.
    /// </summary>
    /// <returns>BASE DN.</returns>
    public string GetBaseDn() => _dn.Value;

    private string GetBaseDnInternal(LdapConnection conn)
    {
        if (_connectionString.HasBaseDn)
        {
            _logger.LogDebug("Base DN was consumed from config: {BaseDN:l}", _connectionString.Container);
            return _connectionString.Container;
        }

        _logger.LogDebug("Try to consume Base DN from LDAP server");

        var filter = "(objectclass=*)";
        var searchRequest = new SearchRequest(null, filter, SearchScope.Base, "*");

        var response = conn.SendRequest(searchRequest);
        if (response is not SearchResponse searchResponse)
        {
            throw new Exception($"Invalid search response: {response}");
        }

        if (searchResponse.Entries.Count == 0)
        {
            throw new Exception($"Unable to consume {_defaultNamingContextAttr} from LDAP server: empty search result entrues");
        }

        var defaultNamingContext = searchResponse.Entries[0].GetFirstValueAttribute(_defaultNamingContextAttr);
        if (!defaultNamingContext.HasValues)
        {
            throw new Exception($"Unable to consume {_defaultNamingContextAttr} from LDAP server: '{_defaultNamingContextAttr}' attr was not found");
        }

        var value = defaultNamingContext.GetNotEmptyValues().FirstOrDefault();
        if (value is null)
        {
            throw new Exception($"Unable to consume {_defaultNamingContextAttr} from LDAP server: '{_defaultNamingContextAttr} attr value is empty'");
        }

        _logger.LogDebug("Base DN was consumed from LDAP server: {BaseDN:l}", value);

        return value;
    }
}

internal static class SearchResultEntryExtensions
{
    /// <summary>
    /// Returns a <see cref="LdapAttribute"/> with empty or single value.
    /// </summary>
    /// <param name="entry">Search Result entry</param>
    /// <param name="attr">Attribute name (type).</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">If <paramref name="entry"/> is null.</exception>
    /// <exception cref="ArgumentException">If <paramref name="attr"/> is empty.</exception>
    public static LdapAttribute GetFirstValueAttribute(this SearchResultEntry entry, string attr)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (string.IsNullOrWhiteSpace(attr))
        {
            throw new ArgumentException($"'{nameof(attr)}' cannot be null or whitespace.", nameof(attr));
        }

        var value = entry.Attributes[attr]?[0]?.ToString();
        return new LdapAttribute(attr, value);
    }
}

public class LdapAttribute : ValueObject
{
    /// <summary>
    /// Attribute name.
    /// </summary>
    public LdapAttributeName Name { get; }
    
    /// <summary>
    /// Attribute values.
    /// </summary>
    public string?[] Values { get; }

    public bool HasValues => Values.Length != 0;

    /// <summary>
    /// Creates LdapAttribute with the sinle value.
    /// </summary>
    /// <param name="name">Attribute name.</param>
    /// <param name="value">Attribute value.</param>
    public LdapAttribute(LdapAttributeName name, string? value) : this(name, [ value ]) { }
    
    /// <summary>
    /// Creates LdapAttribute with the specified values.
    /// </summary>
    /// <param name="name">Attribute name.</param>
    /// <param name="values">Attribute values.</param>
    public LdapAttribute(LdapAttributeName name, IEnumerable<string?> values)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(values);

        Name = name;
        Values = values.OrderByDescending(x => x).ToArray();
    }

    public string[] GetNotEmptyValues() => Values.Where(x => !string.IsNullOrEmpty(x)).ToArray()!;


    public override string ToString()
    {
        var sb = new StringBuilder(Name);
        if (Values.Length == 0)
        {
            return sb.ToString();
        }

        sb.Append($":{string.Join(',', Values.OrderDescending().Select(x => $"'{x}'"))}");
        return sb.ToString();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        const string nullValue = "_null_";

        yield return Name;

        foreach (var component in Values)
        {
            // Needed to distinguish an empty string from null value.
            yield return component ?? nullValue;
        }
    }
}

public class LdapAttributeName : ValueObject
{
    public string Value { get; }
    
    public LdapAttributeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        Value = name;
    }

    public static implicit operator string(LdapAttributeName name)
    {
        if (name is null)
        {
            throw new InvalidCastException("Name is null");
        }

        return name.Value;
    }

    public static implicit operator LdapAttributeName(string name)
    {
        try
        {
            return new(name);
        }
        catch (Exception ex)
        {
            throw new InvalidCastException("Failed to cast", ex);
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;
}

public abstract class ValueObject
{
    private int? _hashCode;
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (GetType() != obj.GetType())
        {
            return false;
        }

        var valueObject = (ValueObject)obj;

        return GetEqualityComponents().SequenceEqual(valueObject.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        if (!_hashCode.HasValue)
        {
            _hashCode = GetEqualityComponents()
                .Aggregate(
                    1,
                    (current, obj) =>
                    {
                        unchecked
                        {
                            return current * 23 + (obj?.GetHashCode() ?? 0);
                        }
                    }
                );
        }

        return _hashCode.Value;
    }

    public static bool operator ==(ValueObject a, ValueObject b)
    {
        if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
        {
            return true;
        }

        if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
        {
            return false;
        }

        return a.Equals(b);
    }

    public static bool operator !=(ValueObject a, ValueObject b)
    {
        return !(a == b);
    }
}