using Multifactor.Radius.Adapter.v2.Application.Core.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Core.Models;

public sealed record Privacy(PrivacyMode PrivacyMode, string[] PrivacyFields);