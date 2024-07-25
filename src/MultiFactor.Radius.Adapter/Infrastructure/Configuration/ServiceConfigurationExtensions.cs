using System.Linq;
using System.Text;
using MultiFactor.Radius.Adapter.Infrastructure.Configuration.RootLevel;

namespace MultiFactor.Radius.Adapter.Infrastructure.Configuration;

public static class ServiceConfigurationExtensions
{
    public static void Validate(this IServiceConfiguration serviceConfiguration)
    {
        var sources = new[] { AuthenticationSource.Radius, AuthenticationSource.None };

        foreach (var client in serviceConfiguration.Clients)
        {
            if (!sources.Contains(client.FirstFactorAuthenticationSource) || !client.CheckMembership)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(client.ServiceAccountUser) || string.IsNullOrWhiteSpace(client.ServiceAccountPassword))
            {
                var msg = new StringBuilder($"Configuration error: 'service-account-user' and 'service-account-password' elements not found. Please check configuration of client '{client.Name}'.");
                throw new System.Exception(msg.ToString());
            }
        }
    }
}