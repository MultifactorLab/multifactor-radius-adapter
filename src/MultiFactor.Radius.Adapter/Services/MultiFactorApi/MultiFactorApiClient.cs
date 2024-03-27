//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md


using LdapForNet.Adsddl;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Core;
using MultiFactor.Radius.Adapter.Core.Http;
using MultiFactor.Radius.Adapter.Core.Serialization;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace MultiFactor.Radius.Adapter.Services.MultiFactorApi
{
    /// <summary>
    /// Service to interact with multifactor web api
    /// </summary>
    internal class MultifactorApiClient : IMultifactorApiClient
    {
        private readonly IAuthenticatedClientCache _authenticatedClientCache;
        private ILogger<MultifactorApiClient> _logger;
        private readonly IHttpClientAdapter _httpClientAdapter;

        public MultifactorApiClient(IAuthenticatedClientCache authenticatedClientCache, ILogger<MultifactorApiClient> logger, IHttpClientAdapter httpClientAdapter)
        {
            _authenticatedClientCache = authenticatedClientCache ?? throw new ArgumentNullException(nameof(authenticatedClientCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientAdapter = httpClientAdapter ?? throw new ArgumentNullException(nameof(httpClientAdapter));
        }

        public Task<AccessRequestDto> CreateRequestAsync(CreateRequestDto dto, BasicAuthHeaderValue auth)
        {
            if (dto is null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (auth is null)
            {
                throw new ArgumentNullException(nameof(auth));
            }

            return SendRequest("access/requests/ra", dto, auth);
        }

        public Task<AccessRequestDto> ChallengeAsync(ChallengeDto dto, BasicAuthHeaderValue auth)
        {
            if (dto is null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (auth is null)
            {
                throw new ArgumentNullException(nameof(auth));
            }

            return SendRequest("access/requests/ra/challenge", dto, auth);
        }

        private async Task<AccessRequestDto> SendRequest(string url, object payload, BasicAuthHeaderValue auth)
        {
            var headers = new Dictionary<string, string>
            {
                {"Authorization", $"Basic {auth}" }
            };

            try
            {
                var response = await _httpClientAdapter.PostAsync<MultiFactorApiResponse<AccessRequestDto>>(url, payload, headers);
                if (!response.Success)
                {
                    _logger.LogWarning("Got unsuccessful response from API: {@response}", response);
                }

                return response.Model;
            }
            catch (TaskCanceledException tce)
            {
                throw new MultifactorApiUnreachableException($"Multifactor API host unreachable: {url}. Reason: Http request timeout", tce);
            }
            catch (Exception ex)
            {
                throw new MultifactorApiUnreachableException($"Multifactor API host unreachable: {url}. Reason: {ex.Message}", ex);
            }
        }
    }
}
