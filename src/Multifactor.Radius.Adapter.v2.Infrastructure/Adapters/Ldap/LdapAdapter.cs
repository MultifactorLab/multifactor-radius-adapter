using System.DirectoryServices.Protocols;
using System.Text;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Extensions;
using Multifactor.Core.Ldap.LdapGroup.Load;
using Multifactor.Core.Ldap.LdapGroup.Membership;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Adapters.Ldap;

public sealed class LdapAdapter : ILdapAdapter
{
    private readonly ILdapConnectionFactory _connectionFactory;
    private readonly LdapSchemaLoader _schemaLoader;
    private readonly IMembershipCheckerFactory _ldapMembershipCheckerFactory;
    private readonly ILdapGroupLoaderFactory  _ldapGroupLoaderFactory;
    private readonly ILogger<LdapAdapter> _logger;

    public LdapAdapter(
        ILdapConnectionFactory connectionFactory,
        LdapSchemaLoader schemaLoader,
        ILogger<LdapAdapter> logger, IMembershipCheckerFactory ldapMembershipCheckerFactory, ILdapGroupLoaderFactory ldapGroupLoaderFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _schemaLoader = schemaLoader ?? throw new ArgumentNullException(nameof(schemaLoader));
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

    #region FindUserProfile
    public ILdapProfile? FindUserProfile(FindUserRequest request)
    {
        using var connection = CreateConnection(request.ConnectionData);   
        var identityToSearch = request.UserIdentity;
        if (request.UserIdentity.Format == UserIdentityFormat.NetBiosName)
        {
            var index = request.UserIdentity.Identity.IndexOf('\\');
            if (index <= 0)
                throw new ArgumentException($"Invalid NetBIOS identity: {request.UserIdentity.Identity}");
            var userName = request.UserIdentity.Identity[(index + 1)..];
            identityToSearch = new UserIdentity(userName);
        }
        var filter = GetFilter(identityToSearch, request.LdapSchema);
        var result = connection.Find(request.SearchBase, filter, SearchScope.Subtree, attributes: request.AttributeNames ?? []);
        var entry = result.FirstOrDefault();
        return entry is null ? null : new LdapProfile(entry, request.LdapSchema);
    }
    
    private string GetFilter(UserIdentity identity, ILdapSchema schema)
    {
        var identityAttribute = GetIdentityAttribute(identity, schema);
        var objectClass = schema.ObjectClass;
        var classValue = schema.UserObjectClass;

        return $"(&({objectClass}={classValue})({identityAttribute}={identity.Identity}))";
    }
    
    private static string GetIdentityAttribute(UserIdentity identity, ILdapSchema schema) => identity.Format switch
    {
        UserIdentityFormat.UserPrincipalName => "userPrincipalName",
        UserIdentityFormat.DistinguishedName => schema.Dn,
        UserIdentityFormat.SamAccountName => schema.Uid,
        _ => throw new NotSupportedException("Unsupported user identity format")
    };
    #endregion
    
    public ILdapSchema? LoadSchema(LdapConnectionData request)
    {
        var options = new LdapConnectionOptions(new LdapConnectionString(request.ConnectionString), 
            AuthType.Basic, 
            request.UserName, 
            request.Password, 
            TimeSpan.FromSeconds(request.BindTimeoutInSeconds));
        return _schemaLoader.Load(options);
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
        var options = new LdapConnectionOptions(new LdapConnectionString(data.ConnectionString, true), 
            AuthType.Basic, 
            data.UserName, 
            data.Password, 
            TimeSpan.FromSeconds(data.BindTimeoutInSeconds));
        return _connectionFactory.CreateConnection(options);
    }

}