using System.Globalization;
using System.Text;
using System.Text.Json;
using Serilog.Events;
using Serilog.Formatting;

namespace StockMgmt.Common.Logging;

public sealed class CustomJsonFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();

            writer.WriteString("timestamp", logEvent.Timestamp.ToUniversalTime());
            writer.WriteString("level", logEvent.Level.ToString());
            writer.WriteString("service", GetString(logEvent, "Application") ?? "stock-mgmt");
            writer.WriteString("environment", GetString(logEvent, "Environment") ?? "Production");
            WriteStringIfNotEmpty(writer, "method", GetString(logEvent, "RequestMethod"));
            WriteStringIfNotEmpty(writer, "path", GetString(logEvent, "RequestPath"));

            var statusCode = GetInt(logEvent, "StatusCode");
            if (statusCode.HasValue)
                writer.WriteNumber("statusCode", statusCode.Value);

            var elapsedMs = GetElapsed(logEvent, "Elapsed");
            if (elapsedMs.HasValue)
                writer.WriteNumber("elapsedMs", elapsedMs.Value);

            WriteStringIfNotEmpty(writer, "traceId", GetString(logEvent, "TraceIdentifier"));
            var rawTemplate = logEvent.MessageTemplate.Text;
            var message = rawTemplate;
            var colonIndex = rawTemplate.IndexOf(':');
            if (colonIndex > 0 && rawTemplate.IndexOf('{', colonIndex) >= 0)
                message = rawTemplate[..colonIndex].Trim();
            writer.WriteString("message", message);

            var userId = GetLong(logEvent, "UserId");
            if (userId.HasValue)
                writer.WriteNumber("userId", userId.Value);

            var productId = GetLong(logEvent, "ProductId");
            if (productId.HasValue)
                writer.WriteNumber("productId", productId.Value);

            if (logEvent.Exception is not null)
            {
                writer.WriteStartObject("exception");
                writer.WriteString("type", logEvent.Exception.GetType().FullName);
                writer.WriteString("message", logEvent.Exception.Message);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }
        output.Write(Encoding.UTF8.GetString(stream.ToArray()));
        output.WriteLine();
    }

    private static string? GetString(LogEvent logEvent, string propertyName)
    {
        if (!logEvent.Properties.TryGetValue(propertyName, out var value))
            return null;

        return value switch
        {
            ScalarValue scalar when scalar.Value is not null => Convert.ToString(scalar.Value, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    private static int? GetInt(LogEvent logEvent, string propertyName)
    {
        if (!logEvent.Properties.TryGetValue(propertyName, out var value))
            return null;

        if (value is ScalarValue scalar)
        {
            return scalar.Value switch
            {
                int i => i,
                long l => (int)l,
                double d => (int)Math.Round(d),
                decimal m => (int)Math.Round(m),
                _ => null
            };
        }

        return null;
    }

    private static long? GetLong(LogEvent logEvent, string propertyName)
    {
        if (!logEvent.Properties.TryGetValue(propertyName, out var value))
            return null;

        if (value is ScalarValue scalar)
        {
            return scalar.Value switch
            {
                long l => l,
                int i => i,
                double d => (long)Math.Round(d),
                decimal m => (long)Math.Round(m),
                _ => null
            };
        }

        return null;
    }

    private static int? GetElapsed(LogEvent logEvent, string propertyName)
    {
        var elapsed = GetInt(logEvent, propertyName);
        if (elapsed.HasValue)
            return elapsed;

        // Serilog.AspNetCore can emit ElapsedMilliseconds instead of Elapsed depending on configuration
        return GetInt(logEvent, "ElapsedMilliseconds");
    }

    private static void WriteStringIfNotEmpty(Utf8JsonWriter writer, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            writer.WriteString(name, value);
    }
}
