using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace MultiFactor.Radius.Adapter.Services.Ldap.Profile;

internal class LdapAttributes : ILdapAttributes
{
    private readonly Dictionary<string, List<string>> _attrs;

    public string DistinguishedName { get; }
    public ReadOnlyCollection<string> Keys => new ReadOnlyCollection<string>(_attrs.Keys.ToList());

    /// <summary>
    /// Creates new instance of LdapAttributes with the specified entry distinguished name.
    /// </summary>
    /// <param name="dn">Entry distinguished name.</param>
    /// <exception cref="ArgumentException"></exception>
    public LdapAttributes(string dn)
    {
        if (string.IsNullOrWhiteSpace(dn))
        {
            throw new ArgumentException($"'{nameof(dn)}' cannot be null or whitespace.", nameof(dn));
        }

        DistinguishedName = dn;

        _attrs = new Dictionary<string, List<string>>();
    }

    /// <summary>
    /// Creates new instance of LdapAttributes with the specified entry distinguished name and copies content from the <paramref name="source"/> ldap attributes.
    /// </summary>
    /// <param name="dn">Entry distinguished name.</param>
    /// <param name="source">Source ldap attributes.</param>
    public LdapAttributes(string dn, ILdapAttributes source)
    {
        if (string.IsNullOrWhiteSpace(dn))
        {
            throw new ArgumentException($"'{nameof(dn)}' cannot be null or whitespace.", nameof(dn));
        }

        DistinguishedName = dn;

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (source is LdapAttributes ldapAttributes)
        {
            _attrs = ldapAttributes._attrs;
            return;
        }

        _attrs = new Dictionary<string, List<string>>();
        foreach (var attr in source.Keys)
        {
            _attrs[attr] = new List<string>(source.GetValues(attr));
        }
    }

    /// <summary>
    /// Creates new instance of LdapAttributes with the specified entry distinguished name and copies content from the <paramref name="source"/> ldap attributes.
    /// </summary>
    /// <param name="dn">Entry distinguished name.</param>
    /// <param name="source">Source ldap attributes.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public LdapAttributes(string dn, IDictionary<string, string[]> source)
    {
        if (string.IsNullOrWhiteSpace(dn))
        {
            throw new ArgumentException($"'{nameof(dn)}' cannot be null or whitespace.", nameof(dn));
        }

        DistinguishedName = dn;

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        _attrs = new Dictionary<string, List<string>>();
        foreach (var attr in source.Keys)
        {
            _attrs[attr] = new List<string>(source[attr]);
        }
    }

    public bool Has(string attribute)
    {
        if (attribute is null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }
        // ToLower(CultureInfo.InvariantCulture) - same as in the native DirectoryServices search result entry.
        return _attrs.ContainsKey(attribute.ToLower(CultureInfo.InvariantCulture));
    }

    public string GetValue(string attribute)
    {
        if (attribute is null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        var attr = attribute.ToLower(CultureInfo.InvariantCulture);
        if (!_attrs.ContainsKey(attr))
        {
            return default;
        }

        return _attrs[attr].FirstOrDefault();
    }

    public ReadOnlyCollection<string> GetValues(string attribute)
    {
        if (attribute is null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        var attr = attribute.ToLower(CultureInfo.InvariantCulture);

        if (!_attrs.ContainsKey(attr))
        {
            return new ReadOnlyCollection<string>(Array.Empty<string>());
        }

        return _attrs[attr].AsReadOnly();
    }

    public LdapAttributes Add(string attribute, IEnumerable<string> value)
    {
        if (attribute is null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var attr = attribute.ToLower(CultureInfo.InvariantCulture);
        if (!_attrs.ContainsKey(attr))
        {
            _attrs[attr] = new List<string>();
        }

        _attrs[attr].AddRange(value);
        _attrs[attr] = _attrs[attr].Where(x => x is not null).Distinct().ToList();

        return this;
    }
    
    public LdapAttributes Add(string attribute, string value) => Add(attribute, new[] { value });
    

    public LdapAttributes Remove(string attribute)
    {
        if (attribute is null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        var attr = attribute.ToLower(CultureInfo.InvariantCulture);
        if (_attrs.ContainsKey(attr))
        {
            _attrs[attr].Remove(attribute);
        }

        return this;
    }

    public LdapAttributes Replace(string attribute, IEnumerable<string> value)
    {
        if (attribute is null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        var attr = attribute.ToLower(CultureInfo.InvariantCulture);
        _attrs[attr] = new List<string>(value);

        return this;
    }
}
