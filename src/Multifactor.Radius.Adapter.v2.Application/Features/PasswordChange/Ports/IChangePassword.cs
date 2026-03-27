using Multifactor.Radius.Adapter.v2.Application.Features.PasswordChange.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PasswordChange.Ports;

public interface IChangePassword
{
    bool Execute(ChangeUserPasswordDto request);
}