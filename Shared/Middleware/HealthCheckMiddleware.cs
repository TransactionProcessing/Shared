using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shared.Middleware
{
    public class HealthCheckMiddleware
    {
        public static Task WriteResponse(HttpContext context,
                                         HealthReport healthReport) {
            context.Response.ContentType = "application/json; charset=utf-8";

            JsonWriterOptions options = new JsonWriterOptions {
                                                                  Indented = true
                                                              };
            using MemoryStream memoryStream = new MemoryStream();
            using(Utf8JsonWriter jsonWriter = new Utf8JsonWriter(memoryStream, options)) {
                jsonWriter.WriteStartObject();
                jsonWriter.WriteString("status", healthReport.Status.ToString());
                jsonWriter.WriteString("totalDuration", healthReport.TotalDuration.ToString());
                jsonWriter.WriteStartArray("results");

                foreach (KeyValuePair<String, HealthReportEntry> healthReportEntry in healthReport.Entries) {
                    jsonWriter.WriteStartObject();
                    jsonWriter.WriteString("name", healthReportEntry.Key);
                    jsonWriter.WriteString("status", healthReportEntry.Value.Status.ToString());
                    jsonWriter.WriteString("duration", healthReportEntry.Value.Duration.ToString());
                    jsonWriter.WriteString("description", healthReportEntry.Value.Description);
                    jsonWriter.WriteStartArray("tags");
                    foreach (String valueTag in healthReportEntry.Value.Tags) {
                        jsonWriter.WriteStringValue(valueTag);
                    }

                    jsonWriter.WriteEndArray();
                    jsonWriter.WriteEndObject();
                }

                jsonWriter.WriteEndArray();
                jsonWriter.WriteEndObject();
            }

            return context.Response.WriteAsync(Encoding.UTF8.GetString(memoryStream.ToArray()));
        }
    }
}
