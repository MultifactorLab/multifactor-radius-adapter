using System.Globalization;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;

namespace Multifactor.Radius.Adapter.v2.Infrastructure.Logging;

public class CustomCompactJsonFormatter : ITextFormatter
{
    private readonly JsonValueFormatter _valueFormatter;
    private readonly string _timestampFormat;

    public CustomCompactJsonFormatter(string timestampFormat, JsonValueFormatter? valueFormatter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timestampFormat);
        
        _timestampFormat = timestampFormat;
        _valueFormatter = valueFormatter ?? new JsonValueFormatter("$type");
    }

    public void Format(LogEvent logEvent, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(output);

        FormatEvent(logEvent, output);
        output.WriteLine();
    }

    private void FormatEvent(LogEvent logEvent, TextWriter output)
    {
        output.Write("{\"@t\":\"");
        output.Write(logEvent.Timestamp.ToString(_timestampFormat));
        output.Write("\",\"@m\":");
        WriteJsonString(logEvent.MessageTemplate.Render(logEvent.Properties, CultureInfo.InvariantCulture), output);
        output.Write(",\"@i\":\"");
        output.Write(EventIdHash.Compute(logEvent.MessageTemplate.Text).ToString("x8", CultureInfo.InvariantCulture));
        output.Write('"');

        WriteLevel(logEvent.Level, output);
        WriteException(logEvent.Exception, output);
        WriteProperties(logEvent.Properties, output);

        output.Write('}');
    }

    private static void WriteJsonString(string value, TextWriter output)
    {
        JsonValueFormatter.WriteQuotedJsonString(value, output);
    }

    private static void WriteLevel(LogEventLevel level, TextWriter output)
    {
        if (level != LogEventLevel.Information)
        {
            output.Write(",\"@l\":\"");
            output.Write(level);
            output.Write('\"');
        }
    }

    private static void WriteException(Exception? exception, TextWriter output)
    {
        if (exception != null)
        {
            output.Write(",\"@x\":");
            WriteJsonString(exception.ToString(), output);
        }
    }

    private void WriteProperties(IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output)
    {
        foreach (var property in properties)
        {
            var name = EscapePropertyName(property.Key);
            
            output.Write(',');
            WriteJsonString(name, output);
            output.Write(':');
            _valueFormatter.Format(property.Value, output);
        }
    }

    private static string EscapePropertyName(string name)
    {
        if (name.Length > 0 && name[0] == '@')
            return "@" + name;
            
        return name;
    }
}