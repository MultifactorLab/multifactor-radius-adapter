using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap.Attributes;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap.Identity;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.Ldap;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

public class ProfileLoadingStep : IRadiusPipelineStep
{
    private readonly ILdapProfileService _ldapProfileService;
    private readonly ILogger<ProfileLoadingStep> _logger;

    public ProfileLoadingStep(ILdapProfileService ldapProfileService, ILogger<ProfileLoadingStep> logger)
    {
        ArgumentNullException.ThrowIfNull(ldapProfileService);
        ArgumentNullException.ThrowIfNull(logger);

        _ldapProfileService = ldapProfileService;
        _logger = logger;
    }

    public Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.RequestPacket.UserName))
        {
            var clientAddress = context.ProxyEndpoint?.Address.ToString() ?? context.RemoteEndpoint.Address.ToString();
            _logger.LogWarning("No user name provided in packet '{id}' from '{client}'", context.RequestPacket.Identifier, clientAddress);
            return Task.CompletedTask;
        }

        if (context.LdapSchema is null)
        {
            _logger.LogError("No ldap schema loaded.");
            return Task.CompletedTask;
        }

        var userIdentity = new UserIdentity(context.RequestPacket.UserName);
        var attributes = GetAttributes(context.Settings.RadiusReplyAttributes.Values.SelectMany(x => x)).ToArray();
        var domain = context.LdapSchema.NamingContext;
        var profile = _ldapProfileService.LoadLdapProfile(context.Settings.ClientConfigurationName, context.FirstFactorLdapServerConfiguration, domain, userIdentity, attributes);
        if (profile is null)
        {
            _logger.LogWarning("Unable to load profile for user '{user}' from '{domain}'", userIdentity.Identity, domain.StringRepresentation);
            return Task.CompletedTask;;
        }

        context.UserLdapProfile = profile;
        return Task.CompletedTask;
    }

    private IEnumerable<LdapAttributeName> GetAttributes(IEnumerable<RadiusReplyAttributeValue> replyAttributeValues)
    {
        return replyAttributeValues.Where(x => x.FromLdap).Select(x => new LdapAttributeName(x.LdapAttributeName));
    }
}