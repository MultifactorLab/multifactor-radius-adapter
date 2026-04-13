namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.Exceptions;

public sealed class PipelineNotFoundException : Exception
{
    public PipelineNotFoundException(string message) : base(message) { }
}