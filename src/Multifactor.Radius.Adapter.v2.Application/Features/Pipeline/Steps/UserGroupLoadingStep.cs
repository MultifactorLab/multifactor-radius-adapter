using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Steps;

public class UserGroupLoadingStep : IRadiusPipelineStep
{
    private readonly ILdapAdapter _ldapAdapter;
    private readonly ILogger<UserGroupLoadingStep> _logger;

    public UserGroupLoadingStep(ILdapAdapter ldapAdapter, ILogger<UserGroupLoadingStep> logger)
    {
        _ldapAdapter = ldapAdapter;
        _logger = logger;
    }

    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", nameof(UserGroupLoadingStep));

        if (ShouldSkipGroupLoading(context))
            return Task.CompletedTask;
        
        ArgumentNullException.ThrowIfNull(context.LdapProfile, nameof(context.LdapProfile));
        ArgumentNullException.ThrowIfNull(context.LdapConfiguration, nameof(context.LdapConfiguration));
        
        var userGroups = new HashSet<string>();

        foreach (var group in context.LdapProfile.MemberOf.Select(x => x.Components.Deepest.Value))
            userGroups.Add(group);
        
        context.UserGroups = userGroups;
        
        if (!context.LdapConfiguration.LoadNestedGroups)
        {
            _logger.LogDebug("Nested groups for {domain} are not required.", context.LdapConfiguration.ConnectionString);
            return Task.CompletedTask;
        }

        LoadGroupsFromLdapCatalog(context, userGroups);
        
        return Task.CompletedTask;
    }

    private void LoadGroupsFromLdapCatalog(RadiusPipelineContext context, HashSet<string> userGroups)
    {
        
        if (context.LdapConfiguration!.NestedGroupsBaseDns.Count > 0)
            LoadUserGroupsFromContainers(context, userGroups);
        else
            LoadUserGroupsFromRoot(context, userGroups);
    }

    private void LoadUserGroupsFromContainers(RadiusPipelineContext context, HashSet<string> userGroups)
    {
        foreach (var dn in context.LdapConfiguration!.NestedGroupsBaseDns)
        {
            _logger.LogDebug("Loading nested groups from '{dn}' at '{domain}' for '{user}'", dn, context.LdapConfiguration.ConnectionString, context.RequestPacket.UserName);

            var request = new LoadUserGroupRequest
            {
                ConnectionString = context.LdapConfiguration.ConnectionString,
                UserName = context.LdapConfiguration.Username,
                Password = context.LdapConfiguration.Password,
                BindTimeoutInSeconds = context.LdapConfiguration.BindTimeoutSeconds,
                LdapSchema = context.LdapSchema!,
                UserDN = context.LdapProfile!.Dn,
                SearchBase = dn
            };
            
            var groups = _ldapAdapter.LoadUserGroups(request);
            var groupLog = string.Join("\n", groups);
            _logger.LogDebug("Found groups at '{domain}' for '{user}': {groups}", dn, context.RequestPacket.UserName, groupLog);
                
            foreach (var group in groups)
                userGroups.Add(group);
        }
    }

    private void LoadUserGroupsFromRoot(RadiusPipelineContext context, HashSet<string> userGroups)
    {
            var request = new LoadUserGroupRequest
            {
                ConnectionString = context.LdapConfiguration.ConnectionString,
                UserName = context.LdapConfiguration.Username,
                Password = context.LdapConfiguration.Password,
                BindTimeoutInSeconds = context.LdapConfiguration.BindTimeoutSeconds,
                LdapSchema = context.LdapSchema!,
                UserDN = context.LdapProfile!.Dn
            };
            
        _logger.LogDebug("Loading nested groups from root at '{domain}' for '{user}'", context.LdapConfiguration!.ConnectionString, context.RequestPacket.UserName);   
        var groups = _ldapAdapter.LoadUserGroups(request);
            
        var groupLog = string.Join("\n", groups);
        _logger.LogDebug("Found groups at root for '{user}': {groups}", context.RequestPacket.UserName, groupLog);
        foreach (var group in groups)
            userGroups.Add(group);
    }

    private bool ShouldSkipGroupLoading(RadiusPipelineContext context)
    {
        return !AcceptedRequest(context) || GroupsNotRequired(context) || UnsupportedAccountType(context);
    }

    private bool GroupsNotRequired(RadiusPipelineContext context)
    {
        var notRequired = !context
            .ClientConfiguration.ReplyAttributes
            .Values
            .SelectMany(x => x)
            .Any(x => x.IsMemberOf || x.UserGroupCondition.Count > 0);

        if (!notRequired)
            return false;
        
        _logger.LogDebug("User groups are not required.");
        return true;
    }

    private bool UnsupportedAccountType(RadiusPipelineContext context)
    {
        if (context.IsDomainAccount)
            return false;
        
        _logger.LogInformation(
            "User '{user}' used '{accountType}' account to log in. User group loading is skipped.",
            context.RequestPacket.UserName,
            context.RequestPacket.AccountType);
        
        return true;
    }
    
    private static bool AcceptedRequest(RadiusPipelineContext context)
    {
        return context.FirstFactorStatus is
                   AuthenticationStatus.Accept or AuthenticationStatus.Bypass
               && context.SecondFactorStatus is 
                   AuthenticationStatus.Accept or AuthenticationStatus.Bypass;
    }
}