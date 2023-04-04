using System;

namespace MultiFactor.Radius.Adapter.Core.Exceptions
{
    [Serializable]
    internal class InvalidConfigurationException : Exception
    {
        public InvalidConfigurationException(string message)
            : base($"Configuration error: {message}") { }

        public InvalidConfigurationException(string message, Exception inner)
            : base($"Configuration error: {message}", inner) { }

        protected InvalidConfigurationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
