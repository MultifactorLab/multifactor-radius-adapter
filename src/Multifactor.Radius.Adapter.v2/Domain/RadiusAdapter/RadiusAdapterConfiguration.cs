//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System.Reflection;
using Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter.Sections;
using Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter.Sections.LdapServer;
using Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter.Sections.RadiusReply;
using Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter.Sections.UserNameTransform;

namespace Multifactor.Radius.Adapter.v2.Domain.RadiusAdapter;

public class RadiusAdapterConfiguration
{
    private static readonly Lazy<string[]> _sectionNames = new(GetSectionNames);
    
    public static string[] KnownSectionNames => _sectionNames.Value;
    
    public AppSettingsSection AppSettings { get; init; } = new();
    public RadiusReplySection RadiusReply { get; init; } = new();
    public UserNameTransformRulesSection UserNameTransformRules { get; init; } = new();
    public LdapServersSection LdapServers { get; init; } = new();

    private static string[] GetSectionNames()
    {
        return typeof(RadiusAdapterConfiguration)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.Name)
            .ToArray();
    }
}