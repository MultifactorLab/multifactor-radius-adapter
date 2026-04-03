namespace Multifactor.Radius.Adapter.v2.Infrastructure.Features.PacketHandler.Adapters.Radius.Models;

internal sealed class ParsedAttribute
{
    public string Name { get; }
    public object Value { get; }
    public bool IsMessageAuthenticator { get; }

    public ParsedAttribute(string name, object value, bool isMessageAuthenticator = false)
    {
        Name = name;
        Value = value;
        IsMessageAuthenticator = isMessageAuthenticator;
    }
}
