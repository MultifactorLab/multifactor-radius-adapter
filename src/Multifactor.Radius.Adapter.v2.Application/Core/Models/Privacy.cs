using Multifactor.Radius.Adapter.v2.Application.Core.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Core.Models
{
    public record Privacy(PrivacyMode PrivacyMode, string[] PrivacyFields);
}
