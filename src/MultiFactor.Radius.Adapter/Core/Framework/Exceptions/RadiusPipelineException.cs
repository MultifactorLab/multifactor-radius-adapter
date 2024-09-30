using System;

namespace MultiFactor.Radius.Adapter.Core.Framework.Exceptions;

[Serializable]
internal class RadiusPipelineException : Exception
{
    public RadiusPipelineException(string message) : base(message) { }

    public RadiusPipelineException(string message, Exception inner) : base(message, inner) { }

    protected RadiusPipelineException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}