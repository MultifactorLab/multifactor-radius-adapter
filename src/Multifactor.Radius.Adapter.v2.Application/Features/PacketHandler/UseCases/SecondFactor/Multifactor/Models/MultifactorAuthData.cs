namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models;

public sealed class MultifactorAuthData
{
    public string ApiKey { get; set; }
    public string ApiSecret { get; set; }

    public MultifactorAuthData(string apiKey, string apiSecret)
    {
        ArgumentNullException.ThrowIfNull(apiKey);
        ArgumentNullException.ThrowIfNull(apiSecret);
        ApiKey = apiKey;
        ApiSecret = apiSecret;
    }
}