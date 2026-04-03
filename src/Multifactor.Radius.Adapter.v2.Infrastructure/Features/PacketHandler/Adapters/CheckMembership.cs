using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.LdapGroup.Membership;
using Multifactor.Core.Ldap.Name;
using Multifactor.Radius.Adapter.v2.Application.Core.Models.Dto;
using Multifactor.Radius.Adapter.v2.Application.SharedPorts;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.Adapters;

internal sealed class CheckMembership : ICheckMembership
{
    private readonly ILdapConnectionFactory _connectionFactory;
    private readonly IMembershipCheckerFactory _ldapMembershipCheckerFactory;

    public CheckMembership(ILdapConnectionFactory connectionFactory, IMembershipCheckerFactory ldapMembershipCheckerFactory)
    {
        _connectionFactory = connectionFactory;
        _ldapMembershipCheckerFactory = ldapMembershipCheckerFactory;
    }

    public bool Execute(MembershipDto dto)
    {        
        ArgumentNullException.ThrowIfNull(dto);
        if(dto.TargetGroups == null || dto.TargetGroups.Length == 0)
            throw new InvalidOperationException();
        var options = new LdapConnectionOptions(
            new LdapConnectionString(dto.ConnectionString, true), 
            dto.AuthType, 
            dto.UserName, 
            dto.Password, 
            TimeSpan.FromSeconds(dto.BindTimeoutInSeconds));
        
        using var connection = _connectionFactory.CreateConnection(options);        
        return dto.NestedGroupsBaseDns.Length > 0
            ? dto.NestedGroupsBaseDns
                .Select(groupBaseDn => IsMemberOf(dto, connection, groupBaseDn))
                .Any(isMemberOf => isMemberOf)
            : IsMemberOf(dto, connection);
    }
    
    private bool IsMemberOf(MembershipDto request, ILdapConnection connection, DistinguishedName? searchBase = null)
    {
        var membershipChecker = _ldapMembershipCheckerFactory.GetMembershipChecker(request.LdapSchema, connection, searchBase ?? request.LdapSchema.NamingContext);
        return membershipChecker.IsMemberOf(request.DistinguishedName, request.TargetGroups.ToArray()); 
    }
}