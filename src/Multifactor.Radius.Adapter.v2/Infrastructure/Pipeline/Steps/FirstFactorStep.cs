using Multifactor.Core.Ldap.LangFeatures;
using Multifactor.Radius.Adapter.v2.Core.FirstFactor;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

public class FirstFactorStep : IRadiusPipelineStep
{
    private readonly IFirstFactorProcessorProvider _firstFactorProcessor;
    public FirstFactorStep(IFirstFactorProcessorProvider processorProvider)
    {
        _firstFactorProcessor = processorProvider;
    }

    public async Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        Throw.IfNull(context, nameof(context));
        var processor = _firstFactorProcessor.GetProcessor(context.Settings.FirstFactorAuthenticationSource);
        await processor.ProcessFirstFactor(context);
    }
}