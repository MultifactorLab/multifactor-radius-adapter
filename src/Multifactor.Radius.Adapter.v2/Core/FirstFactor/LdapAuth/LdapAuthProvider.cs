using Multifactor.Radius.Adapter.v2.Core.Radius;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor.LdapAuth;

public class LdapAuthProvider : ILdapAuthProvider
{
    private readonly IEnumerable<ILdapAuthProcessor> _processors;
    
    public LdapAuthProvider(IEnumerable<ILdapAuthProcessor> authProcessors)
    {
        _processors = authProcessors;
    }

    public ILdapAuthProcessor? GetLdapAuthProcessor(AuthenticationType authenticationType)
    {
        return _processors.FirstOrDefault(processor => processor.AuthenticationType == authenticationType);
    }
}

public interface ILdapAuthProvider
{
    ILdapAuthProcessor? GetLdapAuthProcessor(AuthenticationType authenticationType);
}