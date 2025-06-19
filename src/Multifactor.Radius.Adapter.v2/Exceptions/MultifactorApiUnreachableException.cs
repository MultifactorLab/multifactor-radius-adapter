namespace Multifactor.Radius.Adapter.v2.Exceptions;

[Serializable]
public class MultifactorApiUnreachableException : Exception
{
    public MultifactorApiUnreachableException() { }
    public MultifactorApiUnreachableException(string message) : base(message) { }
    public MultifactorApiUnreachableException(string message, Exception inner) : base(message, inner) { }
    protected MultifactorApiUnreachableException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}