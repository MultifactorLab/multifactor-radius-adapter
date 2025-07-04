using System.DirectoryServices.Protocols;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client.Build;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Service.Build;
using Multifactor.Radius.Adapter.v2.Core.Pipeline;
using Multifactor.Radius.Adapter.v2.Core.Radius;
using Multifactor.Radius.Adapter.v2.Core.Radius.Packet;
using Multifactor.Radius.Adapter.v2.EndToEndTests.Fixtures;
using Multifactor.Radius.Adapter.v2.EndToEndTests.Udp;
using Multifactor.Radius.Adapter.v2.Extensions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter;
using Multifactor.Radius.Adapter.v2.Server;
using Multifactor.Radius.Adapter.v2.Server.Udp;
using Multifactor.Radius.Adapter.v2.Services.AdapterResponseSender;
using Multifactor.Radius.Adapter.v2.Services.Ldap;
using Multifactor.Radius.Adapter.v2.Services.Radius;
using ILdapConnectionFactory = Multifactor.Radius.Adapter.v2.Core.Ldap.ILdapConnectionFactory;

namespace Multifactor.Radius.Adapter.v2.EndToEndTests;

public abstract class E2ETestBase(RadiusFixtures radiusFixtures) : IDisposable
{
    private IHost? _host;
    private IClientConfigurationFactory? _clientConfigurationFactory;
    private IRadiusPacketService _radiusPacketService = radiusFixtures.Parser;
    private readonly SharedSecret _secret = radiusFixtures.SharedSecret;
    private readonly UdpSocket _udpSocket = radiusFixtures.UdpSocket;
    
    private protected async Task StartHostAsync(
        RadiusAdapterConfiguration rootConfig,
        Dictionary<string, RadiusAdapterConfiguration>? clientConfigs = null,
        Action<HostApplicationBuilder>? configure = null)
    {
        var builder = Host.CreateApplicationBuilder(["--environment", "Test"]);
        builder.Services.AddMemoryCache();
        builder.Services.AddAdapterLogging();
    
        var appVars = new ApplicationVariables
        {
            AppPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory),
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
            StartedAt = DateTime.Now
        };
    
        builder.Services.AddSingleton(appVars);
        builder.Services.AddRadiusDictionary();
        builder.Services.AddConfiguration();
        
        builder.Services.ReplaceService(prov =>
        {
            var factory = prov.GetRequiredService<IServiceConfigurationFactory>();

            var config = factory.CreateConfig(rootConfig);

            return config;
        });

        var clientConfigsProvider = new E2EClientConfigurationsProvider(clientConfigs);
        builder.Services.ReplaceService<IClientConfigurationsProvider>(clientConfigsProvider);
        builder.Services.AddLdapSchemaLoader();
        builder.Services.AddDataProtectionService();
    
        builder.Services.AddFirstFactor();
        builder.Services.AddPipelines();
        
        builder.Services.AddSingleton<IUdpPacketHandler, UdpPacketHandler>();
        builder.Services.AddTransient<IResponseSender, AdapterResponseSender>();
        builder.Services.AddServices();
        builder.Services.AddChallenge();
        builder.Services.AddUdpClient();
        builder.Services.AddMultifactorHttpClient();
    
        builder.Services.AddSingleton<AdapterServer>();
        builder.Services.AddHostedService<ServerHost>();
        
        configure?.Invoke(builder);
        
        _host = builder.Build();
        
        _clientConfigurationFactory = _host.Services.GetService<IClientConfigurationFactory>();
        
        await _host.StartAsync();
    }
    
    protected IRadiusPacket SendPacketAsync(IRadiusPacket? radiusPacket, SharedSecret? secret = null)
    {
        ArgumentNullException.ThrowIfNull(radiusPacket);

        var packetBytes = _radiusPacketService.GetBytes(radiusPacket, secret ?? _secret);
        _udpSocket.Send(packetBytes);

        var data = _udpSocket.Receive();
        var parsed = _radiusPacketService.Parse(data.GetBytes(), secret ?? _secret, radiusPacket.Authenticator);

        return parsed;
    }
    
    protected RadiusPacket CreateRadiusPacket(PacketCode packetCode, byte identifier = 0)
    {
        RadiusPacket packet;
        switch (packetCode)
        {
            case PacketCode.AccessRequest:
                packet = RadiusPacketFactory.AccessRequest(identifier);
                break;
            case PacketCode.StatusServer:
                packet = RadiusPacketFactory.StatusServer(identifier);
                break;
            case PacketCode.AccessChallenge:
                packet = RadiusPacketFactory.AccessChallenge(identifier);
                break;
            case PacketCode.AccessReject:
                packet = RadiusPacketFactory.AccessReject(identifier);
                break;
            default:
                throw new NotImplementedException();
        }

        return packet;
    }
    
    protected void SetAttributeForUserInCatalogAsync(
        DistinguishedName userDn,
        RadiusAdapterConfiguration config,
        string attributeName,
        object attributeValue)
    {
        var clientConfiguration = CreateClientConfiguration(config);
        var connectionFactory = _host!.Services.GetRequiredService<ILdapConnectionFactory>();
        var serverConfig = clientConfiguration.LdapServers.First();
        
        using var connection = connectionFactory.CreateConnection(new LdapConnectionOptions(
            new LdapConnectionString(serverConfig.ConnectionString),
            AuthType.Basic,
            serverConfig.UserName,
            serverConfig.Password,
            TimeSpan.FromSeconds(serverConfig.BindTimeoutInSeconds)));
        
        var request = BuildModifyRequest(userDn, attributeName, attributeValue);
        var response = connection.SendRequest(request);

        if (response.ResultCode != ResultCode.Success)
            throw new Exception($"Failed to set attribute: {response.ResultCode}");
    }
    
    protected IClientConfiguration CreateClientConfiguration(RadiusAdapterConfiguration configuration)
    {
        var serviceConfig = _host!.Services.GetService<IServiceConfiguration>();
        return _clientConfigurationFactory!.CreateConfig("e2e", configuration, serviceConfig!);
    }
    
    private ModifyRequest BuildModifyRequest(
        DistinguishedName dn,
        string attributeName,
        object attributeValue)
    {
        var attribute = new DirectoryAttributeModification
        {
            Name = attributeName,
            Operation = DirectoryAttributeOperation.Replace
        };
        
        var bytes = Encoding.UTF8.GetBytes(attributeValue.ToString());
        attribute.Add(bytes);
        
        return new ModifyRequest(dn.StringRepresentation, attribute);
    }
    
    public void Dispose()
    {
        _host?.StopAsync();
        _host?.Dispose();
    }
}