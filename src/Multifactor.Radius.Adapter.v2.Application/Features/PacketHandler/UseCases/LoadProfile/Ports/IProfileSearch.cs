using System.DirectoryServices.Protocols;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile.Ports;

public interface IProfileSearch
{
    ILdapProfile? Execute(FindUserDto request);

    /// <summary>
    /// Возвращает ВСЕ записи, подошедшие под фильтр поиска, а не только первую.
    /// Нужен при поиске через Global Catalog, где sAMAccountName формально уникален
    /// только в рамках домена (не леса).
    /// </summary>
    IReadOnlyList<ILdapProfile> ExecuteMany(FindUserDto request);

    /// <summary>
    /// Резолвит NetBIOS-имя домена в его DNS-имя
    /// Нужен, чтобы явно указанный пользователем домен в DOMAIN\user не терялся при поиске
    /// через Global Catalog. Возвращает null, если сопоставление не найдено.
    /// </summary>
    string? ResolveDomainDnsNameByNetBiosName(
        string connectionString,
        AuthType authType,
        string userName,
        string password,
        int bindTimeoutInSeconds,
        string netBiosName);
}

