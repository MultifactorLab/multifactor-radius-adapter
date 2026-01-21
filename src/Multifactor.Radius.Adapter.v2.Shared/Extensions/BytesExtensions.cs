namespace Multifactor.Radius.Adapter.v2.Shared.Extensions;

public static class BytesExtensions
{
    public static string? ToBase64(this byte[]? bytes)
    {
        return bytes != null ? Convert.ToBase64String(bytes) : null;
    }
}