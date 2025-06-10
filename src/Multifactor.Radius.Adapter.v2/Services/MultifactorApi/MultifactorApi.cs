using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Core.MultifactorApi;
using Multifactor.Radius.Adapter.v2.Exceptions;
using Multifactor.Radius.Adapter.v2.Infrastructure.Http;

namespace Multifactor.Radius.Adapter.v2.Services.MultifactorApi;

public class MultifactorApi : IMultifactorApi
{
    private readonly ILogger<MultifactorApi> _logger;
    private readonly IHttpClient _httpClient;

    public MultifactorApi(IHttpClient httpClient, ILogger<MultifactorApi> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient, nameof(httpClient));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _logger = logger;
        _httpClient = httpClient;
    }

    public Task<AccessRequestResponse> CreateAccessRequest(AccessRequest payload, ApiCredential apiCredentials)
    {
        ArgumentNullException.ThrowIfNull(payload, nameof(payload));
        ArgumentNullException.ThrowIfNull(apiCredentials, nameof(apiCredentials));

        return SendRequestAsync("access/requests/ra", payload, apiCredentials);
    }

    public Task<AccessRequestResponse> SendChallengeAsync(ChallengeRequest payload, ApiCredential apiCredentials)
    {
        ArgumentNullException.ThrowIfNull(payload, nameof(payload));
        ArgumentNullException.ThrowIfNull(apiCredentials, nameof(apiCredentials));

        return SendRequestAsync("access/requests/ra/challenge", payload, apiCredentials);
    }

    private async Task<AccessRequestResponse> SendRequestAsync(string url, object payload, ApiCredential credentials)
    {
        var trace = $"rds-{Guid.NewGuid()}";
        using var scope = _logger.BeginScope(new Dictionary<string, object>(1) { { "mf-trace-id", trace } });
        var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{credentials.Usr}:{credentials.Pwd}"));
        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Basic {auth}" },
            { "mf-trace-id", trace }
        };

        try
        {
            return await SendAsync(url, payload, headers);
        }
        catch (HttpRequestException ex)
        {
            return ProcessHttpRequestException(ex, url);
        }
        catch (TaskCanceledException tce)
        {
            throw new MultifactorApiUnreachableException(
                $"Multifactor API host unreachable: {url}. Reason: Http request timeout", tce);
        }
        catch (Exception ex)
        {
            throw new MultifactorApiUnreachableException(
                $"Multifactor API host unreachable: {url}. Reason: {ex.Message}", ex);
        }
    }

    private async Task<AccessRequestResponse> SendAsync(string url, object payload, Dictionary<string, string> headers)
    {
        var response = await _httpClient.PostAsync<MultiFactorApiResponse<AccessRequestResponse>>(url, payload, headers);
            
        if (response is null)
            return new AccessRequestResponse()
            {
                Status = RequestStatus.Denied,
                ReplyMessage = "Empty response",
            };
            
        if (!response.Success)
        {
            _logger.LogWarning("Got unsuccessful response from API: {@response}", response);
        }

        return response.Model;
    }

    private AccessRequestResponse ProcessHttpRequestException(HttpRequestException ex, string url)
    {
        if (ex.StatusCode != HttpStatusCode.TooManyRequests)
        {
            throw new MultifactorApiUnreachableException(
                $"Multifactor API host unreachable: {url}. Reason: {ex.Message}", ex);
        }

        _logger.LogWarning("Unsuccessful api response: '{message:l}'", ex.Message);
        return new AccessRequestResponse()
        {
            Status = RequestStatus.Denied,
            ReplyMessage = "Too Many Requests"
        };
    }
}