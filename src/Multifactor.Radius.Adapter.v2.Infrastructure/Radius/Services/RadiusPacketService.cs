using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Exceptions;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Builders;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Parsers;
using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Validators;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Services;

public class RadiusPacketService : IRadiusPacketService
{
    private readonly IRadiusPacketParser _parser;
    private readonly IRadiusPacketBuilder _builder;
    private readonly IRadiusPacketValidator _validator;
    private readonly ILogger<RadiusPacketService> _logger;
    private readonly INasIdentifierExtractor _nasIdentifierExtractor;

    public RadiusPacketService(
        IRadiusPacketParser parser,
        IRadiusPacketBuilder builder,
        IRadiusPacketValidator validator,
        INasIdentifierExtractor nasIdentifierExtractor,
        ILogger<RadiusPacketService> logger)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _nasIdentifierExtractor = nasIdentifierExtractor ?? throw new ArgumentNullException(nameof(nasIdentifierExtractor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public RadiusPacket ParsePacket(byte[] packetBytes, SharedSecret sharedSecret, RadiusAuthenticator? requestAuthenticator = null)
    {
        if (packetBytes == null) throw new ArgumentNullException(nameof(packetBytes));
        if (sharedSecret == null) throw new ArgumentNullException(nameof(sharedSecret));

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
            throw new RadiusPacketException("Failed to parse RADIUS packet", ex);
        }
    }

    public byte[] SerializePacket(RadiusPacket packet, SharedSecret sharedSecret)
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
            throw new RadiusPacketException("Failed to serialize RADIUS packet", ex);
        }
    }


    public RadiusPacket CreateResponse(RadiusPacket request, PacketCode responseCode)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        try
        {
            _logger.LogDebug("Creating response packet for request Id={Id}, ResponseCode={ResponseCode}", 
                request.Identifier, responseCode);
            
            var response = _builder.CreateResponse(request, responseCode);
            
            _logger.LogDebug("Successfully created response packet: Code={Code}, Id={Id}", 
                response.Code, response.Identifier);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create response packet for request Id={Id}", request.Identifier);
            throw new RadiusPacketException("Failed to create response packet", ex);
        }
    }

    public bool TryGetNasIdentifier(byte[] packetBytes, out string nasIdentifier)
    {
        if (packetBytes == null) throw new ArgumentNullException(nameof(packetBytes));
        
        return _nasIdentifierExtractor.TryExtract(packetBytes, out nasIdentifier);
    }
}