namespace Multifactor.Radius.Adapter.v2.Core.Radius.Packet;

public class RadiusAttribute
{
    private List<object> _values;
    public IReadOnlyList<object> Values => _values;
    public string Name { get; }

    public RadiusAttribute(string attributeName)
    {
        Name = attributeName ?? throw new ArgumentNullException(nameof(attributeName));
        _values = new List<object>();
    }

    public void AddValues(params object?[] value)
    {
        if (value.Length > 0)
            _values.AddRange(value!);
        else
            throw new ArgumentException(nameof(value));
    }

    public void RemoveAllValues()
    {
        _values = new List<object>();
    }
}