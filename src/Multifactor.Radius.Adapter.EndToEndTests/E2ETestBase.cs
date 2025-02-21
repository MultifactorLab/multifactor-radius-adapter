using System.Text;
using LdapForNet;
using LdapForNet.Native;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MultiFactor.Radius.Adapter.Core.Framework;
using MultiFactor.Radius.Adapter.Core.Radius;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures.ConfigLoading;
using Multifactor.Radius.Adapter.EndToEndTests.Fixtures.Radius;
using Multifactor.Radius.Adapter.EndToEndTests.Udp;
using MultiFactor.Radius.Adapter.Extensions;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ClientLevel;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.ConfigurationLoading;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;
using MultiFactor.Radius.Adapter.Services.Ldap;
using MultiFactor.Radius.Adapter.Services.Ldap.Connection;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;

namespace Multifactor.Radius.Adapter.EndToEndTests;

public abstract class E2ETestBase(RadiusFixtures radiusFixtures) : IDisposable
{
    private IHost? _host;
    private ProfileLoader? _profileLoader;
    private ClientConfigurationFactory _clientConfigurationFactory;
    
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
        
        _profileLoader = _host.Services.GetService<ProfileLoader>();
        _clientConfigurationFactory = _host.Services.GetService<ClientConfigurationFactory>();
        
        await _host.StartAsync();
    }
    
    private protected async Task StartHostAsync(
        RadiusAdapterConfiguration rootConfig,
        Dictionary<string, RadiusAdapterConfiguration>? clientConfigs = null,
        Action<RadiusHostApplicationBuilder>? configure = null)
    {
        _radiusHostApplicationBuilder.AddLogging();

        _radiusHostApplicationBuilder.Services.ReplaceService(prov =>
        {
            var factory = prov.GetRequiredService<ServiceConfigurationFactory>();

            var config = factory.CreateConfig(rootConfig);
            config.Validate();

            return config;
        });
        
        var clientConfigsProvider = new E2EClientConfigurationsProvider(clientConfigs);
        
        _radiusHostApplicationBuilder.Services.ReplaceService<IClientConfigurationsProvider>(clientConfigsProvider);

        _radiusHostApplicationBuilder.AddMiddlewares();

        _radiusHostApplicationBuilder.ConfigureApplication();

        configure?.Invoke(_radiusHostApplicationBuilder);

        _host = _radiusHostApplicationBuilder.Build();
        
        _profileLoader = _host.Services.GetService<ProfileLoader>();
        _clientConfigurationFactory = _host.Services.GetService<ClientConfigurationFactory>();
        
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

    protected IRadiusPacket? CreateRadiusPacket(PacketCode packetCode, SharedSecret? secret = null, byte identifier = 0)
    {
        IRadiusPacket? packet;
        switch (packetCode)
        {
            case PacketCode.AccessRequest:
                packet = RadiusPacketFactory.AccessRequest(secret ?? _secret, identifier);
                break;
            case PacketCode.StatusServer:
                packet = RadiusPacketFactory.StatusServer(secret ?? _secret, identifier);
                break;
            case PacketCode.AccessChallenge:
                packet = RadiusPacketFactory.AccessChallenge(secret ?? _secret, identifier);
                break;
            case PacketCode.AccessReject:
                packet = RadiusPacketFactory.AccessReject(secret ?? _secret, identifier);
                break;
            default:
                throw new NotImplementedException();
        }

        return packet;
    }
    
    protected async Task SetAttributeForUserInCatalogAsync(
        string userName,
        RadiusAdapterConfiguration config,
        string attributeName,
        object attributeValue)
    {
        var clientConfiguration = CreateClientConfiguration(config);
        
        var user = LdapIdentity.ParseUser(userName);
        using var connection = LdapConnectionAdapter.CreateAsTechnicalAccAsync(
            clientConfiguration.ActiveDirectoryDomain,
            clientConfiguration,
            NullLogger<LdapConnectionAdapter>.Instance);

        var formatter = new BindIdentityFormatter(clientConfiguration);
        var serviceUser = LdapIdentity.ParseUser(clientConfiguration.ServiceAccountUser);
        await connection.BindAsync(formatter.FormatIdentity(serviceUser, clientConfiguration.ActiveDirectoryDomain), clientConfiguration.ServiceAccountPassword);
           
        var profile = await _profileLoader.LoadAsync(clientConfiguration, connection, user);
        var isFreeIpa = clientConfiguration.IsFreeIpa && clientConfiguration.FirstFactorAuthenticationSource != AuthenticationSource.ActiveDirectory;
        var request = BuildModifyRequest(profile.DistinguishedName, attributeName, attributeValue, isFreeIpa);
        var response = await connection.SendRequestAsync(request);

        if (response.ResultCode != Native.ResultCode.Success)
        {
            throw new Exception($"Failed to set attribute: {response.ResultCode}");
        }
    }
    
    protected IClientConfiguration CreateClientConfiguration(RadiusAdapterConfiguration configuration)
    {
        var serviceConfig = _host.Services.GetService<IServiceConfiguration>();
        return _clientConfigurationFactory.CreateConfig("e2e", configuration, serviceConfig);
    }
    
    private ModifyRequest BuildModifyRequest(
        string dn,
        string attributeName,
        object attributeValue,
        bool isFreeIpa)
    {
        var attrName = attributeName;
        
        var attribute = new DirectoryModificationAttribute
        {
            Name = attrName,
            LdapModOperation = Native.LdapModOperation.LDAP_MOD_REPLACE
        };
        
        var bytes = Encoding.UTF8.GetBytes(attributeValue.ToString());
        if (isFreeIpa)
            attribute.Add(bytes);
        else
            attribute.Add(bytes);
        

        return new ModifyRequest(dn, attribute);
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