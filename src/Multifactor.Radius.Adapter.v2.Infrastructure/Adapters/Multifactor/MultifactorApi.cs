using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Exceptions;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Ports;
using Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Multifactor.Http;
using Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Multifactor.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Multifactor;

public class MultifactorApi : IMultifactorApi
{
    private const string ClientName = "multifactor-api";
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<MultifactorApi> _logger;

    public MultifactorApi(
        IHttpClientFactory clientFactory, 
        ILogger<MultifactorApi> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<AccessRequestResponse> CreateAccessRequest(
        AccessRequestQuery query, 
        MultifactorAuthData authData, 
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));
        
        var dto = AccessRequestDto.FromQuery(query);
        return await SendRequestAsync<AccessRequestDto, AccessRequestResponse>(
            endpoint: "access/requests/ra",
            data: dto,
            authData: authData,
            cancellationToken: cancellationToken);
    }
    
    public async Task<AccessRequestResponse> SendChallengeAsync(
        ChallengeRequestQuery query, 
        MultifactorAuthData authData, 
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));
        
        var dto = ChallengeRequestDto.FromQuery(query);
        return await SendRequestAsync<ChallengeRequestDto, AccessRequestResponse>(
            endpoint: "access/requests/ra/challenge",
            data: dto,
            authData: authData,
            cancellationToken: cancellationToken);
    }
    
    private async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        string endpoint,
        TRequest data,
        MultifactorAuthData authData,
        CancellationToken cancellationToken)
        where TResponse : class, new()
    {
        using var client = CreateAuthenticatedClient(authData);
        
        try
        {
            var response = await client.PostAsJsonAsync(
                endpoint, 
                data, 
                _jsonOptions, 
                cancellationToken);
            
            return await ProcessResponseAsync<TResponse>(response, endpoint, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return ProcessHttpRequestException<TResponse>(ex, client.BaseAddress?.OriginalString);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Multifactor API timeout expired for endpoint: {Endpoint}", endpoint);
            return CreateDeniedResponse<TResponse>("Request timeout");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Multifactor API request was cancelled for endpoint: {Endpoint}", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            throw new MultifactorApiUnreachableException(
                $"Multifactor API host unreachable: {client.BaseAddress?.OriginalString}. " +
                $"Endpoint: {endpoint}. Reason: {ex.Message}", 
                ex);
        }
    }
    
    private HttpClient CreateAuthenticatedClient(MultifactorAuthData authData)
    {
        var client = _clientFactory.CreateClient(ClientName);
        var authHeader = new BasicAuthHeaderValue(authData.ApiKey, authData.ApiSecret);
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", 
            authHeader.GetBase64());
        
        return client;
    }
    
    private async Task<TResponse> ProcessResponseAsync<TResponse>(
        HttpResponseMessage response,
        string endpoint,
        CancellationToken cancellationToken)
        where TResponse : class, new()
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Multifactor API request failed. Endpoint: {Endpoint}, Status: {StatusCode}, Error: {Error}",
                endpoint, response.StatusCode, errorContent);
            
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return CreateDeniedResponse<TResponse>("Too Many Requests");
            }
            
            response.EnsureSuccessStatusCode();
        }
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<MultiFactorApiResponse<TResponse>>(
            content, 
            _jsonOptions);
        
        if (apiResponse is null)
        {
            _logger.LogWarning("Empty response from Multifactor API for endpoint: {Endpoint}", endpoint);
            return CreateDeniedResponse<TResponse>("Empty response");
        }
        
        if (!apiResponse.Success)
        {
            _logger.LogWarning(
                "Unsuccessful response from Multifactor API. Endpoint: {Endpoint}, Response: {@Response}",
                endpoint, apiResponse);
        }
        
        return apiResponse.Model ?? CreateDeniedResponse<TResponse>("Response model is null");
    }
    
    private static TResponse CreateDeniedResponse<TResponse>(string? message = null)
        where TResponse : class, new()
    {
        if (typeof(TResponse) == typeof(AccessRequestResponse))
        {
            return (TResponse)(object)new AccessRequestResponse
            {
                Status = RequestStatus.Denied,
                ReplyMessage = message
            };
        }
        
        return new TResponse();
    }
    
    private TResponse ProcessHttpRequestException<TResponse>(
        HttpRequestException ex, 
        string? url)
        where TResponse : class, new()
    {
        if (ex.StatusCode != HttpStatusCode.TooManyRequests)
        {
            throw new MultifactorApiUnreachableException(
                $"Multifactor API host unreachable: {url}. Reason: {ex.Message}", 
                ex);
        }

        _logger.LogWarning("Rate limit exceeded: {Message}", ex.Message);
        return CreateDeniedResponse<TResponse>("Too Many Requests");
    }
}