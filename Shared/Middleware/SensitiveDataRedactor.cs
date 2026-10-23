#nullable enable
namespace Shared.Middleware;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml;

internal readonly record struct BodyCaptureResult(String? Body, String Status);

internal static class SensitiveDataRedactor
{
    public static IReadOnlyDictionary<String, String[]> RedactQuery(IQueryCollection query, IReadOnlySet<String> sensitiveFieldNames)
    {
        Dictionary<String, String[]> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<String, StringValues> item in query)
            result[item.Key] = item.Value.Select(value => sensitiveFieldNames.Contains(item.Key) ? "[REDACTED]" : value ?? String.Empty).ToArray();
        return result;
    }

    public static IReadOnlyDictionary<String, String> FilterHeaders(IHeaderDictionary headers, IReadOnlySet<String> allowlist, IReadOnlySet<String> sensitiveHeaders)
    {
        Dictionary<String, String> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<String, StringValues> item in headers)
            if (allowlist.Contains(item.Key)) result[item.Key] = sensitiveHeaders.Contains(item.Key) ? "[REDACTED]" : item.Value.ToString();
        return result;
    }

    public static BodyCaptureResult RedactBody(ReadOnlyMemory<Byte> body, String? contentType, IReadOnlySet<String> sensitiveFieldNames, Int32 maximumBytes, IReadOnlySet<String> supportedContentTypes)
    {
        if (body.Length > Math.Max(0, maximumBytes)) return new(null, "size_limit");
        if (String.IsNullOrWhiteSpace(contentType)) return new(null, "unsupported_content_type");
        String mediaType = contentType.Split(';', 2)[0].Trim();
        Boolean json = mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
                       (mediaType.StartsWith("application/", StringComparison.OrdinalIgnoreCase) && mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase) && supportedContentTypes.Contains("application/*+json"));
        Boolean form = mediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
        Boolean xml = mediaType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) || mediaType.Equals("text/xml", StringComparison.OrdinalIgnoreCase) || mediaType.Equals("application/soap+xml", StringComparison.OrdinalIgnoreCase);
        Boolean allowed = supportedContentTypes.Contains(mediaType) || (json && supportedContentTypes.Contains("application/*+json"));
        if (!(json || form || xml) || !allowed) return new(null, "unsupported_content_type");
        try
        {
            String text = Encoding.UTF8.GetString(body.Span);
            if (json)
            {
                using JsonDocument doc = JsonDocument.Parse(text);
                using MemoryStream output = new();
                using (Utf8JsonWriter writer = new(output)) WriteJson(doc.RootElement, writer, sensitiveFieldNames);
                return new(Encoding.UTF8.GetString(output.ToArray()), "captured");
            }
            if (form)
            {
                String[] pairs = text.Split('&');
                for (Int32 i = 0; i < pairs.Length; i++)
                {
                    String[] part = pairs[i].Split('=', 2);
                    String key = Uri.UnescapeDataString(part[0].Replace('+', ' '));
                    if (sensitiveFieldNames.Contains(key)) pairs[i] = part[0] + "=[REDACTED]";
                }
                return new(String.Join('&', pairs), "captured");
            }
            XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            XmlDocument document = new() { XmlResolver = null };
            using (XmlReader reader = XmlReader.Create(new StringReader(text), settings)) document.Load(reader);
            RedactXml(document.DocumentElement, sensitiveFieldNames);
            return new(document.OuterXml, "captured");
        }
        catch { return new(null, "redaction_failed"); }
    }

    private static void WriteJson(JsonElement element, Utf8JsonWriter writer, IReadOnlySet<String> sensitive)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    if (sensitive.Contains(property.Name)) writer.WriteStringValue("[REDACTED]"); else WriteJson(property.Value, writer, sensitive);
                }
                writer.WriteEndObject(); break;
            case JsonValueKind.Array:
                writer.WriteStartArray(); foreach (JsonElement child in element.EnumerateArray()) WriteJson(child, writer, sensitive); writer.WriteEndArray(); break;
            default: element.WriteTo(writer); break;
        }
    }

    private static void RedactXml(XmlNode? node, IReadOnlySet<String> sensitive)
    {
        if (node is not XmlElement element) return;
        foreach (XmlAttribute attribute in element.Attributes)
            if (sensitive.Contains(attribute.LocalName)) attribute.Value = "[REDACTED]";
        if (sensitive.Contains(element.LocalName))
        {
            element.InnerText = "[REDACTED]";
            return;
        }
        foreach (XmlNode child in element.ChildNodes) RedactXml(child, sensitive);
    }
}
