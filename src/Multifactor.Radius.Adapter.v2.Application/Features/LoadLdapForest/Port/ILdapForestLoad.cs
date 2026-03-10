using Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.LoadLdapForest.Port
{
    public interface ILdapForestLoad
    {
        IForestMetadata Execute(LoadMetadataDto request);
    }
}
