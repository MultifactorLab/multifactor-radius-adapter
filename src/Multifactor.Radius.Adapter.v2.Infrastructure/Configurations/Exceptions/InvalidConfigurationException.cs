namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;

/// <summary>
/// The Radius adapter configuration is invalid.
/// </summary>
public class InvalidConfigurationException : Exception
{
    public InvalidConfigurationException(string message)
        : base($"Configuration error: {message}") { }

    public InvalidConfigurationException(string message, Exception inner)
        : base($"Configuration error: {message}", inner) { }
}
