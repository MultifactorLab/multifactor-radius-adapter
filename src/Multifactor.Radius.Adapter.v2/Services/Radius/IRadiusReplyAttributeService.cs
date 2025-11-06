namespace Multifactor.Radius.Adapter.v2.Services.Radius;

public interface IRadiusReplyAttributeService
{
    IDictionary<string, List<object>> GetReplyAttributes(GetReplyAttributesRequest request);
}