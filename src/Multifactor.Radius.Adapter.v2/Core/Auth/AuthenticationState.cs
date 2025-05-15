namespace Multifactor.Radius.Adapter.v2.Core.Auth;

public class AuthenticationState : IAuthenticationState
{
    public AuthenticationStatus FirstFactorStatus { get; set; }
    public AuthenticationStatus SecondFactorStatus { get; set; }
}