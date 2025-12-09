namespace Multifactor.Radius.Adapter.v2.Infrastructure.Exceptions
{
    internal class LdapUserNotFoundException : Exception
    {
        public LdapUserNotFoundException(string user, string domain)
            : base($"User '{user}' not found at domain '{domain}'") { }

        public LdapUserNotFoundException(string user, string domain, Exception inner)
            : base($"User '{user}' not found at domain '{domain}': {inner.Message}", inner) { }
    }
}
