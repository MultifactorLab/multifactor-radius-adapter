namespace Multifactor.Radius.Adapter.v2.Domain.Auth;

public class AuthenticationState
{
    public AuthenticationStatus FirstFactorStatus { get; set; }
    public AuthenticationStatus SecondFactorStatus { get; set; }
}