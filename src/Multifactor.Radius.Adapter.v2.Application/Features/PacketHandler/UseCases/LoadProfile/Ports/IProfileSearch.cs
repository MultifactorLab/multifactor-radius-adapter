using Multifactor.Radius.Adapter.v2.Application.Core.Models.Abstractions;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.LoadProfile.Ports;

public interface IProfileSearch
{
    /// <summary>
    /// Ищет профиль пользователя (обычный поиск по одному домену,
    /// поиск через Global Catalog со снятием неоднозначности, повторное чтение полного
    /// профиля с найденного DC).
    /// </summary>
    FindUserResult Execute(FindUserDto request);
}

public record FindUserResult
{
    /// <summary>
    /// Профиль найден и однозначно определён.
    /// </summary>
    /// <param name="Profile">Профиль пользователя (полный, а не частично реплицированный).</param>
    /// <param name="BindConnectionString">
    /// Connection-string, к которому нужно обращаться для bind этого пользователя.
    /// </param>
    public sealed record Found(ILdapProfile Profile, string BindConnectionString) : FindUserResult;

    /// <summary>
    /// Профиль не найден (или найден неоднозначно и не смог быть уточнён).
    /// </summary>
    /// <param name="IsFinal">
    /// true — результат окончательный - сразу отказываем в аутентификации.<br/>
    /// false — можно попробовать следующий настроенный LDAP-сервер.
    /// </param>
    public sealed record NotFound(bool IsFinal) : FindUserResult;
}

