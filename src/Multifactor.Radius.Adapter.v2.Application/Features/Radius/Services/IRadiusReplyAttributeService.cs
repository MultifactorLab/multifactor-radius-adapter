using Multifactor.Radius.Adapter.v2.Application.Features.Radius.Models;

namespace Multifactor.Radius.Adapter.v2.Application.Features.Radius.Services;

public interface IRadiusReplyAttributeService
{
    IDictionary<string, List<object>> GetReplyAttributes(GetReplyAttributesRequest request);
}