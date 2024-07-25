using MultiFactor.Radius.Adapter.Infrastructure.Configuration.Models;
using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration.Exceptions;

/// <summary>
/// The Radius adapter configuration is invalid.
/// </summary>
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

    /// <summary>
    /// Returns new <see cref="InvalidConfigurationException"/> for the specified property of a <see cref="RadiusAdapterConfiguration"/> type. You can use a formatted string to pass the property name. 
    /// <br/> If a <see cref="DescriptionAttribute"/> attribute is found for the specified property, its value will be passed to the formatted string as argument {prop}.
    /// <br/> Otherwise, the real property name will be passed.
    /// <br/> Example: 
    /// <code>
    /// 
    /// class RadiusAdapterConfiguration
    /// {
    ///     [Description("some-property")]
    ///     public string SomeProperty { get; init; }
    ///     
    ///     public int SomeOtherProperty { get; init; }
    /// }
    /// 
    /// // InvalidConfigurationException with message "Element 'some-property' not found. Please check configuration file.";
    /// InvalidConfigurationException.ThrowFor(x => x.SomeProperty, "Element '{prop}' not found. Please check configuration file.");
    /// 
    /// // InvalidConfigurationException with message "Element 'SomeOtherProperty' has invalid value";
    /// InvalidConfigurationException.ThrowFor(x => x.SomeOtherProperty, "Element '{prop}' has invalid value.");
    /// 
    /// </code>
    /// </summary>
    /// <typeparam name="TProp">Property type of a <see cref="RadiusAdapterConfiguration"/> type.</typeparam>
    /// <param name="propertySelector">Property selector.</param>
    /// <param name="formattedMessage">
    /// Formatted message that will be passed to exception. Use pattern {prop} to pass the property name.
    /// <br/> You can also use wildcards like {0}, {1}, {n} to replace it with arguments (like <see cref="string.Format"/> method).
    /// </param>
    /// <param name="args">Items to format message.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="propertySelector"/> is null.</exception>
    /// <exception cref="ArgumentException">If <paramref name="formattedMessage"/> is null, empty or whitespace.</exception>
    public static InvalidConfigurationException For<TProp>(Expression<Func<RadiusAdapterConfiguration, TProp>> propertySelector, 
        string formattedMessage,
        params object[] args)
    {
        if (propertySelector is null)
        {
            throw new ArgumentNullException(nameof(propertySelector));
        }

        if (string.IsNullOrWhiteSpace(formattedMessage))
        {
            throw new ArgumentException($"'{nameof(formattedMessage)}' cannot be null or whitespace.", nameof(formattedMessage));
        }

        var propertyName = RadiusAdapterConfigurationDescription.Property(propertySelector);

        formattedMessage = formattedMessage.Replace("{prop}", propertyName);
        formattedMessage = string.Format(formattedMessage, args);

        return new InvalidConfigurationException(formattedMessage);
    }
}
