using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Models;
using Multifactor.Radius.Adapter.EndToEndTests.Udp;

namespace Multifactor.Radius.Adapter.EndToEndTests;

internal static class E2ETestsUtils
{
    internal static RadiusPacketParser GetRadiusPacketParser()
    {
        var appVar = new ApplicationVariables
        {
            AppPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? throw new Exception()
        };
        var dict = new RadiusDictionary(appVar);
        dict.Read();

        return new RadiusPacketParser(NullLogger<RadiusPacketParser>.Instance, dict);
    }

    internal static UdpSocket GetUdpSocket(string ip, int port)
    {
        return new UdpSocket(IPAddress.Parse(ip), port);
    }

    internal static Dictionary<string, string> GetEnvironmentVariables(string fileName)
    {
        var envs = new Dictionary<string, string>();
        var sensitiveDataPath = TestEnvironment.GetAssetPath(TestAssetLocation.E2ESensitiveData, fileName);

        var lines = File.ReadLines(sensitiveDataPath);
        foreach (var line in lines)
        {
            var parts = line.Split('=');
            envs.Add(parts[0].Trim(), parts[1].Trim());
        }

        return envs;
    }
    
    internal static ConfigSensitiveData[] GetConfigSensitiveData(string fileName)
    {
        var sensitiveDataPath = TestEnvironment.GetAssetPath(TestAssetLocation.E2ESensitiveData, fileName);

        var lines = File.ReadLines(sensitiveDataPath);
        var sensitiveData = new List<ConfigSensitiveData>();
        
        foreach (var line in lines)
        {
            var parts = line.Split('_');
            var data = sensitiveData.FirstOrDefault(x => x.ConfigName == parts[0].Trim());
            if (data != null)
            {
                data.AddConfigValue(parts[1].Trim(), parts[2].Trim());
            }
            else
            {
                var newElement = new ConfigSensitiveData(parts[0].Trim());
                newElement.AddConfigValue(parts[1].Trim(), parts[2].Trim());
                sensitiveData.Add(newElement);
            }
        }

        return sensitiveData.ToArray();
    }

    internal static string GetEnvPrefix(string envKey)
    {
        if (string.IsNullOrWhiteSpace(envKey))
            throw new ArgumentNullException(nameof(envKey));
        var parts = envKey.Split('_');
        if (parts?.Length > 0)
        {
            return parts[0] + "_";
        }
        
        throw new ArgumentException($"Invalid env key: {envKey}");
    }
}