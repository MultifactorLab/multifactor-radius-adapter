using Multifactor.Radius.Adapter.v2.Application.Configuration.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.FirstFactor;

public class FirstFactorProcessorProvider : IFirstFactorProcessorProvider
{
    private readonly IEnumerable<IFirstFactorProcessor> _firstFactorProcessors;

    public FirstFactorProcessorProvider(IEnumerable<IFirstFactorProcessor> processors)
    {
        ArgumentNullException.ThrowIfNull(processors);
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