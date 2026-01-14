using System.Net;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Models.Enum;
using NetTools;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Parser.ValueParser;

public interface IValueParser
{
    T ParseEnum<T>(string? value, T defaultValue = default, bool required = false) where T : struct;
    bool ParseBool(string? value, bool defaultValue);
    int ParseInt(string? value, int defaultValue);
    TimeSpan ParseTimeSpan(string? value, TimeSpan? defaultValue = null);
    TimeSpan ParseTimeout(string? value, TimeSpan defaultValue);
    IPEndPoint? ParseEndpoint(string? value, bool required = false);
    public IPEndPoint[] ParseEndpoints(string? value, char separator = ';', bool required = false);
    Uri? ParseUri(string? value, bool required = false);
    IPAddress? ParseIpAddress(string? value, bool required = false);
    IReadOnlyList<Uri> ParseUrls(string? value, bool required = false);
    IReadOnlyList<IPAddressRange> ParseIpRanges(string? value);
    IReadOnlyList<DistinguishedName> ParseDistinguishedNames(string? value);
    IReadOnlyList<string> ParseStringList(string? value, char separator = ';');
    (PrivacyMode Mode, string[] Fields) ParsePrivacyModeWithFields(string? value);
    (int min, int max) ParseDelaySettings(string value);
}
