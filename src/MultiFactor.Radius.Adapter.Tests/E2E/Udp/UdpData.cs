using System.Text;

namespace MultiFactor.Radius.Adapter.Tests.E2E.Udp;

public record UdpData
{
    private readonly Memory<byte> _memory;
    private byte[]? _bytes;
    private string? _string;

    public UdpData(Memory<byte> bytes)
    {
        _memory = bytes;
    }

    public byte[] GetBytes()
    {
        return _bytes ??= _memory.ToArray();
    }

    public string GetString()
    {
        return _string ??= Encoding.ASCII.GetString(GetBytes());
    }
}