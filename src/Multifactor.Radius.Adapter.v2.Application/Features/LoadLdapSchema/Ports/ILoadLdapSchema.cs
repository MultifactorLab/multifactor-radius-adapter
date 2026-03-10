using Multifactor.Core.Ldap.Schema;
using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapSchema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapSchema.Ports
{
    public interface ILoadLdapSchema
    {
        ILdapSchema? Execute(LoadLdapSchemaDto request);
    }
}
