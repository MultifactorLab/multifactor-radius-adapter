namespace Multifactor.Radius.Adapter.v2.Core.Radius.Packet
{
    // See https://datatracker.ietf.org/doc/html/rfc2865#section-3
    public enum PacketCode
    {
        AccessRequest = 1,
        AccessAccept = 2,
        AccessReject = 3,
        AccountingRequest = 4,
        AccountingResponse = 5,
        AccessChallenge = 11,
        StatusServer = 12,
        StatusClient = 13,
        DisconnectRequest = 40,
        DisconnectAck = 41,
        DisconnectNak = 42,
        CoaRequest = 43,
        CoaAck = 44,
        CoaNak = 45
    }
}
