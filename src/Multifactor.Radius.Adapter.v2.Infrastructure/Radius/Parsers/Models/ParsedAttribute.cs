namespace Multifactor.Radius.Adapter.v2.Infrastructure.Radius.Parsers.Models;

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
