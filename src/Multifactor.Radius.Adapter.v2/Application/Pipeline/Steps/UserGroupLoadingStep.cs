using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps.Interfaces;
using Multifactor.Radius.Adapter.v2.Domain.Auth;
using Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces;
using Multifactor.Radius.Adapter.v2.Infrastructure.Configuration.Client;
using Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Dto;
using Multifactor.Radius.Adapter.v2.Infrastructure.Ldap.Interface;
using Multifactor.Radius.Adapter.v2.Infrastructure.Pipeline;
using ILdapConnection = Multifactor.Radius.Adapter.v2.Domain.Ldap.Interfaces.ILdapConnection;

namespace Multifactor.Radius.Adapter.v2.Application.Pipeline.Steps;

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

    public Task ExecuteAsync(RadiusPipelineExecutionContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(UserGroupLoadingStep));

        if (ShouldSkipGroupLoading(context))
            return Task.CompletedTask;
        
        ArgumentNullException.ThrowIfNull(context.UserLdapProfile, nameof(context.UserLdapProfile));
        ArgumentNullException.ThrowIfNull(context.LdapServerConfiguration, nameof(context.LdapServerConfiguration));
        
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

    private void LoadGroupsFromLdapCatalog(RadiusPipelineExecutionContext context, HashSet<string> userGroups)
    {
        using var connection = _ldapConnectionFactory.CreateConnection(GetLdapConnectionOptions(context.LdapServerConfiguration!));
        
        if (context.LdapServerConfiguration!.NestedGroupsBaseDns.Count > 0)
            LoadUserGroupsFromContainers(context, userGroups, connection);
        else
            LoadUserGroupsFromRoot(context, userGroups, connection);
    }

    private void LoadUserGroupsFromContainers(RadiusPipelineExecutionContext context, HashSet<string> userGroups, ILdapConnection connection)
    {
        foreach (var dn in context.LdapServerConfiguration!.NestedGroupsBaseDns)
        {
            _logger.LogDebug("Loading nested groups from '{dn}' at '{domain}' for '{user}'", dn, context.LdapServerConfiguration.ConnectionString, context.RequestPacket.UserName);
            
            var request = new LoadUserGroupsRequest(
                context.LdapSchema!,
                connection,
                context.UserLdapProfile!.Dn,
                dn);
            
            var groups = _ldapGroupService.LoadUserGroups(request);
            var groupLog = string.Join("\n", groups);
            _logger.LogDebug("Found groups at '{domain}' for '{user}': {groups}", dn, context.RequestPacket.UserName, groupLog);
                
            foreach (var group in groups)
                userGroups.Add(group);
        }
    }

    private void LoadUserGroupsFromRoot(RadiusPipelineExecutionContext context, HashSet<string> userGroups, ILdapConnection connection)
    {
        var request = new LoadUserGroupsRequest(
            context.LdapSchema!,
            connection,
            context.UserLdapProfile!.Dn);
            
        _logger.LogDebug("Loading nested groups from root at '{domain}' for '{user}'", context.LdapServerConfiguration!.ConnectionString, context.RequestPacket.UserName);   
        var groups = _ldapGroupService.LoadUserGroups(request);
            
        var groupLog = string.Join("\n", groups);
        _logger.LogDebug("Found groups at root for '{user}': {groups}", context.RequestPacket.UserName, groupLog);
        foreach (var group in groups)
            userGroups.Add(group);
    }

    private LdapConnectionOptions GetLdapConnectionOptions(ILdapServerConfiguration serverConfiguration)
    {
        return new LdapConnectionOptions(
            new LdapConnectionString(serverConfiguration.ConnectionString),
            AuthType.Basic,
            serverConfiguration.UserName,
            serverConfiguration.Password,
            TimeSpan.FromSeconds(serverConfiguration.BindTimeoutInSeconds));
    }

    private bool ShouldSkipGroupLoading(RadiusPipelineExecutionContext context)
    {
        return !AcceptedRequest(context) || GroupsNotRequired(context) || UnsupportedAccountType(context);
    }

    private bool GroupsNotRequired(RadiusPipelineExecutionContext context)
    {
        var notRequired = !context
            .RadiusReplyAttributes
            .Values
            .SelectMany(x => x)
            .Any(x => x.IsMemberOf || x.UserGroupCondition.Count > 0);

        if (!notRequired)
            return false;
        
        _logger.LogDebug("User groups are not required.");
        return true;
    }

    private bool UnsupportedAccountType(RadiusPipelineExecutionContext context)
    {
        if (context.IsDomainAccount)
            return false;
        
        _logger.LogInformation(
            "User '{user}' used '{accountType}' account to log in. User group loading is skipped.",
            context.RequestPacket.UserName,
            context.RequestPacket.AccountType);
        
        return true;
    }
    
    private bool AcceptedRequest(RadiusPipelineExecutionContext context)
    {
        return context.AuthenticationState.FirstFactorStatus is
                   AuthenticationStatus.Accept or AuthenticationStatus.Bypass
               && context.AuthenticationState.SecondFactorStatus is 
                   AuthenticationStatus.Accept or AuthenticationStatus.Bypass;
    }
}