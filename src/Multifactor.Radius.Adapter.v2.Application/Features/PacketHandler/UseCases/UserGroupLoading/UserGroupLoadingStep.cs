using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using Multifactor.Radius.Adapter.v2.Application.Core.Enum;
using Multifactor.Radius.Adapter.v2.Application.Core.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.UserGroupLoading.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.UserGroupLoading.Ports;

namespace Multifactor.Radius.Adapter.v2.Application.Features.PacketHandler.UseCases.UserGroupLoading;

internal sealed class UserGroupLoadingStep : IRadiusPipelineStep
{
    private readonly ILoadGroups _loadGroups;
    private readonly ILogger<UserGroupLoadingStep> _logger;
    private const string StepName = nameof(UserGroupLoadingStep);
    public UserGroupLoadingStep(ILoadGroups loadGroups, ILogger<UserGroupLoadingStep> logger)
    {
        _loadGroups = loadGroups;
        _logger = logger;
    }

    public Task ExecuteAsync(RadiusPipelineContext context)
    {
        _logger.LogDebug("'{name}' started", StepName);

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
        var userIdentity = new UserIdentity(context.RequestPacket.UserName);
        var domainInfo = context.ForestMetadata?.DetermineForestDomain(userIdentity);
        var connectionString = domainInfo?.ConnectionString ?? context.LdapConfiguration!.ConnectionString;
        var schema = domainInfo?.Schema ?? context.LdapSchema!;
        var authType = domainInfo?.GetAuthType() ?? AuthType.Basic;
        var userName = context.LdapConfiguration.Username;
        if (authType == AuthType.Negotiate)
        {
            userName = UserIdentity.TransformDnToUpn(context.LdapConfiguration.Username);
        }

        foreach (var dn in context.LdapConfiguration!.NestedGroupsBaseDns)
        {
            _logger.LogDebug("Loading nested groups from '{dn}' at '{domain}' for '{user}'", dn, connectionString, context.RequestPacket.UserName);

            var dto = new LoadUserGroupDto()
            { 
                ConnectionString = connectionString,
                UserName = userName,
                Password = context.LdapConfiguration.Password,
                BindTimeoutInSeconds = context.LdapConfiguration.BindTimeoutSeconds,
                LdapSchema = schema,
                UserDN = context.LdapProfile!.Dn,
                SearchBase = dn,
                AuthType = authType
            };
            
            var groups = _loadGroups.Execute(dto);
            var groupLog = string.Join("\n", groups);
            _logger.LogDebug("Found groups at '{domain}' for '{user}': {groups}", dn, context.RequestPacket.UserName, groupLog);
                
            foreach (var group in groups)
                userGroups.Add(group);
        }
    }

    private void LoadUserGroupsFromRoot(RadiusPipelineContext context, HashSet<string> userGroups)
    {        
        var userIdentity = new UserIdentity(context.RequestPacket.UserName);
        var domainInfo = context.ForestMetadata?.DetermineForestDomain(userIdentity);
        var connectionString = domainInfo?.ConnectionString ?? context.LdapConfiguration!.ConnectionString;
        var schema = domainInfo?.Schema ?? context.LdapSchema!;
        var authType = domainInfo?.GetAuthType() ?? AuthType.Basic;
        var userName = context.LdapConfiguration.Username;
        if (authType == AuthType.Negotiate)
        {
            userName = UserIdentity.TransformDnToUpn(context.LdapConfiguration.Username);
        }
        var dto = new LoadUserGroupDto()
        {
            ConnectionString = connectionString!,
            UserName = userName,
            Password = context.LdapConfiguration.Password,
            BindTimeoutInSeconds = context.LdapConfiguration.BindTimeoutSeconds,
            LdapSchema = schema,
            UserDN = context.LdapProfile!.Dn,
            AuthType = authType
        };
            
        _logger.LogDebug("Loading nested groups from root at '{domain}' for '{user}'", connectionString, context.RequestPacket.UserName);   
        var groups = _loadGroups.Execute(dto);
            
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