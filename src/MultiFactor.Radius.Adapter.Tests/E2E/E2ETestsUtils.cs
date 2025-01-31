using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Radius;
using MultiFactor.Radius.Adapter.Core.Radius.Attributes;
using MultiFactor.Radius.Adapter.Tests.E2E.Udp;

namespace MultiFactor.Radius.Adapter.Tests.E2E;

internal static class E2ETestsUtils
{
    internal static RadiusPacketParser GetRadiusPacketParser()
    {
        var appVar = new ApplicationVariables
        {
            AppPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? throw new Exception()
        };
        var dict = new RadiusDictionary(appVar);
        dict.Read();

        return new RadiusPacketParser(NullLogger<RadiusPacketParser>.Instance, dict);
    }

    internal static UdpSocket GetUdpSocket(string ip, int port)
    {
        return new UdpSocket(IPAddress.Parse(ip), port);
    }
}