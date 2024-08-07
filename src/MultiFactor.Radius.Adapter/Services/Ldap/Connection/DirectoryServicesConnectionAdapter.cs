using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MultiFactor.Radius.Adapter.Services.Ldap.Profile;

namespace MultiFactor.Radius.Adapter.Services.Ldap.Connection;

public class LdapConnectionFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public LdapConnectionFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }
    
    public ILdapConnectionAdapter Create(string ldapPath, LdapIdentity username, string password)
    {
        return new DirectoryServicesConnectionAdapter(ldapPath, username, password);
    }
}

public class DirectoryServicesConnectionAdapter : ILdapConnectionAdapter
{
    private readonly DirectoryEntry _de;
    private readonly string _password;
    
    public string Path { get; }
    public LdapIdentity Username { get; }


    private readonly Lazy<LdapDomain> _domain;

    public DirectoryServicesConnectionAdapter(string path, LdapIdentity username, string password)
    {
        _de = new DirectoryEntry(path, username.Name, password, AuthenticationTypes.None);
        Path = path;
        Username = username;
        _password = password;
        _domain = new Lazy<LdapDomain>(() =>
        {
            var dObj = $"{path}/rootDSE";
            using var root = new DirectoryEntry(dObj, username.Name, password, AuthenticationTypes.None);
            var ctx = root.Properties["defaultNamingContext"][0]?.ToString();
            if (ctx is null)
            {
                throw new Exception("Unknown naming context");
            }

            return LdapDomain.Parse(ctx);
        });
    }
    
    public Task<LdapDomain> WhereAmIAsync()
    {
        return Task.FromResult(_domain.Value);
    }

    public Task<ILdapAttributes[]> SearchQueryAsync(string baseDn, string filter, SearchScope scope, params string[] attributes)
    {
        using var ds = new DirectorySearcher(_de);
        ds.SearchScope = System.DirectoryServices.SearchScope.Subtree;
        ds.PropertiesToLoad.Clear();
        ds.PropertiesToLoad.AddRange(attributes);

        using var searchResult = ds.FindAll();
        if (searchResult.Count == 0)
        {
            return Task.FromResult(Array.Empty<ILdapAttributes>());
        }
        
        var collection = new List<ILdapAttributes>();
        foreach (SearchResult entry in searchResult)
        {
            var profileAttributes = new LdapAttributes(GetString(entry, "distinguishedName"));
            collection.Add(profileAttributes);
            
            var keys = attributes.Where(x => !x.Equals("memberof", StringComparison.OrdinalIgnoreCase));
            foreach (var key in keys)
            {
                if (!entry.Properties.Contains(key))
                {
                    profileAttributes.Add(key, Array.Empty<string>());
                    continue;
                }     
            
                var values = GetStrings(entry, key);
                profileAttributes.Add(key, values);    
            }

            //groups
            var groups = entry.Properties.Contains("memberOf")
                ? GetStrings(entry, "memberOf")
                : [];

            profileAttributes.Add("memberOf", groups);

            // if (clientConfig.ShouldLoadUserGroups())
            // {
            //     var additionalGroups = await _userGroupsSource.GetUserGroupsAsync(clientConfig, adapter, entry.Dn);
            //     profileAttributes.Add("memberOf", additionalGroups);
            // }
        }
        
        return Task.FromResult(collection.ToArray());
    }
    
    private static string GetString(SearchResult result, string property)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (string.IsNullOrWhiteSpace(property))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(property));
        }

        var values = result.Properties[property];
        return values.Count == 0 
            ? null 
            : values[0].ToString();
    }
    
    private static string[] GetStrings(SearchResult result, string property)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (string.IsNullOrWhiteSpace(property))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(property));
        }

        var values = result.Properties[property];
        return values.Cast<object>().Select(x => x.ToString()).ToArray();
    }
    
    public void Dispose()
    {
        _de.Dispose();
    }
}