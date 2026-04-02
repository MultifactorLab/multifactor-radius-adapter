using System.DirectoryServices.Protocols;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Models;

public sealed record CheckConnectionDto(string ConnectionString,
    string UserName,
    string Password,
    int BindTimeoutInSeconds,
    AuthType AuthType = AuthType.Basic);