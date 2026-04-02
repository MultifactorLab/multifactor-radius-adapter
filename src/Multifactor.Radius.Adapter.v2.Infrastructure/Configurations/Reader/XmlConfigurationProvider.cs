
//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configurations.Reader;

internal sealed class XmlConfigurationProvider : ConfigurationProvider, IConfigurationSource
{
    private const string AppSettingsElement = "appSettings";
    
    private string _path;

    public XmlConfigurationProvider(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder) => this;

    public override void Load()
    {
        try
        {
            LoadInternal();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load configuration file '{_path}'", ex);
        }
    }

    private void LoadInternal()
    {
        var xml = XDocument.Load(_path);
        var root = xml.Root;

        if (root is null)
        {
            throw new Exception("Root XML element not found");
        }

        var appSettings = root.Element(AppSettingsElement);
        if (appSettings != null)
        {
            var appSettingsElements = appSettings.Elements().ToArray();
            XmlAssert.HasUniqueElements(appSettingsElements, x => x.Attribute("key")?.Value);
            FillAppSettingsSection(appSettingsElements);
        }

        var sections = root.Elements()
            .Where(x => x.Name != AppSettingsElement)
            .ToArray();
        XmlAssert.HasUniqueElements(sections, x => x.Name);

        foreach (var section in sections)
        {
            ProcessSection(section);
        }
    }

    private void ProcessSection(XElement section, string? parentKey = null)
    {
        var sectionName = section.Name.ToString();
        var currentKey = parentKey != null ? $"{parentKey}:{sectionName}" : sectionName;

        var childElements = section.Elements().ToArray();

        if (childElements.Length > 0)
        {
            var groups = childElements.GroupBy(x => x.Name);

            foreach (var group in groups)
            {
                if (group.Count() == 1)
                {
                    ProcessElement(group.First(), currentKey);
                }
                else
                {
                    var index = 0;
                    foreach (var element in group)
                    {
                        ProcessElement(element, currentKey, index);
                        index++;
                    }
                }
            }
        }
        else
        {
            ProcessAttributes(section, currentKey);
        }
    }

    private void ProcessElement(XElement element, string parentKey, int? index = null)
    {
        string elementKey;

        if (index.HasValue)
        {
            elementKey = $"{parentKey}:{index.Value}";
        }
        else
        {
            elementKey = $"{parentKey}:{element.Name}";
        }

        ProcessAttributes(element, elementKey);

        var nestedElements = element.Elements().ToArray();
        if (nestedElements.Length > 0)
        {
            var nestedGroups = nestedElements.GroupBy(x => x.Name);
            foreach (var group in nestedGroups)
            {
                if (group.Count() == 1)
                {
                    ProcessElement(group.First(), elementKey);
                }
                else
                {
                    var nestedIndex = 0;
                    foreach (var nestedElement in group)
                    {
                        ProcessElement(nestedElement, elementKey, nestedIndex);
                        nestedIndex++;
                    }
                }
            }
        }
    }

    private void ProcessAttributes(XElement element, string baseKey)
    {
        if (element.HasAttributes)
        {
            foreach (var attr in element.Attributes())
            {
                var attrKey = $"{baseKey}:{ToPascalCase(attr.Name.LocalName)}";
                Data[attrKey] = attr.Value;
            }
        }
    }

    private void FillAppSettingsSection(XElement[] appSettingsElements)
    {
        for (var i = 0; i < appSettingsElements.Length; i++)
        {
            var key = XmlAssert.HasAttribute(appSettingsElements[i], "key");
            var value = XmlAssert.HasAttribute(appSettingsElements[i], "value");

            var newKey = $"{AppSettingsElement}:{ToPascalCase(key)}";
            Data.Add(newKey, value);
        }
    }
    
    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;
            
        var separators = new[] { '-', '_', '.', ' ' };
        var parts = input.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        var result = new StringBuilder();
        
        foreach (var part in parts)
        {
            if (part.Length > 0)
            {
                result.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                {
                    result.Append(part.Substring(1).ToLowerInvariant());
                }
            }
        }
        
        return result.ToString();
    }
}

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

