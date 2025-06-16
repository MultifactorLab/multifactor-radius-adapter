//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Multifactor.Radius.Adapter.v2.Extensions;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.XmlAppConfiguration;

public class XmlAppConfigurationSource : ConfigurationProvider, IConfigurationSource
{
    private const string _appSettingsElement = "appSettings";

    private readonly RadiusConfigurationFile _path;

    public XmlAppConfigurationSource(RadiusConfigurationFile path)
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
            throw new Exception($"Failed to load configuration file '{_path.Path}'", ex);
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

        var appSettings = root.Element(_appSettingsElement);
        if (appSettings != null)
        {
            var appSettingsElements = appSettings.Elements().ToArray();
            XmlAssert.HasUniqueElements(appSettingsElements, x => x.Attribute("key")?.Value);

            FillAppSettingsSection(appSettingsElements);
        }

        var sections = root.Elements()
            .Where(x => x.Name != _appSettingsElement)
            .ToArray();
        XmlAssert.HasUniqueElements(sections, x => x.Name);

        foreach (var section in sections)
        {
            FillSection(section);
        }
    }

    private void FillAppSettingsSection(XElement[] appSettingsElements)
    {
        for (var i = 0; i < appSettingsElements.Length; i++)
        {
            var key = XmlAssert.HasAttribute(appSettingsElements[i], "key");
            var value = XmlAssert.HasAttribute(appSettingsElements[i], "value");

            var newKey = $"{_appSettingsElement}:{key.ToPascalCase()}";
            Data.Add(newKey, value);
        }
    }

    private void FillSection(XElement section, string parentKey = null, string postfix = null)
    {
        var sectionKey = section.Name.ToString();
        if (parentKey != null)
        {
            sectionKey = $"{parentKey}:{sectionKey}";
        }

        if (postfix != null)
        {
            sectionKey = $"{sectionKey}:{postfix}";
        }

        if (section.HasAttributes)
        {
            foreach (var attr in section.Attributes())
            {
                var attrKey = $"{sectionKey}:{attr.Name.LocalName.ToPascalCase()}";
                Data[attrKey] = attr.Value;
            }
        }

        if (!section.HasElements)
        {
            return;
        }

        var groups = section.Elements().GroupBy(x => x.Name);
        foreach (var group in groups)
        {
            if (group.Count() == 1)
            {
                FillSection(group.First(), sectionKey);
                continue;
            }

            var index = 0;
            foreach (var arrEntry in group)
            {
                FillSection(arrEntry, sectionKey, index.ToString());
                index++;
            }
        }
    }
}