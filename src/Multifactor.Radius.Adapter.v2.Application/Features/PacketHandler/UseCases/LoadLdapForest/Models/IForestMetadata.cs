namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Models;

public sealed class DomainInfo
{
    public string DnsName { get; init; }
    public string DistinguishedName { get; init; }
    public string NetBiosName { get; init; }
    public bool IsTrusted { get; set; }
    public List<string> UpnSuffixes { get; init; } = [];
}

public interface IForestMetadata //todo
{
    string RootDomain { get; }
    IReadOnlyDictionary<string, DomainInfo> Domains { get; } // key = DNS name
    IReadOnlyDictionary<string, DomainInfo> UpnSuffixes { get; } // key = UPN suffix
    IReadOnlyDictionary<string, DomainInfo> NetBiosNames { get; } // key = NetBIOS name
    DomainInfo? GetDomainByNetBios(string netBiosName);
    IReadOnlyList<DomainInfo> GetDomainsByUpnSuffix(string suffix);
}

public sealed class ForestMetadata : IForestMetadata //TODO
{
    public string RootDomain { get; set; }
    public Dictionary<string, DomainInfo> Domains { get; set; } = new();
    public Dictionary<string, DomainInfo> UpnSuffixes { get; set; } = new();
    public Dictionary<string, DomainInfo> NetBiosNames { get; set; } = new();

    IReadOnlyDictionary<string, DomainInfo> IForestMetadata.Domains => Domains;
    IReadOnlyDictionary<string, DomainInfo> IForestMetadata.UpnSuffixes => UpnSuffixes;
    IReadOnlyDictionary<string, DomainInfo> IForestMetadata.NetBiosNames => NetBiosNames;

    public DomainInfo? GetDomainByNetBios(string netBiosName)
    {
        return string.IsNullOrEmpty(netBiosName) ? null 
            : NetBiosNames.GetValueOrDefault(netBiosName.ToUpperInvariant());
    }
    
    public IReadOnlyList<DomainInfo> GetDomainsByUpnSuffix(string suffix)
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