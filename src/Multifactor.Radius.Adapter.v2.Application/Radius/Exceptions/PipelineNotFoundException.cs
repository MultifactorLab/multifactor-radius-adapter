namespace Multifactor.Radius.Adapter.v2.Application.Radius.Exceptions;

public class PipelineNotFoundException : Exception
{
    public PipelineNotFoundException(string message) : base(message) { }
}