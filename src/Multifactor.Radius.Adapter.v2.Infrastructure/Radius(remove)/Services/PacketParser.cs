using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius_remove_.Parsers;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius_remove_.Validators;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius_remove_.Services;

internal sealed class PacketParser : IPacketParser
{
    private readonly IRadiusPacketParser _parser;
    private readonly IRadiusPacketValidator _validator;
    private readonly ILogger<PacketParser> _logger;

    public PacketParser(
        IRadiusPacketParser parser,
        IRadiusPacketValidator validator,
        ILogger<PacketParser> logger)
    {
        _parser = parser;
        _validator = validator;
        _logger = logger;
    }
    public RadiusPacket Execute(byte[]? packetBytes, SharedSecret sharedSecret, RadiusAuthenticator? requestAuthenticator = null)
    {
        ArgumentNullException.ThrowIfNull(packetBytes);
        ArgumentNullException.ThrowIfNull(sharedSecret);

        try
        {
            _logger.LogDebug("Parsing RADIUS packet, length: {Length}", packetBytes.Length);
            
            _validator.ValidateRawPacket(packetBytes);

            var packet = requestAuthenticator == null ? _parser.Parse(packetBytes, sharedSecret) 
                : _parser.Parse(packetBytes, sharedSecret, requestAuthenticator);
            
            _validator.ValidateParsedPacket(packet, sharedSecret);
            
            _logger.LogDebug("Successfully parsed RADIUS packet: Code={Code}, Id={Id}, Attributes={AttributeCount}",
                packet.Code, packet.Identifier, packet.Attributes.Count);
            
            return packet;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse RADIUS packet. Length: {Length}", packetBytes.Length);
            throw new Exception("Failed to parse RADIUS packet", ex);
        }
    }
}