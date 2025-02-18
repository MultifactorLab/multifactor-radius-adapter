using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Radius;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures.ConfigLoading;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Models;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Radius;
using Multifactor.Radius.Adapter.EndToEndTests.Udp;
using MultiFactor.Radius.Adapter.Extensions;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;

namespace Multifactor.Radius.Adapter.EndToEndTests;

public abstract class E2ETestBase(RadiusFixtures radiusFixtures) : IDisposable
{
    private IHost? _host;
    private readonly RadiusHostApplicationBuilder _radiusHostApplicationBuilder = RadiusHost.CreateApplicationBuilder([
        "--environment", "Test"
    ]);
    private readonly RadiusPacketParser _packetParser = radiusFixtures.Parser;
    private readonly SharedSecret? _secret = radiusFixtures.SharedSecret;
    private readonly UdpSocket _udpSocket = radiusFixtures.UdpSocket;

    private protected async Task StartHostAsync(
        string rootConfigName,
        string[]? clientConfigFileNames = null,
        string? envPrefix = null,
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

        ReplaceRadiusConfigs(rootConfigName, clientConfigFileNames, envPrefix: envPrefix);

        configure?.Invoke(_radiusHostApplicationBuilder);

        _host = _radiusHostApplicationBuilder.Build();

        await _host.StartAsync();
    }
    
    private protected async Task StartHostAsync(
        E2ERadiusConfiguration radiusConfiguration,
        Action<RadiusHostApplicationBuilder>? configure = null)
    {
        _radiusHostApplicationBuilder.AddLogging();

        _radiusHostApplicationBuilder.Services.ReplaceService(prov =>
        {
            var factory = prov.GetRequiredService<ServiceConfigurationFactory>();

            var config = factory.CreateConfig(radiusConfiguration.RootConfiguration);
            config.Validate();

            return config;
        });
        
        var clientConfigsProvider = new E2EClientConfigurationsProvider(radiusConfiguration.ClientConfigs);
        
        _radiusHostApplicationBuilder.Services.ReplaceService<IClientConfigurationsProvider>(clientConfigsProvider);

        _radiusHostApplicationBuilder.AddMiddlewares();

        _radiusHostApplicationBuilder.ConfigureApplication();

        configure?.Invoke(_radiusHostApplicationBuilder);

        _host = _radiusHostApplicationBuilder.Build();

        await _host.StartAsync();
    }

    protected IRadiusPacket SendPacketAsync(IRadiusPacket? radiusPacket)
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

    protected IRadiusPacket? CreateRadiusPacket(PacketCode packetCode, SharedSecret? secret = null)
    {
        IRadiusPacket? packet;
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
        string[]? clientConfigFileNames = null,
        string? envPrefix = null)
    {
        if (string.IsNullOrEmpty(rootConfigName))
            throw new ArgumentException("Empty config path");

        var clientConfigs = clientConfigFileNames?
            .Select(fileName => TestEnvironment.GetAssetPath(TestAssetLocation.E2EBaseConfigs, fileName))
            .ToArray() ?? [];

        var rootConfig = TestEnvironment.GetAssetPath(TestAssetLocation.E2EBaseConfigs, rootConfigName);

        _radiusHostApplicationBuilder.Services.Configure<TestConfigProviderOptions>(x =>
        {
            x.RootConfigFilePath = rootConfig;
            x.ClientConfigFilePaths = clientConfigs;
            x.EnvironmentVariablePrefix = envPrefix;
        });
    }

    public void Dispose()
    {
        _host?.StopAsync();
        _host?.Dispose();
    }
}