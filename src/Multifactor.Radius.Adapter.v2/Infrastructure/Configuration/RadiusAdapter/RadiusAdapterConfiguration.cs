//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System.Reflection;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.LdapServer;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.RadiusReply;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter.Sections.UserNameTransform;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;

public class RadiusAdapterConfiguration
{
    private static readonly Lazy<string[]> _knownSectionNames;
    public static string[] KnownSectionNames => _knownSectionNames.Value;

    static RadiusAdapterConfiguration()
    {
        _knownSectionNames = new Lazy<string[]>(() =>
        {
            return typeof(RadiusAdapterConfiguration)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Select(x => x.Name)
                .ToArray();
        });
    }
    
    public AppSettingsSection AppSettings { get; init; } = new();
    public RadiusReplySection RadiusReply { get; init; } = new();
    public UserNameTransformRulesSection UserNameTransformRules { get; init; } = new();
    public LdapServersSection LdapServers { get; init; } = new();
}
