using System.Net;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.SecondFactor;

internal static class ApiErrorPolicy
{
    public static bool ShouldDenyRequest(HttpStatusCode? status, out string? reason)
    {
        switch (status)
        {
            case HttpStatusCode.Unauthorized:
                reason = "Unauthorized";
                return true;
            case HttpStatusCode.Forbidden:
                reason = "Forbidden";
                return true;
            case HttpStatusCode.TooManyRequests:
                reason = "Too Many Requests";
                return true;
            default:
                reason = null;
                return false;
        }
    }
}
