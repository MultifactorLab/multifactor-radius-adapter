namespace Multifactor.Radius.Adapter.v2.Application.Core.Models.Packet;

/// <summary>
/// Radius attribute model
/// </summary>
public sealed class RadiusAttribute
{
    private readonly List<object> _values;
    public IReadOnlyList<object> Values => _values;
    public string Name { get; }

    public RadiusAttribute(string attributeName)
    {
        Name = attributeName ?? throw new ArgumentNullException(nameof(attributeName));
        _values = [];
    }

    public void AddValues(params object?[] value)
    {
        if (value.Length > 0)
            _values.AddRange(value!);
        else
            throw new ArgumentException(nameof(value));
    }
}