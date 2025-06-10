namespace Multifactor.Radius.Adapter.v2.Services.AuthenticatedClientCache;

public class AuthenticatedClient
{
    private readonly DateTime _authenticatedAt;

    public string Id { get; }
    public TimeSpan Elapsed => DateTime.Now - _authenticatedAt;

    public AuthenticatedClient(string id, DateTime authenticatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        
        Id = id;
        _authenticatedAt = authenticatedAt;
    }

    public static AuthenticatedClient Create(params string?[] components)
    {
        ArgumentNullException.ThrowIfNull(components);
        if (components.Length == 0) throw new ArgumentException(nameof(components));

        return new AuthenticatedClient(ParseId(components), DateTime.Now);
    }

    public static string ParseId(params string?[] components) => string.Join('-', components.Where(x => !string.IsNullOrWhiteSpace(x)));      
}