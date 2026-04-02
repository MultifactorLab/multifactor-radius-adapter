using System.Text.Json.Serialization;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.SecondFactor.Multifactor.Models.Enum;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestStatus
{
    AwaitingAuthentication,
    Granted,
    Denied
}