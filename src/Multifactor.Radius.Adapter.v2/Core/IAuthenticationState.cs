namespace Multifactor.Radius.Adapter.v2.Core;

public interface IAuthenticationState
{
    public AuthenticationStatus FirstFactorStatus { get; }
    
    public AuthenticationStatus SecondFactorStatus { get; }
}