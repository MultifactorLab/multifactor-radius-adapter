using MultiFactor.Radius.Adapter.Core.Radius;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures.Radius;

internal static class RadiusPacketFactory
{
    public static IRadiusPacket AccessRequest()
    {
        return new RadiusPacket(PacketCode.AccessRequest, 0, "secret");
    }
    
    public static IRadiusPacket AccessChallenge()
    {
        return new RadiusPacket(PacketCode.AccessChallenge, 0, "secret");
    }
    
    public static IRadiusPacket AccessReject()
    {
        return new RadiusPacket(PacketCode.AccessReject, 0, "secret");
    }
}
