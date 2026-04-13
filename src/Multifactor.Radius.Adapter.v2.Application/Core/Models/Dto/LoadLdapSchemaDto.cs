using System.DirectoryServices.Protocols;

namespace Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;

public sealed record LoadLdapSchemaDto(string ConnectionString,
    string UserName,
    string Password,
    int BindTimeoutInSeconds,
    AuthType AuthType = AuthType.Basic);