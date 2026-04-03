using System.Net;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Integrations.Multifactor;

internal static class WebProxyFactory
{
    public static bool TryCreateWebProxy(string proxyAddress, out WebProxy? proxy)
    {
        if (string.IsNullOrWhiteSpace(proxyAddress) || !TryParseUri(proxyAddress, out var proxyUri))
        {
            proxy = null;
            return false;
        }

        proxy = new WebProxy(proxyUri);
        SetProxyCredentials(proxy, proxyUri);
        return true;
    }

    private static bool TryParseUri(string apiUri, out Uri uri)
    {
        if (Uri.TryCreate(apiUri, UriKind.Absolute, out uri!))
            return true;
        var uriSeparatorIdx = apiUri.LastIndexOf('@');
        if (uriSeparatorIdx == -1)
            return false;
        
        var leftPart = apiUri[..uriSeparatorIdx].Replace("@", "%40");
        var rightPart = apiUri[(uriSeparatorIdx + 1)..];
        var escapedUri = $"{leftPart}@{rightPart}";
        uri = new Uri(escapedUri);
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