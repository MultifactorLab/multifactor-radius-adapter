using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Interop;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Forest;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;
using Multifactor.Radius.Adapter.v2.Services.LdapForest;

namespace Multifactor.Radius.Adapter.v2.Services.NetBios;

public class NetBiosService : INetBiosService
{
    private readonly ILogger _logger;
    private readonly IForestMetadataCache _forestMetadataCache;
    private readonly ILdapConnection _ldapConnection;
    private readonly IDomainPermissionRules _domainPermissionRules;

    public NetBiosService(IForestMetadataCache forestMetadataCache, ILdapConnection connection, ILogger logger, IDomainPermissionRules domainPermissionRules = null )
    {
        _logger = logger;
        _forestMetadataCache = forestMetadataCache;
        _ldapConnection = connection;
        _domainPermissionRules = domainPermissionRules;
    }

    public string ConvertNetBiosToUpn(string clientKey, UserIdentity identity, DistinguishedName domain)
    {
        var netBiosParts = new NetBiosParts(identity.Identity);
        var foundDomain = GetDomainByIdentityAsync(clientKey, domain, identity);
        var fqdn = LdapNamesUtils.DnToFqdn(foundDomain);
        return $"{netBiosParts.UserName}@{fqdn}";
    }

    public DistinguishedName GetDomainByIdentityAsync(string clientKey, DistinguishedName domain, UserIdentity identity)
    {
        if (identity?.Format != UserIdentityFormat.NetBiosName)
            throw new ArgumentException("Invalid identity");

        Throw.IfNull(domain, nameof(domain));
        var fqdn = LdapNamesUtils.DnToFqdn(domain);
        _logger.LogInformation("Trying to resolve domain by user: {UserName:l}.", identity.Identity);

        try
        {
            return TryStrictResolving(fqdn, identity);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Error during translate netbios name {UserName:l}", identity.Identity);
        }

        try
        {
            return TryFindSuitableSuffix(clientKey, fqdn, identity);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Error during translate netbios name {UserName:l}. Domain can't resolving, the request handling stopped.", identity.Identity);
            throw;
        }
    }

    private DistinguishedName TryStrictResolving(string possibleDomain, UserIdentity identity)
    {
        using (var nameTranslator = new NameTranslator(possibleDomain, _logger))
        {
            var netBiosDomain = nameTranslator.Translate(identity.Identity);
            if (!string.IsNullOrEmpty(netBiosDomain))
            {
                _logger.LogInformation("Success find {Netbios:l} by {UserName:l}", netBiosDomain, identity.Identity);
                return new DistinguishedName(netBiosDomain);
            }
        }
        
        throw new Exception($"Domain {possibleDomain} not found");
    }
    
    private DistinguishedName TryFindSuitableSuffix(string clientKey, string possibleDomain, UserIdentity identity)
    {
        _logger.LogInformation("Degradation of the domain resolving method for {UserName:l}", identity.Identity);

        _logger.LogDebug("Start connection to {Domain}", possibleDomain);
        var domainDn = LdapNamesUtils.FqdnToDn(possibleDomain);
        var schema = _forestMetadataCache.Get(clientKey, domainDn);

        if (schema == null)
        {
            schema = LoadSchema(domainDn);
            _forestMetadataCache.Add(clientKey, schema);
        }
        var netBiosParts = new NetBiosParts(identity.Identity);
        var userDomain = schema.FindDomainByNetbiosName(netBiosParts.Netbios);
        _logger.LogInformation("Success find {UserDomain:l} by {UserName:l}", userDomain, identity.Identity);
        return userDomain;
    }

    private IForestSchema LoadSchema(DistinguishedName domain)
    {
        var loader = new ForestSchemaLoader(_ldapConnection, _logger, _domainPermissionRules);
        return loader.Load(domain);
    }

    private class NetBiosParts
    {
        public string Netbios { get; set; }
        public string UserName { get; set; }

        public NetBiosParts(string identity)
        {
            var index = identity.IndexOf('\\');
            if (index <= 0)
                throw new ArgumentException($"Invalid NetBIOS identity: {identity}");

            Netbios = identity[..index];
            UserName = identity[(index + 1)..];
        }
    }
}