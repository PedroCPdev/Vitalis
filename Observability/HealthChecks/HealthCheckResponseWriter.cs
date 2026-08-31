using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Vitalis.Observability.HealthChecks;

/// <summary>
/// Serializa o resultado dos health checks em um JSON detalhado, com o status geral e o
/// detalhamento de cada verificação (duração, descrição, dados e erro).
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // Mantém a acentuação legível nas descrições em português.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsync(Serialize(report));
    }

    /// <summary>Converte o relatório em JSON — extraído para permitir teste unitário direto.</summary>
    public static string Serialize(HealthReport report)
    {
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 3),
            timestamp = DateTimeOffset.UtcNow,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 3),
                tags = entry.Value.Tags,
                data = entry.Value.Data.Count == 0 ? null : entry.Value.Data,
                error = entry.Value.Exception?.Message
            })
        };

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }
}
