namespace Multifactor.Radius.Adapter.v2.Infrastructure.Cache.AuthenticatedClientCache;

public class AuthenticatedClient
{
    private readonly DateTime _authenticatedAt;

    public string Id { get; }
    public TimeSpan Elapsed => DateTime.Now - _authenticatedAt;

    public AuthenticatedClient(params string?[] components)
    {
        ArgumentNullException.ThrowIfNull(components);
        if (components.Length == 0) throw new ArgumentException(nameof(components));
        Id = ParseId(components);
        _authenticatedAt = DateTime.Now;
    }

    public static string ParseId(params string?[] components) => string.Join('-', components.Where(x => !string.IsNullOrWhiteSpace(x)));      
}