using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.Adapters.Radius;

internal interface IRadiusPacketValidator
{
    void ValidateRawPacket(byte[] packetBytes);
    void ValidateParsedPacket(RadiusPacket packet, SharedSecret sharedSecret);
    void ValidatePacketForSerialization(RadiusPacket packet);
}

internal sealed class RadiusPacketValidator : IRadiusPacketValidator
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
        if (!Enum.IsDefined(typeof(PacketCode), (int)code))
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
        ArgumentNullException.ThrowIfNull(packet);

        if (!Enum.IsDefined(typeof(PacketCode), packet.Code))
            throw new InvalidOperationException($"Invalid packet code for serialization: {packet.Code}");

        if (packet.Authenticator.Value.Length != 16)
            throw new InvalidOperationException("Authenticator must be 16 bytes for serialization");

        switch (packet.Code)
        {
            case PacketCode.AccessAccept:
            case PacketCode.AccessReject:
            case PacketCode.AccessChallenge:
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
        if (!packet.HasAttribute("User-Name"))
        {
            _logger.LogWarning("Access-Request missing User-Name attribute");
        }

        var hasPassword = packet.HasAttribute("User-Password");
        var hasChapPassword = packet.HasAttribute("CHAP-Password");
        var hasChapChallenge = packet.HasAttribute("CHAP-Challenge");

        if (!hasPassword && !(hasChapPassword && hasChapChallenge))
        {
            _logger.LogWarning("Access-Request missing authentication credentials");
        }
    }

    private static void ValidateAccountingRequest(RadiusPacket packet)
    {
        if (!packet.HasAttribute("Acct-Status-Type"))
        {
            throw new InvalidOperationException("Accounting-Request missing Acct-Status-Type");
        }
    }

    private void ValidateCoaRequest(RadiusPacket packet)
    {
        if (!packet.HasAttribute("User-Name") && !packet.HasAttribute("Acct-Session-Id"))
        {
            _logger.LogWarning("CoA/Disconnect request missing User-Name or Acct-Session-Id");
        }
    }
}