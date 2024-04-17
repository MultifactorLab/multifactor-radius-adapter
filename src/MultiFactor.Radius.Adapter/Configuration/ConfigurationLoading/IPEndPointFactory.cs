//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System;
using System.Net;

namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading
{
    public static class IPEndPointFactory
    {
        public static bool TryParse(string text, out IPEndPoint ipEndPoint)
        {
            ipEndPoint = null;

            if (Uri.TryCreate(string.Concat("tcp://", text), UriKind.Absolute, out Uri uri))
            {
                if (!IPAddress.TryParse(uri.Host, out var parsed)) return false;

                ipEndPoint = new IPEndPoint(parsed, uri.Port < 0 ? 0 : uri.Port);
                return true;
            }

            if (Uri.TryCreate(string.Concat("tcp://", string.Concat("[", text, "]")), UriKind.Absolute, out uri))
            {
                if (!IPAddress.TryParse(uri.Host, out var parsed)) return false;

                ipEndPoint = new IPEndPoint(parsed, uri.Port < 0 ? 0 : uri.Port);
                return true;
            }

            return false;
        }
    }
}