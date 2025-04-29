using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.RadiusAdapter
{
    internal static class RadiusAdapterConfigurationDescription
    {
        /// <summary>
        /// Returns description of a Radius adapter configuration property. 
        /// <br/>If a <see cref="DescriptionAttribute"/> attribute is found for the specified property, its value will be returned.
        /// <br/> Otherwise, the real property name will be returned.
        /// </summary>
        /// <typeparam name="TProp">Property type.</typeparam>
        /// <param name="propertySelector">Property for which you need to get a name.</param>
        /// <returns>Description of a property</returns>
        /// <exception cref="ArgumentNullException">if <paramref name="configuration"/> is null or if <paramref name="propertySelector"/> is null.</exception>
        /// <exception cref="InvalidOperationException">If you are trying to access a member of a type that is not a property.</exception>
        public static string Property<TProp>(Expression<Func<RadiusAdapterConfiguration, TProp>> propertySelector)
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
}
