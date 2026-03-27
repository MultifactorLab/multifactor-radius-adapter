namespace Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Models;

public sealed record LoadMetadataDto(
    string ConnectionString,
    string UserName,
    string Password,
    int BindTimeoutInSeconds,
    bool AlternativeSuffixesEnabled);