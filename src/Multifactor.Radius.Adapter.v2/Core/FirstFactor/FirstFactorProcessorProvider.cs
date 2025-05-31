using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Core.Auth;

namespace Multifactor.Radius.Adapter.v2.Core.FirstFactor;

public class FirstFactorProcessorProvider : IFirstFactorProcessorProvider
{
    private readonly IEnumerable<IFirstFactorProcessor> _firstFactorProcessors;

    public FirstFactorProcessorProvider(IEnumerable<IFirstFactorProcessor> processors)
    {
        Throw.IfNull(processors, nameof(processors));
        _firstFactorProcessors = processors;
    }

    public IFirstFactorProcessor GetProcessor(AuthenticationSource authSource)
    {
        var processor = _firstFactorProcessors.FirstOrDefault(processor => processor.AuthenticationSource == authSource);
        if (processor == null)
            throw new ArgumentException($"No processor found for authentication source {authSource}");
        return processor;
    }
}