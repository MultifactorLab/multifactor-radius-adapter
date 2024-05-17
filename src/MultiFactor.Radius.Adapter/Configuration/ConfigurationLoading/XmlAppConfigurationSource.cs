//Copyright(c) 2020 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/multifactor-radius-adapter/blob/main/LICENSE.md

using Microsoft.Extensions.Configuration;
using MultiFactor.Radius.Adapter.Core.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MultiFactor.Radius.Adapter.Configuration.ConfigurationLoading;

internal class XmlAppConfigurationSource : ConfigurationProvider, IConfigurationSource
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
        var xml = XDocument.Load(_path);
        var root = xml.Root;

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
            try
            {
                FillSection(section);
            }
            // убрать срань
            catch (StackOverflowException ex)
            {
                throw new Exception($"Section '{section.Name}' has too many levels of nested nodes", ex);
            }
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
                var attrKey = $"{sectionKey}:{attr.Name}";
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