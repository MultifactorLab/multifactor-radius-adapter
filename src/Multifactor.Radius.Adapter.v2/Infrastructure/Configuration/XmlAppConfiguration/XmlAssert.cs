//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System.Xml.Linq;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.XmlAppConfiguration;

internal static class XmlAssert
{
    /// <summary>
    /// Explodes if the collection contains duplicates.
    /// </summary>
    /// <typeparam name="TKey">Selector key type.</typeparam>
    /// <param name="elements">Source collection.</param>
    /// <param name="keySelector">Grouping selector.</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="Exception"></exception>
    public static void HasUniqueElements<TKey>(IEnumerable<XElement> elements, Func<XElement, TKey> keySelector)
    {
        if (elements is null)
        {
            throw new ArgumentNullException(nameof(elements));
        }

        if (keySelector is null)
        {
            throw new ArgumentNullException(nameof(keySelector));
        }

        var duplicates = elements
            .GroupBy(keySelector)
            .Where(x => x.Count() > 1)
            .Select(x => $"'{x.Key}'")
            .ToArray();

        if (duplicates.Length != 0)
        {
            var d = string.Join(", ", duplicates);
            throw new Exception($"Invalid xml config. Duplicates found: {d}");
        }
    }

    /// <summary>
    /// Returns attribute value or throws if the attribute does not exist.
    /// </summary>
    /// <param name="element">Target element.</param>
    /// <param name="attribute">Attribute to get value from.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="Exception"></exception>
    public static string HasAttribute(XElement element, string attribute)
    {
        if (element is null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        if (string.IsNullOrWhiteSpace(attribute))
        {
            throw new ArgumentException($"'{nameof(attribute)}' cannot be null or whitespace.", nameof(attribute));
        }

        var attr = element.Attribute(attribute);
        if (attr == null)
        {
            throw new Exception($"Invalid xml config: required attribute 'value' not found. Target element: {element}");
        }

        return attr.Value;
    }
}
