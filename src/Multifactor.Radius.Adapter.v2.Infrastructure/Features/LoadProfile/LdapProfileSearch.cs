using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Multifactor.Core.Ldap;
using Multifactor.Core.Ldap.Connection;
using Multifactor.Core.Ldap.Connection.LdapConnectionFactory;
using Multifactor.Core.Ldap.Name;
using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadProfile.Ports;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.Pipeline.Models.Enum;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Multifactor.Core.Ldap.Extensions;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.LoadProfile
{
    internal sealed class LdapProfileSearch : IProfileSearch
    {
        private readonly ILogger<IProfileSearch> _logger;
        private readonly ILdapConnectionFactory _connectionFactory;
        private readonly LdapSchemaLoader _schemaLoader;
        public LdapProfileSearch(ILogger<IProfileSearch> logger,
        ILdapConnectionFactory connectionFactory,
        LdapSchemaLoader schemaLoader)
        {
            _logger = logger;
            _connectionFactory = connectionFactory;
            _schemaLoader = schemaLoader;
        }
        public ILdapProfile? Execute(FindUserRequest request)
        {
            _logger.LogInformation("Try to find '{userIdentity}' profile at '{domain}'.", request.UserIdentity.Identity, request.SearchBase.StringRepresentation);
            var connectionString = new LdapConnectionString(request.ConnectionData.ConnectionString, true);
            if (request.Domain is not null)
            {
                var trustedFqdn = DnToFqdn(request.Domain);
                connectionString = CopySchemaAndPort(connectionString, trustedFqdn);
            }
            var options = new LdapConnectionOptions(connectionString,
                AuthType.Negotiate,
                LdapNameConverter.ConvertToUpn(request.ConnectionData.UserName),
                request.ConnectionData.Password,
                TimeSpan.FromSeconds(request.ConnectionData.BindTimeoutInSeconds));
            using var connection = _connectionFactory.CreateConnection(options);
            var schema = _schemaLoader.Load(options);
            var identityToSearch = request.UserIdentity;
            if (request.UserIdentity.Format == UserIdentityFormat.NetBiosName)
            {
                var index = request.UserIdentity.Identity.IndexOf('\\');
                if (index <= 0)
                    throw new ArgumentException($"Invalid NetBIOS identity: {request.UserIdentity.Identity}");
                var userName = request.UserIdentity.Identity[(index + 1)..];
                identityToSearch = new UserIdentity(userName);
            }
            var filter = GetFilter(identityToSearch, schema);
            _logger.LogDebug("Search base = '{searchBase}'. Filter for search = '{filter}'", request.SearchBase.StringRepresentation, filter);
            var result = connection.Find(request.SearchBase, filter, SearchScope.Subtree, attributes: request.AttributeNames ?? []);
            var entry = result.FirstOrDefault();
            return entry is null ? null : new LdapProfile(entry, schema);
        }

        public LdapConnectionString CopySchemaAndPort(LdapConnectionString ldapConnectionString, string newHost)
        {
            var initialLdapSchema = ldapConnectionString.Scheme;
            var initialLdapPort = ldapConnectionString.Port;
            return new LdapConnectionString($"{initialLdapSchema}://{newHost}:{initialLdapPort}", true);
        }

        public static string DnToFqdn(DistinguishedName name)
        {
            var ncs = name.Components.Reverse();
            return string.Join(".", ncs.Select(x => x.Value));
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

    }
}
