using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models.Enums;
using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Ports;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Validators;

public class RadiusPacketValidator : IRadiusPacketValidator
{
    private readonly ILogger<RadiusPacketValidator> _logger;

    public RadiusPacketValidator(ILogger<RadiusPacketValidator> logger)
    {
        _logger = logger;
    }

    public void ValidateRawPacket(byte[] packetBytes)
    {
        ArgumentNullException.ThrowIfNull(packetBytes);

        if (packetBytes.Length < 20)
            throw new InvalidOperationException($"Packet too short: {packetBytes.Length} bytes");

        if (packetBytes.Length > 4096)
            throw new InvalidOperationException($"Packet too large: {packetBytes.Length} bytes");

        byte code = packetBytes[0];
        if (!Enum.IsDefined(typeof(PacketCode), code))
            throw new InvalidOperationException($"Invalid packet code: {code}");

        ushort declaredLength = BitConverter.ToUInt16([packetBytes[3], packetBytes[2]], 0);
        if (declaredLength != packetBytes.Length)
            throw new InvalidOperationException(
                $"Packet length mismatch. Declared: {declaredLength}, Actual: {packetBytes.Length}");

        _logger.LogDebug("Raw packet validation passed: Length={Length}, Code={Code}", 
            packetBytes.Length, (PacketCode)code);
    }

    public void ValidateParsedPacket(RadiusPacket packet, SharedSecret sharedSecret)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(sharedSecret);

        if (!Enum.IsDefined(typeof(PacketCode), packet.Code))
            throw new InvalidOperationException($"Invalid packet code: {packet.Code}");

        if (packet.Authenticator.Value.Length != 16)
            throw new InvalidOperationException("Authenticator must be 16 bytes");

        if (packet.RequestAuthenticator != null && packet.RequestAuthenticator.Value.Length != 16)
            throw new InvalidOperationException("Request authenticator must be 16 bytes");

        switch (packet.Code)
        {
            case PacketCode.AccessRequest:
                ValidateAccessRequest(packet);
                break;
            case PacketCode.AccountingRequest:
                ValidateAccountingRequest(packet);
                break;
            case PacketCode.DisconnectRequest:
            case PacketCode.CoaRequest:
                ValidateCoaRequest(packet);
                break;
        }

        _logger.LogDebug("Parsed packet validation passed: Code={Code}, Id={Id}, Attributes={AttributeCount}",
            packet.Code, packet.Identifier, packet.Attributes.Count);
    }

    public void ValidatePacketForSerialization(RadiusPacket packet)
    {
        if (packet == null)
            throw new ArgumentNullException(nameof(packet));

        if (!Enum.IsDefined(typeof(PacketCode), packet.Code))
            throw new InvalidOperationException($"Invalid packet code for serialization: {packet.Code}");

        if (packet.Authenticator.Value.Length != 16)
            throw new InvalidOperationException("Authenticator must be 16 bytes for serialization");

        // Check for required attributes based on packet type
        switch (packet.Code)
        {
            case PacketCode.AccessAccept:
            case PacketCode.AccessReject:
            case PacketCode.AccessChallenge:
                // Response packets should have request authenticator
                if (packet.RequestAuthenticator == null)
                {
                    _logger.LogWarning("Response packet missing request authenticator: Code={Code}", packet.Code);
                }
                break;
        }

        _logger.LogDebug("Packet ready for serialization: Code={Code}, Id={Id}", packet.Code, packet.Identifier);
    }

    private void ValidateAccessRequest(RadiusPacket packet)
    {
        // Access-Request should have User-Name
        if (!packet.HasAttribute("User-Name"))
        {
            _logger.LogWarning("Access-Request missing User-Name attribute");
        }

        // Should have either User-Password or CHAP-Password/Challenge
        bool hasPassword = packet.HasAttribute("User-Password");
        bool hasChapPassword = packet.HasAttribute("CHAP-Password");
        bool hasChapChallenge = packet.HasAttribute("CHAP-Challenge");

        if (!hasPassword && !(hasChapPassword && hasChapChallenge))
        {
            _logger.LogWarning("Access-Request missing authentication credentials");
        }
    }

    private void ValidateAccountingRequest(RadiusPacket packet)
    {
        // Accounting-Request should have Acct-Status-Type
        if (!packet.HasAttribute("Acct-Status-Type"))
        {
            throw new InvalidOperationException("Accounting-Request missing Acct-Status-Type");
        }
    }

    private void ValidateCoaRequest(RadiusPacket packet)
    {
        // CoA/Disconnect should have specific attributes
        if (!packet.HasAttribute("User-Name") && !packet.HasAttribute("Acct-Session-Id"))
        {
            _logger.LogWarning("CoA/Disconnect request missing User-Name or Acct-Session-Id");
        }
    }
}