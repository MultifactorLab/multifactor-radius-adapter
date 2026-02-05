using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Models;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Exceptions;

/// <summary>
/// The Radius adapter configuration is invalid.
/// </summary>
public class InvalidConfigurationException : Exception
{
    public InvalidConfigurationException(string message)
        : base($"Configuration error: {message}") { }
    public InvalidConfigurationException(string message, string fileName)
        : base($"Configuration error: {message}. Configuration file name: {fileName}") { }

    public InvalidConfigurationException(string message, Exception inner)
        : base($"Configuration error: {message}", inner) { }
    
    public static InvalidConfigurationException For<TProp>(Expression<Func<ConfigurationFile, TProp>> propertySelector, 
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

        var propertyName = Property(propertySelector);

        formattedMessage = formattedMessage.Replace("{prop}", propertyName);
        formattedMessage = string.Format(formattedMessage, args);

        return new InvalidConfigurationException(formattedMessage);
    }

    public static InvalidConfigurationException RequiredFor<TProp>(Expression<Func<ConfigurationFile, TProp>> propertySelector, string filePath)
    {
        const string message = "Property '{prop}' is required. Config name: '{1}'";
        return For(c => propertySelector, message, filePath); ;
    }
    
    private static string Property<TProp>(Expression<Func<ConfigurationFile, TProp>> propertySelector)
    {
        if (propertySelector is null)
        {
            throw new ArgumentNullException(nameof(propertySelector));
        }

        if (propertySelector.Body is not MemberExpression expression)
        {
            throw new InvalidOperationException("Only the class property should be selected");
        }

        if (expression.Member is not PropertyInfo property)
        {
            throw new InvalidOperationException("Only the class property should be selected");
        }

        var attribute = property.GetCustomAttribute<DescriptionAttribute>();
        if (attribute == null)
        {
            return property.Name;
        }

        var description = attribute.Description;
        if (string.IsNullOrWhiteSpace(description))
        {
            return property.Name;
        }

        return description;
    }
}
