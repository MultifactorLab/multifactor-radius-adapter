using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Connection;
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

    public NetBiosService(IForestMetadataCache forestMetadataCache, ILogger<NetBiosService> logger)
    {
        _logger = logger;
        _forestMetadataCache = forestMetadataCache;
    }

    public string ConvertNetBiosToUpn(NetBiosRequest request)
    {
        var netBiosParts = new NetBiosParts(request.UserIdentity.Identity);
        var foundDomain = GetDomainByIdentityAsync(request);
        var fqdn = LdapNamesUtils.DnToFqdn(foundDomain);
        return $"{netBiosParts.UserName}@{fqdn}";
    }

    public DistinguishedName GetDomainByIdentityAsync(NetBiosRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        
        if (request.UserIdentity.Format != UserIdentityFormat.NetBiosName)
            throw new ArgumentException("Invalid identity");
        
        var fqdn = LdapNamesUtils.DnToFqdn(request.Domain);
        _logger.LogInformation("Trying to resolve domain by user: {UserName:l}.", request.UserIdentity.Identity);

        try
        {
            return TryStrictResolving(fqdn, request.UserIdentity);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Error during translate netbios name {UserName:l}", request.UserIdentity.Identity);
        }

        try
        {
            return TryFindSuitableSuffix(request.ClientKey, fqdn, request.UserIdentity, request.Connection, request.DomainPermissionRules);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Error during translate netbios name {UserName:l}. Domain can't resolving, the request handling stopped.", request.UserIdentity.Identity);
            throw;
        }
    }

    private DistinguishedName TryStrictResolving(string possibleDomain, UserIdentity identity)
    {
        using (var nameTranslator = new NameTranslator(possibleDomain, _logger))
        {
            var netBiosDomain = nameTranslator.Translate(identity.Identity);
            if (!string.IsNullOrWhiteSpace(netBiosDomain))
            {
                _logger.LogInformation("Success find {Netbios:l} by {UserName:l}", netBiosDomain, identity.Identity);
                return new DistinguishedName(netBiosDomain);
            }
        }
        
        throw new Exception($"Domain {possibleDomain} not found");
    }
    
    private DistinguishedName TryFindSuitableSuffix(string clientKey, string possibleDomain, UserIdentity identity, ILdapConnection connection, IDomainPermissionRules? domainPermissionRules = null)
    {
        _logger.LogInformation("Degradation of the domain resolving method for {UserName:l}", identity.Identity);

        _logger.LogDebug("Start connection to {Domain}", possibleDomain);
        var domainDn = LdapNamesUtils.FqdnToDn(possibleDomain);
        var schema = _forestMetadataCache.Get(clientKey, domainDn);

        if (schema == null)
        {
            schema = LoadSchema(domainDn, connection, domainPermissionRules);
            _forestMetadataCache.Add(clientKey, schema);
        }
        var netBiosParts = new NetBiosParts(identity.Identity);
        var userDomain = schema.FindDomainByNetbiosName(netBiosParts.Netbios);
        _logger.LogInformation("Successfully found {UserDomain:l} by {UserName:l}", userDomain, identity.Identity);
        return userDomain;
    }

    private IForestSchema LoadSchema(DistinguishedName domain, ILdapConnection connection, IDomainPermissionRules? domainPermissionRules = null)
    {
        var loader = new ForestSchemaLoader(connection, _logger, domainPermissionRules);
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