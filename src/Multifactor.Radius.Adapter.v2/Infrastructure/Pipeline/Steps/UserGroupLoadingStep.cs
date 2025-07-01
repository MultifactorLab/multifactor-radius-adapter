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

//TODO load groups only when request accepted or use cache
public class UserGroupLoadingStep : IRadiusPipelineStep
{
    private readonly ILdapGroupService _ldapGroupService;
    private readonly ILdapConnectionFactory _ldapConnectionFactory;
    private readonly ILogger<UserGroupLoadingStep> _logger;

    public UserGroupLoadingStep(ILdapGroupService groupService, ILdapConnectionFactory connectionFactory, ILogger<UserGroupLoadingStep> logger)
    {
        _ldapGroupService = groupService;
        _ldapConnectionFactory = connectionFactory;
        _logger = logger;
    }

    public Task ExecuteAsync(IRadiusPipelineExecutionContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(UserGroupLoadingStep));

        if (ShouldSkipGroupLoading(context))
        {
            _logger.LogDebug("User groups are not required.");
            return Task.CompletedTask;
        }
        
        var userGroups = new HashSet<string>();
        context.UserGroups = userGroups;

        foreach (var group in context.UserLdapProfile.MemberOf.Select(x => x.Components.Deepest.Value))
            userGroups.Add(group);
        
        if (!context.LdapServerConfiguration.LoadNestedGroups)
        {
            _logger.LogDebug("Nested groups for {domain} are not required.", context.LdapServerConfiguration.ConnectionString);
            return Task.CompletedTask;
        }

        LoadGroupsFromLdapCatalog(context, userGroups);
        
        return Task.CompletedTask;
    }

    private void LoadGroupsFromLdapCatalog(IRadiusPipelineExecutionContext context, HashSet<string> userGroups)
    {
        using var connection = _ldapConnectionFactory.CreateConnection(GetLdapConnectionOptions(context.LdapServerConfiguration));
        
        if (context.LdapServerConfiguration.NestedGroupsBaseDns.Count > 0)
            LoadUserGroupsFromContainers(context, userGroups, connection);
        else
            LoadUserGroupsFromRoot(context, userGroups, connection);
    }

    private void LoadUserGroupsFromContainers(IRadiusPipelineExecutionContext context, HashSet<string> userGroups, ILdapConnection connection)
    {
        foreach (var dn in context.LdapServerConfiguration.NestedGroupsBaseDns)
        {
            _logger.LogDebug("Loading nested groups from '{dn}' at '{domain}' for '{user}'", dn, context.LdapServerConfiguration.ConnectionString, context.RequestPacket.UserName);
            
            var request = new LoadUserGroupsRequest(
                context.LdapSchema!,
                connection,
                context.UserLdapProfile.Dn,
                new DistinguishedName(dn));
            
            var groups = _ldapGroupService.LoadUserGroups(request);
            var groupLog = string.Join("\n", groups);
            _logger.LogDebug("Found groups at '{domain}' for '{user}': {groups}", dn, context.RequestPacket.UserName, groupLog);
                
            foreach (var group in groups)
                userGroups.Add(group);
        }
    }

    private void LoadUserGroupsFromRoot(IRadiusPipelineExecutionContext context, HashSet<string> userGroups, ILdapConnection connection)
    {
        var request = new LoadUserGroupsRequest(
            context.LdapSchema!,
            connection,
            context.UserLdapProfile.Dn);
            
        _logger.LogDebug("Loading nested groups from root at '{domain}' for '{user}'", context.LdapServerConfiguration.ConnectionString, context.RequestPacket.UserName);   
        var groups = _ldapGroupService.LoadUserGroups(request);
            
        var groupLog = string.Join("\n", groups);
        _logger.LogDebug("Found groups at root for '{user}': {groups}", context.RequestPacket.UserName, groupLog);
        foreach (var group in groups)
            userGroups.Add(group);
    }

    private bool ShouldSkipGroupLoading(IRadiusPipelineExecutionContext context) => !context
        .RadiusReplyAttributes
        .Values
        .SelectMany(x => x)
        .Any(x => x.IsMemberOf || x.UserGroupCondition.Count > 0);

    private LdapConnectionOptions GetLdapConnectionOptions(ILdapServerConfiguration serverConfiguration)
    {
        return new LdapConnectionOptions(
            new LdapConnectionString(serverConfiguration.ConnectionString),
            AuthType.Basic,
            serverConfiguration.UserName,
            serverConfiguration.Password,
            TimeSpan.FromSeconds(serverConfiguration.BindTimeoutInSeconds));
    }
}