using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Extensions;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Tests.E2E.Udp;
using MultiFactor.Radius.Adapter.Tests.Fixtures;
using MultiFactor.Radius.Adapter.Tests.Fixtures.ConfigLoading;
using MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

namespace MultiFactor.Radius.Adapter.Tests.E2E;

public abstract class E2ETestBase : IDisposable
{
    private IHost? _host = null;
    private readonly RadiusHostApplicationBuilder _radiusHostApplicationBuilder;
    private readonly RadiusPacketParser _packetParser;
    private readonly SharedSecret _secret;
    private readonly UdpSocket _udpSocket;
    private Dictionary<string, string> _environmentVariables;

    protected E2ETestBase(RadiusFixtures radiusFixtures)
    {
        _radiusHostApplicationBuilder = RadiusHost.CreateApplicationBuilder(new[]
        {
            "--environment", "Test"
        });
        _packetParser = radiusFixtures.Parser;
        _secret = radiusFixtures.SharedSecret;
        _udpSocket = radiusFixtures.UdpSocket;
    }

    private protected async Task StartHostAsync(
        string rootConfigName,
        string[] clientConfigFileNames = null,
        Dictionary<string, string> environmentVariables = null,
        Action<RadiusHostApplicationBuilder>? configure = null)
    {
        _radiusHostApplicationBuilder.AddLogging();

        _radiusHostApplicationBuilder.Services.AddOptions<TestConfigProviderOptions>();
        _radiusHostApplicationBuilder.Services.ReplaceService(prov =>
        {
            var opt = prov.GetRequiredService<IOptions<TestConfigProviderOptions>>().Value;
            var rootConfig = TestRootConfigProvider.GetRootConfiguration(opt);
            var factory = prov.GetRequiredService<ServiceConfigurationFactory>();

            var config = factory.CreateConfig(rootConfig);
            config.Validate();

            return config;
        });

        _radiusHostApplicationBuilder.Services
            .ReplaceService<IClientConfigurationsProvider, TestClientConfigsProvider>();

        _radiusHostApplicationBuilder.AddMiddlewares();

        _radiusHostApplicationBuilder.ConfigureApplication();

        ReplaceRadiusConfigs(rootConfigName, clientConfigFileNames);

        configure?.Invoke(_radiusHostApplicationBuilder);

        _radiusHostApplicationBuilder.InternalHostApplicationBuilder.Configuration.AddRadiusEnvironmentVariables();

        SetEnvironmentVariables(environmentVariables);

        _host = _radiusHostApplicationBuilder.Build();

        await _host.StartAsync();
    }

    protected IRadiusPacket SendPacketAsync(IRadiusPacket radiusPacket)
    {
        if (radiusPacket is null)
        {
            throw new ArgumentNullException(nameof(radiusPacket));
        }

        var packetBytes = _packetParser.GetBytes(radiusPacket);
        _udpSocket.Send(packetBytes);

        var data = _udpSocket.Receive();
        var parsed = _packetParser.Parse(data.GetBytes(), _secret, radiusPacket.Authenticator.Value);

        return parsed;
    }

    protected IRadiusPacket CreateRadiusPacket(PacketCode packetCode, SharedSecret secret = null)
    {
        IRadiusPacket packet = null;
        switch (packetCode)
        {
            case PacketCode.AccessRequest:
                packet = RadiusPacketFactory.AccessRequest(secret ?? _secret);
                break;
            case PacketCode.StatusServer:
                packet = RadiusPacketFactory.StatusServer(secret ?? _secret);
                break;
            case PacketCode.AccessChallenge:
                packet = RadiusPacketFactory.AccessChallenge(secret ?? _secret);
                break;
            case PacketCode.AccessReject:
                packet = RadiusPacketFactory.AccessReject(secret ?? _secret);
                break;
            default:
                throw new NotImplementedException();
        }

        return packet;
    }

    private void ReplaceRadiusConfigs(
        string rootConfigName,
        string[] clientConfigFileNames = null)
    {
        if (string.IsNullOrEmpty(rootConfigName))
            throw new ArgumentException("Empty config path");

        var clientConfigs = clientConfigFileNames?
            .Select(fileName => TestEnvironment.GetAssetPath(TestAssetLocation.E2EBaseConfigs, fileName))
            .ToArray() ?? Array.Empty<string>();

        var rootConfig = TestEnvironment.GetAssetPath(TestAssetLocation.E2EBaseConfigs, rootConfigName);

        _radiusHostApplicationBuilder.Services.Configure<TestConfigProviderOptions>(x =>
        {
            x.RootConfigFilePath = rootConfig;
            x.ClientConfigFilePaths = clientConfigs;
        });
    }

    private void SetEnvironmentVariables(Dictionary<string, string> environmentVariables)
    {
        if (environmentVariables?.Any() != true)
            return;
        _environmentVariables = environmentVariables;
        foreach (var variable in environmentVariables)
        {
            Environment.SetEnvironmentVariable(variable.Key, variable.Value);
        }
    }

    private void UnsetEnvironmentVariables()
    {
        if (_environmentVariables?.Any() != true)
            return;
        foreach (var variable in _environmentVariables)
        {
            Environment.SetEnvironmentVariable(variable.Key, null);
        }
    }

    public void Dispose()
    {
        _host?.StopAsync();
        _host?.Dispose();
        UnsetEnvironmentVariables();
    }
}