namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Interfaces;

public interface IRadiusAttributeTypeConverter
{
    object ConvertType(string attrName, object value);
}