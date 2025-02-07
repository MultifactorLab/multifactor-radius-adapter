using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using MultiFactor.Radius.Adapter.Tests.E2E.Udp;
using MultiFactor.Radius.Adapter.Tests.Fixtures;

namespace MultiFactor.Radius.Adapter.Tests.E2E;

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

    internal static T GetSensitiveData<T>(string fileName, string sectionName)
    {
        var sensitiveDataPath = TestEnvironment.GetAssetPath(TestAssetLocation.E2ESensitiveData, fileName);

        IConfigurationRoot config = new ConfigurationBuilder()
            .AddJsonFile(sensitiveDataPath, optional: false, reloadOnChange: true)
            .Build();

        var sensitiveData = config.GetRequiredSection(sectionName).Get<T>();
        return sensitiveData;
    }

    internal static Dictionary<string, string> GetSensitiveData(string fileName)
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

    internal static string GetEnvPrefix(string envKey)
    {
        if (string.IsNullOrWhiteSpace(envKey))
            throw new ArgumentNullException(nameof(envKey));
        var parts = envKey.Split('_');
        return parts[0] + "_";
    }
}