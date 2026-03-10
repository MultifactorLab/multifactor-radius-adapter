using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.LdapGroup.Load;
using Multifactor.Core.Ldap.LdapGroup.Membership;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
using System.DirectoryServices.Protocols;
using System.Text;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Ldap;

public sealed class LdapAdapter : ILdapAdapter
{
    private readonly ILdapConnectionFactory _connectionFactory;
    private readonly IMembershipCheckerFactory _ldapMembershipCheckerFactory;
    private readonly ILdapGroupLoaderFactory  _ldapGroupLoaderFactory;
    private readonly ILogger<LdapAdapter> _logger;

    public LdapAdapter(
        ILdapConnectionFactory connectionFactory,
        ILogger<LdapAdapter> logger, IMembershipCheckerFactory ldapMembershipCheckerFactory, ILdapGroupLoaderFactory ldapGroupLoaderFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ldapMembershipCheckerFactory = ldapMembershipCheckerFactory;
        _ldapGroupLoaderFactory = ldapGroupLoaderFactory;
    }

    public IReadOnlyList<string> LoadUserGroups(LoadUserGroupRequest request)
    {
        using var connection = CreateConnection(request.ConnectionData);
        var groupLoader = _ldapGroupLoaderFactory.GetGroupLoader(request.LdapSchema, connection, request.SearchBase ?? request.LdapSchema.NamingContext);
        var groupDns = groupLoader.GetGroups(request.UserDN, pageSize: 20);
        return groupDns.Take(request.Limit).Select(x => x.Components.Deepest.Value).ToList();
    }


    public bool CheckConnection(LdapConnectionData request)
    {
        using var connection = CreateConnection(request);
        return true; //true of exception
    }

    #region IsMemberOf
    public bool IsMemberOf(MembershipRequest request)
    {        
        ArgumentNullException.ThrowIfNull(request);
        if(request.TargetGroups == null || request.TargetGroups.Length == 0)
            throw new InvalidOperationException();
        using var connection = CreateConnection(request.ConnectionData);
        
        return request.NestedGroupsBaseDns.Length > 0
            ? request.NestedGroupsBaseDns
                .Select(groupBaseDn => IsMemberOf(request, connection, groupBaseDn))
                .Any(isMemberOf => isMemberOf)
            : IsMemberOf(request, connection);
    }
    
    private bool IsMemberOf(MembershipRequest request, ILdapConnection connection, DistinguishedName? searchBase = null)
    {
        var membershipChecker = _ldapMembershipCheckerFactory.GetMembershipChecker(request.LdapSchema, connection, searchBase ?? request.LdapSchema.NamingContext);
        return membershipChecker.IsMemberOf(request.DistinguishedName, request.TargetGroups.ToArray()); 
    }
    #endregion

    #region ChangeUserPassword
    public bool ChangeUserPassword(ChangeUserPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        
        using var connection = CreateConnection(request.ConnectionData);
        var changePasswordRequest = BuildPasswordChangeRequest(request.LdapSchema, request.DistinguishedName, request.NewPassword);
        var response = connection.SendRequest(changePasswordRequest);
        return response.ResultCode == ResultCode.Success;
    }
    
    private static ModifyRequest BuildPasswordChangeRequest(ILdapSchema ldapSchema, DistinguishedName userDn, string newPassword)
    {
        var attributeName = ldapSchema.LdapServerImplementation == LdapImplementation.ActiveDirectory
            ? "unicodePwd"
            : "userpassword";

        var newPasswordAttribute = new DirectoryAttributeModification
        {
            Name = attributeName,
            Operation = DirectoryAttributeOperation.Replace
        };
        if (ldapSchema.LdapServerImplementation == LdapImplementation.ActiveDirectory)
            newPasswordAttribute.Add(Encoding.Unicode.GetBytes($"\"{newPassword}\""));
        else
            newPasswordAttribute.Add(newPassword);

        return new ModifyRequest(userDn.StringRepresentation, newPasswordAttribute);
    }
    #endregion

    private ILdapConnection CreateConnection(LdapConnectionData data)
    {
        var options = new LdapConnectionOptions(new LdapConnectionString(data.ConnectionString, true, false), 
            AuthType.Basic, 
            data.UserName, 
            data.Password, 
            TimeSpan.FromSeconds(data.BindTimeoutInSeconds));
        return _connectionFactory.CreateConnection(options);
    }

}