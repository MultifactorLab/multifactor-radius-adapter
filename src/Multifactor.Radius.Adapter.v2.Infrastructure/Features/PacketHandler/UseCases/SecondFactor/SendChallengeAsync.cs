using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Exceptions;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models.Enum;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Ports;
using Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.SecondFactor.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.UseCases.SecondFactor;

internal sealed class SendChallenge: ISendChallenge
{
    private const string ClientName = "multifactor-api";
    private const string Url = "access/requests/ra/challenge";
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<ISendChallenge> _logger;
    
    public SendChallenge(
        IHttpClientFactory clientFactory, 
        ILogger<ISendChallenge> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }
    
    public async Task<AccessRequestResponse> Execute(ChallengeRequestDto dto, MultifactorAuthData authData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto, nameof(dto));
        //TODO dto
        using var client = CreateClient(authData);
        try
        {
            var response = await client.PostAsJsonAsync(Url,
                dto, _jsonOptions, cancellationToken);
            
            return await ProcessResponseAsync(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return ProcessHttpRequestException(ex, client.BaseAddress?.OriginalString);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Multifactor API timeout expired for endpoint: {Url}", Url);
            return CreateDeniedResponse("Request timeout");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Multifactor API request was cancelled for endpoint: {Url}", Url);
            throw;
        }
        catch (Exception ex)
        {
            throw new MultifactorApiUnreachableException(
                $"Multifactor API host unreachable: {client.BaseAddress?.OriginalString}. " +
                $"Endpoint: \"access/requests/ra\". Reason: {ex.Message}", 
                ex);
        }
    }
    
    private async Task<AccessRequestResponse> ProcessResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Multifactor API request failed. Endpoint: {Url}, Status: {StatusCode}, Error: {Error}",
                Url, response.StatusCode, errorContent);
            
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return CreateDeniedResponse("Too Many Requests");
            }
            
            response.EnsureSuccessStatusCode();
        }
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var apiResponse = JsonSerializer.Deserialize<MultiFactorApiResponse<AccessRequestResponse>>(
            content, 
            _jsonOptions);
        
        if (apiResponse is null)
        {
            _logger.LogWarning("Empty response from Multifactor API for endpoint: {Url}", Url);
            return CreateDeniedResponse("Empty response");
        }
        
        if (!apiResponse.Success)
        {
            _logger.LogWarning(
                "Unsuccessful response from Multifactor API. Endpoint: {Url}, Response: {@Response}",
                Url, apiResponse);
        }
        
        return apiResponse.Model ?? CreateDeniedResponse("Response model is null");
    }
    
    private static AccessRequestResponse CreateDeniedResponse(string? message = null)
    {
        return new AccessRequestResponse
        {
            Status = RequestStatus.Denied,
            ReplyMessage = message
        };
    }
    
    private AccessRequestResponse ProcessHttpRequestException(
        HttpRequestException ex, string? url)
    {
        if (ex.StatusCode != HttpStatusCode.TooManyRequests)
        {
            throw new MultifactorApiUnreachableException(
                $"Multifactor API host unreachable: {url}. Reason: {ex.Message}", 
                ex);
        }

        _logger.LogWarning("Rate limit exceeded: {Message}", ex.Message);
        return CreateDeniedResponse("Too Many Requests");
    }
    
    private HttpClient CreateClient(MultifactorAuthData authData)
    {
        var client = _clientFactory.CreateClient(ClientName);
        var authHeader = new BasicAuthHeaderValue(authData.ApiKey, authData.ApiSecret);
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", 
            authHeader.GetBase64());
        
        return client;
    }
}