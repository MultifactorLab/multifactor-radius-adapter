namespace Multifactor.Radius.Adapter.v2.Services.Radius;

public interface IRadiusAttributeTypeConverter
{
    object ConvertType(string attrName, object value);
}