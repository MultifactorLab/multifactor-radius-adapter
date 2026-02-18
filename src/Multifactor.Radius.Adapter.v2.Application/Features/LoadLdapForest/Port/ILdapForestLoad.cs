using Multifactor.Radius.Adapter.v2.Application.Features.Ldap.Models;
using Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Port
{
    public interface ILdapForestLoad
    {
        IForestMetadata Execute(LoadMetadataDto request);
    }
}
