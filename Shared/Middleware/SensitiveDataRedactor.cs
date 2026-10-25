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
    private const String RedactedValue = "[REDACTED]";

    public static IReadOnlyDictionary<String, String[]> RedactQuery(IQueryCollection query, IReadOnlySet<String> sensitiveFieldNames)
    {
        Dictionary<String, String[]> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<String, StringValues> item in query)
            result[item.Key] = item.Value.Select(value => sensitiveFieldNames.Contains(item.Key) ? RedactedValue : value ?? String.Empty).ToArray();
        return result;
    }

    public static IReadOnlyDictionary<String, String> FilterHeaders(IHeaderDictionary headers, IReadOnlySet<String> allowlist, IReadOnlySet<String> sensitiveHeaders)
    {
        Dictionary<String, String> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<String, StringValues> item in headers)
            if (allowlist.Contains(item.Key)) result[item.Key] = sensitiveHeaders.Contains(item.Key) ? RedactedValue : item.Value.ToString();
        return result;
    }

    public static BodyCaptureResult RedactBody(ReadOnlyMemory<Byte> body, String? contentType, IReadOnlySet<String> sensitiveFieldNames, IReadOnlySet<String> supportedContentTypes)
    {
        if (!TryGetBodyFormat(contentType, supportedContentTypes, out BodyFormat format)) return new(null, "unsupported_content_type");
        try
        {
            String text = Encoding.UTF8.GetString(body.Span);
            return format switch
            {
                BodyFormat.Json => RedactJsonBody(text, sensitiveFieldNames),
                BodyFormat.Form => RedactFormBody(text, sensitiveFieldNames),
                BodyFormat.Xml => RedactXmlBody(text, sensitiveFieldNames),
                _ => new(null, "unsupported_content_type")
            };
        }
        catch { return new(null, "redaction_failed"); }
    }

    private static Boolean TryGetBodyFormat(String? contentType, IReadOnlySet<String> supportedContentTypes, out BodyFormat format)
    {
        format = default;
        if (String.IsNullOrWhiteSpace(contentType)) return false;
        String mediaType = contentType.Split(';', 2)[0].Trim();
        if (IsSupportedJsonMediaType(mediaType, supportedContentTypes))
        {
            format = BodyFormat.Json;
            return true;
        }
        if (IsSupportedFormMediaType(mediaType, supportedContentTypes))
        {
            format = BodyFormat.Form;
            return true;
        }
        if (IsSupportedXmlMediaType(mediaType, supportedContentTypes))
        {
            format = BodyFormat.Xml;
            return true;
        }
        return false;
    }

    private static Boolean IsSupportedJsonMediaType(String mediaType, IReadOnlySet<String> supportedContentTypes)
    {
        Boolean isJson = mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
                         (mediaType.StartsWith("application/", StringComparison.OrdinalIgnoreCase) &&
                          mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase) &&
                          supportedContentTypes.Contains("application/*+json"));
        return isJson && (supportedContentTypes.Contains(mediaType) || supportedContentTypes.Contains("application/*+json"));
    }

    private static Boolean IsSupportedFormMediaType(String mediaType, IReadOnlySet<String> supportedContentTypes) =>
        mediaType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) && supportedContentTypes.Contains(mediaType);

    private static Boolean IsSupportedXmlMediaType(String mediaType, IReadOnlySet<String> supportedContentTypes)
    {
        Boolean isXml = mediaType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) ||
                        mediaType.Equals("text/xml", StringComparison.OrdinalIgnoreCase) ||
                        mediaType.Equals("application/soap+xml", StringComparison.OrdinalIgnoreCase);
        return isXml && supportedContentTypes.Contains(mediaType);
    }

    private static BodyCaptureResult RedactJsonBody(String text, IReadOnlySet<String> sensitive)
    {
        using JsonDocument document = JsonDocument.Parse(text);
        using MemoryStream output = new();
        using (Utf8JsonWriter writer = new(output)) WriteJson(document.RootElement, writer, sensitive);
        return new(Encoding.UTF8.GetString(output.ToArray()), "captured");
    }

    private static BodyCaptureResult RedactFormBody(String text, IReadOnlySet<String> sensitive)
    {
        String[] pairs = text.Split('&');
        for (Int32 i = 0; i < pairs.Length; i++)
        {
            String[] part = pairs[i].Split('=', 2);
            String key = Uri.UnescapeDataString(part[0].Replace('+', ' '));
            if (sensitive.Contains(key)) pairs[i] = part[0] + "=" + RedactedValue;
        }
        return new(String.Join('&', pairs), "captured");
    }

    private static BodyCaptureResult RedactXmlBody(String text, IReadOnlySet<String> sensitive)
    {
        XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
        XmlDocument document = new() { XmlResolver = null };
        using (XmlReader reader = XmlReader.Create(new StringReader(text), settings)) document.Load(reader);
        RedactXml(document.DocumentElement, sensitive);
        return new(document.OuterXml, "captured");
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
                    if (sensitive.Contains(property.Name)) writer.WriteStringValue(RedactedValue); else WriteJson(property.Value, writer, sensitive);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement child in element.EnumerateArray())
                {
                    WriteJson(child, writer, sensitive);
                }
                writer.WriteEndArray();
                break;
            default: element.WriteTo(writer); break;
        }
    }

    private static void RedactXml(XmlNode? node, IReadOnlySet<String> sensitive)
    {
        if (node is not XmlElement element) return;
        foreach (XmlAttribute attribute in element.Attributes)
            if (sensitive.Contains(attribute.LocalName)) attribute.Value = RedactedValue;
        if (sensitive.Contains(element.LocalName))
        {
            element.InnerText = RedactedValue;
            return;
        }
        foreach (XmlNode child in element.ChildNodes) RedactXml(child, sensitive);
    }

    private enum BodyFormat
    {
        Json,
        Form,
        Xml
    }
}
