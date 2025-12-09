namespace Multifactor.Radius.Adapter.v2.Domain.Pipeline;

public class ExecutionState
{
    public bool IsTerminated { get; private set; }

    public bool ShouldSkipResponse { get; private set; }

    public void Terminate()
    {
        IsTerminated = true;
    }
    
    public void SkipResponse()
    {
        ShouldSkipResponse = true;
    }
}