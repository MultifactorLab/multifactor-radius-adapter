using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Models
{
    public class LoadMetadataDto
    {
        public LdapConnectionData ConnectionData { get; set; }
        public bool AlternativeSuffixesEnabled { get; set; }
    }
}
