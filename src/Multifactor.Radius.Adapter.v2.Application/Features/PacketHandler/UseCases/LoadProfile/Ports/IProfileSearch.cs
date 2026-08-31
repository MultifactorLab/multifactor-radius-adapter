using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile.Ports;

public interface IProfileSearch
{
    /// <summary>
    /// Ищет профиль пользователя и возвращает результат поиска <see cref="FindUserResult"/>.
    /// </summary>
    /// <param name="request">Поисковый запрос</param>
    /// <returns>Результат поиска</returns>
    FindUserResult Execute(FindUserDto request);
}

public record FindUserResult
{
    /// <summary>
    /// Профиль найден
    /// </summary>
    /// <param name="Profile">Профиль пользователя</param>
    /// <param name="BindConnectionString">
    /// Connection-string, к которому нужно обращаться для bind этого пользователя.
    /// </param>
    public sealed record Found(ILdapProfile Profile, string BindConnectionString) : FindUserResult;

    /// <summary>
    /// Профиль не найден
    /// </summary>
    /// <param name="Scope"> Область, в которой выполнялся поиск</param>
    public sealed record NotFound(ProfileSearchScope Scope) : FindUserResult;
}

