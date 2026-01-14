namespace Multifactor.Radius.Adapter.v2.Application.Features.Radius.Services;

public interface IRadiusAttributeTypeConverter
{
    object ConvertType(string attributeName, object value);
}