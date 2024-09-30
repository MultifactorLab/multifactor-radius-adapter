//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System;
using System.Linq;
using System.Reflection;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.RadiusReply;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models.UserNameTransform;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;

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
}
