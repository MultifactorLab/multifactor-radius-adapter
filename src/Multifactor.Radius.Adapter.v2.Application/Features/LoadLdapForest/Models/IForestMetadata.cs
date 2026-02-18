using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Models;

public class DomainInfo
{
    public string DnsName { get; set; }
    public string DistinguishedName { get; set; }
    public string NetBiosName { get; set; }
    public bool IsTrusted { get; set; }
    public List<string> UpnSuffixes { get; set; } = new();
}

public interface IForestMetadata
{
    string RootDomain { get; }
    IReadOnlyDictionary<string, DomainInfo> Domains { get; } // key = DNS name
    IReadOnlyDictionary<string, DomainInfo> UpnSuffixes { get; } // key = UPN suffix
    IReadOnlyDictionary<string, DomainInfo> NetBiosNames { get; } // key = NetBIOS name
    DomainInfo? GetDomainByNetBios(string netBiosName);
    IReadOnlyList<DomainInfo> GetDomainsByUpnSuffix(string suffix);
}

public class ForestMetadata : IForestMetadata
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
        if (string.IsNullOrEmpty(netBiosName)) return null;
        return NetBiosNames.TryGetValue(netBiosName.ToUpperInvariant(), out var domain)
            ? domain : null;
    }
    public IReadOnlyList<DomainInfo> GetDomainsByUpnSuffix(string suffix)
    {
        suffix = suffix.ToLowerInvariant();
        var result = new List<DomainInfo>();

        // Точные совпадения
        if (UpnSuffixes.TryGetValue(suffix, out var exact))
            result.Add(exact);

        // Частичные совпадения
        foreach (var kv in UpnSuffixes)
        {
            if (suffix.EndsWith(kv.Key) && !result.Contains(kv.Value))
                result.Add(kv.Value);
        }

        return result;
    }

}