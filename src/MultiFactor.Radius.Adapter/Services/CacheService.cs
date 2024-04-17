using Microsoft.Extensions.Caching.Memory;
using MultiFactor.Radius.Adapter.Core.Radius;
using System;
using System.Net;

namespace MultiFactor.Radius.Adapter.Services
{
    public class CacheService
    {
        private readonly IMemoryCache _cache;

        public CacheService(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Check is packet was retransmissed (duplicated)
        /// </summary>
        public bool IsRetransmission(IRadiusPacket requestPacket, IPEndPoint remoteEndpoint)
        {
            //unique key is combination of packet code, identifier, client endpoint, user name and request authenticator 

            var uniqueKey = requestPacket.CreateUniqueKey(remoteEndpoint);

            if (_cache.TryGetValue(uniqueKey, out _))
            {
                return true;
            }

            _cache.Set(uniqueKey, "1", DateTimeOffset.UtcNow.AddMinutes(1));

            return false;
        }

    }
}
