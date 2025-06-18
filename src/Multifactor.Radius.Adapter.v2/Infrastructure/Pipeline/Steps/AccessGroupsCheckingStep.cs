using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Core.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Core.Ldap;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Context;
using Multifactor.Radius.Adapter.v2.Services.Ldap;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline.Steps;

public class AccessGroupsCheckingStep : IRadiusPipelineStep
{
    private readonly ILdapGroupService _ldapGroupService;
    private readonly ILdapConnectionFactory _ldapConnectionFactory;
    private readonly ILogger<AccessGroupsCheckingStep> _logger;

    public AccessGroupsCheckingStep(
        ILdapGroupService ldapGroupService,
        ILdapConnectionFactory ldapConnectionFactory,
        ILogger<AccessGroupsCheckingStep> logger)
    {
        _ldapGroupService = ldapGroupService;
        _ldapConnectionFactory = ldapConnectionFactory;
        _logger = logger;
    }

    public Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Settings.LdapServerConfiguration, nameof(context.Settings.LdapServerConfiguration));
        ArgumentNullException.ThrowIfNull(context.UserLdapProfile, nameof(context.UserLdapProfile));
        ArgumentNullException.ThrowIfNull(context.LdapSchema, nameof(context.LdapSchema));
        
        var serverConfig = context.Settings.LdapServerConfiguration;
        if (serverConfig.AccessGroups.Count == 0)
            return Task.CompletedTask;

        var accessGroupsDns = serverConfig.AccessGroups.Select(x => new DistinguishedName(x)).ToArray();
        var isMemberOf = ProcessProfileGroups(context, accessGroupsDns);
        if (isMemberOf)
            return Task.CompletedTask;
        
        if (!context.Settings.LdapServerConfiguration.LoadNestedGroups)
            return TerminatePipeline(context);

        isMemberOf = ProcessNestedGroups(context, accessGroupsDns);
        
        return isMemberOf ? Task.CompletedTask : TerminatePipeline(context);
    }

    private bool ProcessProfileGroups(IRadiusPipelineExecutionContext context, DistinguishedName[] accessGroupsDns)
    {
        var intersection = context.UserLdapProfile.MemberOf.Intersect(accessGroupsDns);
        return intersection.Any();
    }

    private bool ProcessNestedGroups(IRadiusPipelineExecutionContext context, DistinguishedName[] accessGroupsDns)
    {
        if (context.LdapSchema is null)
            throw new InvalidOperationException("No LDAP schema configured.");
        
        using var connection = GetConnection(context.Settings.LdapServerConfiguration);
        return IsMemberOfNestedGroups(context, connection, accessGroupsDns);
    }

    private ILdapConnection GetConnection(ILdapServerConfiguration serverConfiguration)
    {
        var options = new LdapConnectionOptions(
            new LdapConnectionString(serverConfiguration.ConnectionString),
            AuthType.Basic,
            serverConfiguration.UserName,
            serverConfiguration.Password,
            TimeSpan.FromSeconds(serverConfiguration.BindTimeoutInSeconds));

        return _ldapConnectionFactory.CreateConnection(options);
    }

    private bool IsMemberOfNestedGroups(IRadiusPipelineExecutionContext context, ILdapConnection connection, DistinguishedName[] accessGroupsDns) => context.Settings.LdapServerConfiguration.NestedGroupsBaseDns.Count > 0
        ? context.Settings.LdapServerConfiguration.NestedGroupsBaseDns
            .Select(x =>  _ldapGroupService.IsMemberOf(context.LdapSchema!, connection, context.UserLdapProfile.Dn, accessGroupsDns, new DistinguishedName(x)))
            .Any(isMemberOf => isMemberOf)
        : _ldapGroupService.IsMemberOf(context.LdapSchema!, connection, context.UserLdapProfile.Dn, accessGroupsDns);

    private Task TerminatePipeline(IRadiusPipelineExecutionContext context)
    {
        _logger.LogWarning("User '{user}' is not member of any access group of the '{connectionString}'.", context.UserLdapProfile.Dn, context.Settings.LdapServerConfiguration.ConnectionString);
        context.ExecutionState.Terminate();
        return Task.CompletedTask;
    }
}