namespace Multifactor.Radius.Adapter.v2.Application.Core.Enum;

/// <summary>
/// Область, в которой выполнялся поиск профиля.
/// </summary>
public enum ProfileSearchScope
{
    /// <summary>
    /// Поиск в рамках одного домена. Если не найден — можно попробовать следующий настроенный LDAP-сервер.
    /// </summary>
    Domain,

    /// <summary>
    /// Поиск по всему лесу через Global Catalog. Если не найден — дальше пробовать нечего.
    /// </summary>
    Forest
}