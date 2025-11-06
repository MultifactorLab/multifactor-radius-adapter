namespace Multifactor.Radius.Adapter.v2.Core.Pipeline;

public class ExecutionState : IExecutionState
{
    private bool _isTerminated;
    private bool _shouldSkip;

    public bool IsTerminated => _isTerminated;
    public bool ShouldSkipResponse => _shouldSkip;

    public void Terminate()
    {
        _isTerminated = true;
    }
    
    public void SkipResponse()
    {
        _shouldSkip = true;
    }
}