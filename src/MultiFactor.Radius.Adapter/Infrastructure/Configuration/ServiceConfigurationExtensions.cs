using MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration;

public static class ServiceConfigurationExtensions
{
    public static void Validate(this IServiceConfiguration serviceConfiguration)
    {
        foreach (var client in serviceConfiguration.Clients)
        {
            var requireTechUser = client.FirstFactorAuthenticationSource == AuthenticationSource.ActiveDirectory
                ||
                client.FirstFactorAuthenticationSource == AuthenticationSource.Ldap
                ||
                client.CheckMembership;

            if (!requireTechUser)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(client.ServiceAccountUser) || string.IsNullOrWhiteSpace(client.ServiceAccountPassword))
            {
                var msg = $"Configuration error: 'service-account-user' and 'service-account-password' elements not found. Please check configuration of client '{client.Name}'.";
                throw new System.Exception(msg);
            }
        }
    }
}
