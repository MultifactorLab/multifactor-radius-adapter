using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Ports;
using Multifactor.Radius.Adapter.v2.Application.Models;
using Multifactor.Radius.Adapter.v2.Application.Ports;
using Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Multifactor.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Multifactor;

public class MultifactorApi : IMultifactorApi
{
    private const string _clientName = "multifactor-api";
    private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<MultifactorApi> _logger;
    public MultifactorApi(IHttpClientFactory clientFactory, 
        ILogger<MultifactorApi> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<AccessRequestResponse> CreateAccessRequest(AccessRequestQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));
        
        var dto = AccessRequestDto.FromQuery(query);
        var client = _clientFactory.CreateClient(_clientName);
        var response = await client.PostAsJsonAsync("access/requests/ra", dto, cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Multifactor API request was unsuccessful. Method: access/requests/ra. Reason: {error:l}", errContent);
            throw new Exception("Error while requesting access/requests/ra");
        }
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var accessResponse = JsonSerializer.Deserialize<MultiFactorApiResponse<AccessRequestResponse>>(content, _options);
        return accessResponse.Model;
    }
    
    public async Task<AccessRequestResponse> SendChallengeAsync(ChallengeRequestQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));
        
        var dto = ChallengeRequestDto.FromQuery(query);
        var client = _clientFactory.CreateClient(_clientName);
        var response = await client.PostAsJsonAsync("access/requests/ra/challenge", dto, cancellationToken: cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Multifactor API request was unsuccessful. Method: access/requests/ra/challenge: {error:l}", errContent);
            throw new Exception("Error while requesting access/requests/ra/challenge");
        }
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var challengeResponse = JsonSerializer.Deserialize<MultiFactorApiResponse<AccessRequestResponse>>(content, _options);
        return challengeResponse.Model;
    }
    
}