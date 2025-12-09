using Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Dto;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Interfaces;

public interface IRadiusReplyAttributeService
{
    IDictionary<string, List<object>> GetReplyAttributes(GetReplyAttributesRequest request);
}