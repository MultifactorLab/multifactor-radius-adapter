using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Configuration.Core;
using System;
using System.Collections.Concurrent;

namespace MultiFactor.Radius.Adapter.Services
{
    public class AuthenticatedClientCache : IAuthenticatedClientCache
    {
        private static readonly ConcurrentDictionary<string, AuthenticatedClient> _authenticatedClients = new();
        private readonly ILogger _logger;

        public AuthenticatedClientCache(ILogger<AuthenticatedClientCache> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool TryHitCache(string callingStationId, string userName, IClientConfiguration clientConfiguration)
        {
            if (!clientConfiguration.AuthenticationCacheLifetime.Enabled) return false;

            if (!clientConfiguration.AuthenticationCacheLifetime.MinimalMatching && string.IsNullOrEmpty(callingStationId))
            {
                _logger.LogWarning("Remote host parameter miss for user {userName:l}", userName);
                return false;
            }

            var id = AuthenticatedClient.ParseId(callingStationId, clientConfiguration.Name, userName);
            if (!_authenticatedClients.TryGetValue(id, out var authenticatedClient))
            {
                return false;
            }

            _logger.LogDebug($"User {userName} with calling-station-id {callingStationId} authenticated {authenticatedClient.Elapsed:hh\\:mm\\:ss} ago. Authentication session period: {clientConfiguration.AuthenticationCacheLifetime.Lifetime}");

            if (authenticatedClient.Elapsed <= clientConfiguration.AuthenticationCacheLifetime.Lifetime)
            {
                return true;
            }

            _authenticatedClients.TryRemove(id, out _);

            return false;
        }

        public void SetCache(string callingStationId, string userName, IClientConfiguration clientConfiguration)
        {
            if (!clientConfiguration.AuthenticationCacheLifetime.Enabled ||
                !clientConfiguration.AuthenticationCacheLifetime.MinimalMatching && string.IsNullOrEmpty(callingStationId)) return;

            var client = AuthenticatedClient.Create(callingStationId, clientConfiguration.Name, userName);
            if (!_authenticatedClients.ContainsKey(client.Id))
            {
                _authenticatedClients.TryAdd(client.Id, client);
            }
        }
    }
}
