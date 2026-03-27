namespace Multifactor.Radius.Adapter.v2.Application.Features.FirstFactor.Models;

public sealed record CheckConnectionDto(string ConnectionString,
    string UserName,
    string Password,
    int BindTimeoutInSeconds);