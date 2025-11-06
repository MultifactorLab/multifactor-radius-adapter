namespace Multifactor.Radius.Adapter.v2.Core.Pipeline;

public interface IExecutionState
{
    public bool IsTerminated { get; }
    public bool ShouldSkipResponse { get; }

    public void Terminate();

    public void SkipResponse();
}