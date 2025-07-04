using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core.Auth;

namespace Multifactor.Radius.Adapter.v2.Services.AuthenticatedClientCache;

    public class AuthenticatedClientCache : IAuthenticatedClientCache
    {
        // TODO ConcurrentDictionary -> MemoryCache
        private readonly ConcurrentDictionary<string, AuthenticatedClient> _authenticatedClients = new();
        private readonly ILogger _logger;

        public AuthenticatedClientCache(ILogger<AuthenticatedClientCache> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);
            _logger = logger;
        }

        public bool TryHitCache(string? callingStationId, string userName, string clientName, AuthenticatedClientCacheConfig cacheConfig)
        {
            ArgumentNullException.ThrowIfNull(cacheConfig);
            ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
            
            if (!cacheConfig.Enabled)
                return false;

            if (!cacheConfig.MinimalMatching && string.IsNullOrWhiteSpace(callingStationId))
            {
                _logger.LogWarning("Remote host parameter miss for user {userName:l}", userName);
                return false;
            }

            var id = AuthenticatedClient.ParseId(callingStationId, clientName, userName);
            if (!_authenticatedClients.TryGetValue(id, out var authenticatedClient))
                return false;

            _logger.LogDebug($"User {userName} with calling-station-id {callingStationId} authenticated {authenticatedClient.Elapsed:hh\\:mm\\:ss} ago. Authentication session period: {cacheConfig.Lifetime}");

            if (authenticatedClient.Elapsed <= cacheConfig.Lifetime)
                return true;

            _authenticatedClients.TryRemove(id, out _);

            return false;
        }

        public void SetCache(string? callingStationId, string? userName, string clientName, AuthenticatedClientCacheConfig cacheConfig)
        {
            ArgumentNullException.ThrowIfNull(cacheConfig);
            ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
            
            if (!cacheConfig.Enabled || !cacheConfig.MinimalMatching && string.IsNullOrWhiteSpace(callingStationId))
                return;

            var client = AuthenticatedClient.Create(callingStationId, clientName, userName);
            if (!_authenticatedClients.ContainsKey(client.Id))
                _authenticatedClients.TryAdd(client.Id, client);
        }
    }