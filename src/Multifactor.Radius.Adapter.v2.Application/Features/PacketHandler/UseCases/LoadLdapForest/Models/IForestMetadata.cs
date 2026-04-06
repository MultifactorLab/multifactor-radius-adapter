using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using System.DirectoryServices.Protocols;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Models;

public sealed class DomainInfo
{
    public required string ConnectionString { get; init; }
    public required string DnsName { get; init; }
    public required string DistinguishedName { get; init; }
    public required string NetBiosName { get; init; }
    public required ILdapSchema Schema { get; init; }
    public required bool IsTrusted { get; init; }
    public List<string> UpnSuffixes { get; init; } = [];
    public AuthType GetAuthType() => IsTrusted ? AuthType.Negotiate : AuthType.Basic;
}

public interface IForestMetadata
{
    IReadOnlyDictionary<string, DomainInfo> Domains { get; } // key = DNS name
    IReadOnlyDictionary<string, DomainInfo> UpnSuffixes { get; } // key = UPN suffix
    IReadOnlyDictionary<string, DomainInfo> NetBiosNames { get; } // key = NetBIOS name
    DomainInfo? DetermineForestDomain(UserIdentity userIdentity);
}

public sealed class ForestMetadata : IForestMetadata
{
    public Dictionary<string, DomainInfo> Domains { get; set; } = [];
    public Dictionary<string, DomainInfo> UpnSuffixes { get; set; } = [];
    public Dictionary<string, DomainInfo> NetBiosNames { get; set; } = [];

    IReadOnlyDictionary<string, DomainInfo> IForestMetadata.Domains => Domains;
    IReadOnlyDictionary<string, DomainInfo> IForestMetadata.UpnSuffixes => UpnSuffixes;
    IReadOnlyDictionary<string, DomainInfo> IForestMetadata.NetBiosNames => NetBiosNames;

    public DomainInfo? DetermineForestDomain(UserIdentity userIdentity)
    {
        return userIdentity.Format switch
        {
            UserIdentityFormat.UserPrincipalName => GetDomainByUpnSuffix(userIdentity.GetUpnSuffix()),
            UserIdentityFormat.NetBiosName => GetDomainByNetBios(userIdentity.Identity),
            _ => null
        };
    }

    public DomainInfo? GetDomainByNetBios(string netBiosName)
    {
        return string.IsNullOrEmpty(netBiosName) ? null 
            : NetBiosNames.GetValueOrDefault(netBiosName.ToUpperInvariant());
    }

    private DomainInfo? GetDomainByUpnSuffix(string suffix)
    {
        var domains = FindDomainsByUpnSuffix(suffix);
        return domains.Count == 0 ? null : domains[0];
    }

    private IReadOnlyList<DomainInfo> FindDomainsByUpnSuffix(string suffix)
    {
        suffix = suffix.ToLowerInvariant();
        var result = new List<DomainInfo>();

        if (UpnSuffixes.TryGetValue(suffix, out var exact))
            result.Add(exact);

        foreach (var kv in UpnSuffixes.Where(kv => suffix.EndsWith(kv.Key) && !result.Contains(kv.Value)))
        {
            result.Add(kv.Value);
        }
        return result;
    }
}