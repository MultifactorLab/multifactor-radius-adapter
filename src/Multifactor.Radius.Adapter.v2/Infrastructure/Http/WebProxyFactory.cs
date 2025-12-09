using System.Net;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Http;

public static class WebProxyFactory
{
    public static bool TryCreateProxy(string address, out IWebProxy? proxy)
    {
        proxy = null;

        if (string.IsNullOrWhiteSpace(address))
            return false;

        if (!TryParseUri(address, out var uri))
            return false;

        proxy = CreateProxy(uri);
        return true;
    }

    private static IWebProxy CreateProxy(Uri uri)
    {
        var proxy = new WebProxy(uri);

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var credentials = ParseCredentials(uri.UserInfo);
            proxy.Credentials = credentials;
        }

        return proxy;
    }

    private static bool TryParseUri(string address, out Uri uri)
    {
        if (Uri.TryCreate(address, UriKind.Absolute, out uri))
            return true;

        return TryParseUriWithCredentials(address, out uri);
    }

    private static bool TryParseUriWithCredentials(string address, out Uri uri)
    {
        uri = null;

        var atIndex = address.LastIndexOf('@');
        if (atIndex == -1)
            return false;

        var beforeAt = address[..atIndex];
        var afterAt = address[(atIndex + 1)..];

        var escapedBeforeAt = beforeAt.Replace("@", "%40");
        var escapedUri = $"{escapedBeforeAt}@{afterAt}";

        return Uri.TryCreate(escapedUri, UriKind.Absolute, out uri);
    }

    private static NetworkCredential ParseCredentials(string userInfo)
    {
        var parts = userInfo.Split(':', 2);
        var username = parts[0];
        var password = parts.Length > 1 ? parts[1] : string.Empty;

        return new NetworkCredential(username, password);
    }
}