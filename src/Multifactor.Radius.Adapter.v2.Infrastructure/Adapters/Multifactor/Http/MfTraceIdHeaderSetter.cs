namespace Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Multifactor.Http;

public class MfTraceIdHeaderSetter : DelegatingHandler//TODO TELEMETRY
{
    private const string _key = "mf-trace-id";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var trace = $"rds-{ActivityContext.Current.ActivityId}";
        if (!request.Headers.Contains(_key))
        {
            request.Headers.Add(_key, trace);
        }

        var resp = await base.SendAsync(request, cancellationToken);
        if (!resp.Headers.Contains(_key))
        {
            resp.Headers.Add(_key, trace);
        }

        return resp;
    }
}
