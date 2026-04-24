using System.Net;

namespace Multifactor.Radius.Adapter.v2.Application.Core.Models;

public readonly struct IpEntry
{
    private readonly ulong _lower;
    private readonly ulong _upper;
    private readonly ulong _maskLower;
    private readonly ulong _maskUpper;
    private readonly byte _type; // 0=single, 1=cidr, 2=range
    private readonly byte _isV6;

    private IpEntry(ulong lower, ulong upper, ulong maskLower, ulong maskUpper, byte type, bool isV6)
    {
        _lower = lower;
        _upper = upper;
        _maskLower = maskLower;
        _maskUpper = maskUpper;
        _type = type;
        _isV6 = isV6 ? (byte)1 : (byte)0;
    }
    
    public static bool Matches(IReadOnlyList<IpEntry> entries, IPAddress ip)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].Matches(ip))
                return true;
        }
        return false;
    }

    public static IpEntry Single(IPAddress ip)
        => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
            ? CreateV6Single(ip)
            : CreateV4Single(ip);

    public static IpEntry Cidr(IPAddress network, int prefix)
        => network.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
            ? CreateV6Cidr(network, prefix)
            : CreateV4Cidr(network, prefix);

    public static IpEntry Range(IPAddress start, IPAddress end)
        => start.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
            ? CreateV6Range(start, end)
            : CreateV4Range(start, end);

    private static IpEntry CreateV4Single(IPAddress ip)
    {
        uint val = IpV4ToUInt(ip);
        return new IpEntry(val, 0, 0, 0, 0, false);
    }

    private static IpEntry CreateV6Single(IPAddress ip)
    {
        var (high, low) = IpV6ToUInt64(ip);
        return new IpEntry(high, low, 0, 0, 0, true);
    }

    private static IpEntry CreateV4Cidr(IPAddress network, int prefix)
    {
        uint val = IpV4ToUInt(network);
        uint mask = prefix == 0 ? 0 : ~(0xFFFFFFFFu >> prefix);
        return new IpEntry(val, 0, mask, 0, 1, false);
    }

    private static IpEntry CreateV6Cidr(IPAddress network, int prefix)
    {
        var (high, low) = IpV6ToUInt64(network);
        
        ulong maskHigh, maskLow;
        if (prefix <= 64)
        {
            maskHigh = prefix == 0 ? 0 : ~(0xFFFFFFFFFFFFFFFFul >> prefix);
            maskLow = 0;
        }
        else
        {
            maskHigh = 0xFFFFFFFFFFFFFFFFul;
            int lowBits = prefix - 64;
            maskLow = lowBits == 0 ? 0 : ~(0xFFFFFFFFFFFFFFFFul >> lowBits);
        }

        return new IpEntry(high, low, maskHigh, maskLow, 1, true);
    }

    private static IpEntry CreateV4Range(IPAddress start, IPAddress end)
    {
        uint s = IpV4ToUInt(start);
        uint e = IpV4ToUInt(end);
        return new IpEntry(s, 0, e, 0, 2, false);
    }

    private static IpEntry CreateV6Range(IPAddress start, IPAddress end)
    {
        var (sHigh, sLow) = IpV6ToUInt64(start);
        var (eHigh, eLow) = IpV6ToUInt64(end);
        return new IpEntry(sHigh, sLow, eHigh, eLow, 2, true);
    }

    public bool Matches(IPAddress ip)
    {
        bool isV6 = ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;
        if (_isV6 != (isV6 ? 1 : 0)) return false;

        return isV6 ? MatchesV6(ip) : MatchesV4(ip);
    }

    private bool MatchesV4(IPAddress ip)
    {
        uint ipVal = IpV4ToUInt(ip);

        return _type switch
        {
            0 => ipVal == (uint)_lower,
            1 => (ipVal & (uint)_maskLower) == ((uint)_lower & (uint)_maskLower),
            2 => ipVal >= (uint)_lower && ipVal <= (uint)_maskLower,
            _ => false
        };
    }

    private bool MatchesV6(IPAddress ip)
    {
        var (high, low) = IpV6ToUInt64(ip);

        return _type switch
        {
            0 => high == _lower && low == _upper,
            1 => (high & _maskLower) == (_lower & _maskLower) && 
                 (low & _maskUpper) == (_upper & _maskUpper),
            2 => IsV6InRange(high, low),
            _ => false
        };
    }

    private bool IsV6InRange(ulong high, ulong low)
    {
        if (high < _lower || high > _maskLower) return false;
        if (high == _lower && low < _upper) return false;
        if (high == _maskLower && low > _maskUpper) return false;
        return true;
    }

    private static uint IpV4ToUInt(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        return (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);
    }

    private static (ulong high, ulong low) IpV6ToUInt64(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        ulong high = (ulong)bytes[0] << 56 | (ulong)bytes[1] << 48 | (ulong)bytes[2] << 40 | (ulong)bytes[3] << 32 |
                     (ulong)bytes[4] << 24 | (ulong)bytes[5] << 16 | (ulong)bytes[6] << 8  | bytes[7];
        ulong low = (ulong)bytes[8] << 56 | (ulong)bytes[9] << 48 | (ulong)bytes[10] << 40 | (ulong)bytes[11] << 32 |
                    (ulong)bytes[12] << 24 | (ulong)bytes[13] << 16 | (ulong)bytes[14] << 8 | bytes[15];
        return (high, low);
    }
}