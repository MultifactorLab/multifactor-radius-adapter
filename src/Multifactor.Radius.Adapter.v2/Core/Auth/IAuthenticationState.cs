namespace Multifactor.Radius.Adapter.v2.Core.Auth;

public interface IAuthenticationState
{
    public AuthenticationStatus FirstFactorStatus { get; set; }
    
    public AuthenticationStatus SecondFactorStatus { get; set; }
}