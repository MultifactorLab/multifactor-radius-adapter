using System.Text.Json.Serialization;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Multifactor.Models.Enum;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequestStatus
{
    AwaitingAuthentication,
    Granted,
    Denied
}