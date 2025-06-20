using System.DirectoryServices.Protocols;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.LdapGroup.Load;
using Multifactor.Core.Ldap.LdapGroup.Membership;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Core.Ldap;

namespace Multifactor.Radius.Adapter.v2.Services.Ldap;

public class LdapGroupService : ILdapGroupService
{
    private readonly ILdapConnectionFactory _ldapConnectionFactory;
    private readonly ILdapGroupLoaderFactory _ldapGroupLoaderFactory;
    private readonly IMembershipCheckerFactory _ldapMembershipCheckerFactory;

    public LdapGroupService(ILdapGroupLoaderFactory ldapGroupLoaderFactory, IMembershipCheckerFactory ldapMembershipCheckerFactory, ILdapConnectionFactory ldapConnectionFactory)
    {
        _ldapGroupLoaderFactory = ldapGroupLoaderFactory;
        _ldapMembershipCheckerFactory = ldapMembershipCheckerFactory;
        _ldapConnectionFactory = ldapConnectionFactory;
    }

    //TODO create request entity
    public IReadOnlyList<string> LoadUserGroups(ILdapSchema ldapSchema, ILdapConnection connection, DistinguishedName userName, DistinguishedName? searchBase = null, int limit = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(ldapSchema);
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(userName);

        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit));
        var groupLoader = _ldapGroupLoaderFactory.GetGroupLoader(ldapSchema, connection, searchBase ?? ldapSchema.NamingContext);
        var groupDns = groupLoader.GetGroups(userName, pageSize: 20);
        return groupDns.Take(limit).Select(x => x.Components.Deepest.Value).ToList();
    }
    
    public bool IsMemberOf(MembershipRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TargetGroups.Length == 0)
            throw new InvalidOperationException();
        
        var isMemberOf = ProcessProfileGroups(request);
        if (isMemberOf)
            return true;
        
        if (!request.LoadNestedGroups)
            return false;
        
        return ProcessNestedGroups(request);
    }
    
    private bool ProcessProfileGroups(MembershipRequest request)
    {
        var intersection = request.ProfileGroups.Intersect(request.TargetGroups);
        return intersection.Any();
    }
    
    private bool ProcessNestedGroups(MembershipRequest request)
    {
        using var connection = GetConnection(request);
        return IsMemberOfNestedGroups(request, connection);
    }
    
    private ILdapConnection GetConnection(MembershipRequest request)
    {
        var options = new LdapConnectionOptions(
            request.ConnectionString,
            AuthType.Basic,
            request.UserName,
            request.Password,
            request.Timeout);

        return _ldapConnectionFactory.CreateConnection(options);
    }
    
    private bool IsMemberOfNestedGroups(MembershipRequest request, ILdapConnection connection) => request.NestedGroupsBaseDns.Length > 0
        ? request.NestedGroupsBaseDns
            .Select(groupBaseDn => IsMemberOf(request, connection, groupBaseDn))
            .Any(isMemberOf => isMemberOf)
        : IsMemberOf(request, connection);
    
    private bool IsMemberOf(MembershipRequest request, ILdapConnection connection, DistinguishedName? searchBase = null)
    {
        var membershipChecker = _ldapMembershipCheckerFactory.GetMembershipChecker(request.LdapSchema, connection, searchBase ?? request.LdapSchema.NamingContext);
        return membershipChecker.IsMemberOf(request.UserDn, request.TargetGroups); 
    }
}