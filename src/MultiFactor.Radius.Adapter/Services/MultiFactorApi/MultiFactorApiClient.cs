//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md


using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Infrastructure.Http;
using MultiFactor.Radius.Adapter.Services.MultiFactorApi.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MultiFactor.Radius.Adapter.Services.MultiFactorApi
{
    /// <summary>
    /// Service to interact with multifactor web api
    /// </summary>
    internal class MultifactorApiClient : IMultifactorApiClient
    {
        private readonly ILogger<MultifactorApiClient> _logger;
        private readonly HttpClientAdapter _httpClientAdapter;

        public MultifactorApiClient(ILogger<MultifactorApiClient> logger, HttpClientAdapter httpClientAdapter)
        {
            _logger = logger;
            _httpClientAdapter = httpClientAdapter;
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
            var trace = $"rds-{Guid.NewGuid()}";
            using var scope = _logger.BeginScope(new Dictionary<string, object>(1) { { "mf-trace-id", trace } });
            var headers = new Dictionary<string, string>
            {
                {"Authorization", $"Basic {auth.GetBase64()}" },
                {"mf-trace-id", trace }
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
