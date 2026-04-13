namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadLdapForest.Models;

public sealed record LoadMetadataDto(
    string ConnectionString,
    string UserName,
    string Password,
    int BindTimeoutInSeconds,
    bool AlternativeSuffixesEnabled);