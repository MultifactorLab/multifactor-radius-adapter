using System.Net;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Multifactor;

internal static class WebProxyFactory
{
    public static bool TryCreateWebProxy(Uri? proxyAddress, out WebProxy? proxy)
    {
        if (proxyAddress is null)
        {
            proxy = null;
            return false;
        }

        proxy = new WebProxy(proxyAddress);
        SetProxyCredentials(proxy, proxyAddress);
        return true;
    }

    private static void SetProxyCredentials(WebProxy proxy, Uri proxyUri)
    {
        if (string.IsNullOrWhiteSpace(proxyUri.UserInfo))
            return;

        var credentials = proxyUri.UserInfo.Split([':'], 2);
        proxy.Credentials = new NetworkCredential(credentials[0], credentials[1]);
    }
}