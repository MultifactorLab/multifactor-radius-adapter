using Multifactor.Radius.Adapter.v2.Application.Core.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.FirstFactor.Processor;

public interface IFirstFactorProcessorProvider
{
    IFirstFactorProcessor GetProcessor(AuthenticationSource authSource);
}

internal sealed class FirstFactorProcessorProvider : IFirstFactorProcessorProvider
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