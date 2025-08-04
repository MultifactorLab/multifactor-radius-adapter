using Multifactor.Core.Ldap.Schema;

namespace Multifactor.Radius.Adapter.v2.Core.Ldap.Forest;

public class LdapForestEntry
{
    private readonly HashSet<string> _suffixes = new();
    public ILdapSchema Schema { get; }
    public IReadOnlyCollection<string> Suffixes => _suffixes;

    public LdapForestEntry(ILdapSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
        Schema = schema;
    }
    
    public LdapForestEntry(ILdapSchema schema, IEnumerable<string> suffixes)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(suffixes);
        
        Schema = schema;
        foreach (var suffix in suffixes)
            Add(suffix);
    }
    
    public void AddSuffix(string suffix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suffix);
        Add(suffix);
    }
    
    public void AddSuffix(IEnumerable<string> suffix)
    {
        ArgumentNullException.ThrowIfNull(suffix);
        foreach (var s in suffix)
            Add(s);
    }

    private void Add(string suffix)
    {
        _suffixes.Add(NormalizeSuffix(suffix));
    }

    private string NormalizeSuffix(string suffix) => suffix.ToLower().Trim();

}