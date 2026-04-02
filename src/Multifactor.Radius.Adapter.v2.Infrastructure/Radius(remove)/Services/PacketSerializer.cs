using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Ports;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius_remove_.Builders;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius_remove_.Validators;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius_remove_.Services;

internal sealed class PacketSerializer : IPacketSerializer
{
    private readonly IRadiusPacketBuilder _builder;
    private readonly IRadiusPacketValidator _validator;
    private readonly ILogger<PacketSerializer> _logger;

    public PacketSerializer(
        IRadiusPacketBuilder builder,
        IRadiusPacketValidator validator,
        ILogger<PacketSerializer> logger)
    {
        _builder = builder;
        _validator = validator;
        _logger = logger;
    }

    public byte[] Execute(RadiusPacket packet, SharedSecret sharedSecret)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(sharedSecret);

        try
        {
            _logger.LogDebug("Serializing RADIUS packet: Code={Code}, Id={Id}", packet.Code, packet.Identifier);
            
            _validator.ValidatePacketForSerialization(packet);
            
            var result = _builder.Build(packet, sharedSecret);
            
            _logger.LogDebug("Successfully serialized RADIUS packet, length: {Length}", result.Length);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize RADIUS packet: Code={Code}, Id={Id}", 
                packet.Code, packet.Identifier);
            throw new Exception("Failed to serialize RADIUS packet", ex);
        }
    }
}