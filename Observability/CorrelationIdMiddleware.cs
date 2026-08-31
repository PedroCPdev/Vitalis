using System.Diagnostics;
using Serilog.Context;

namespace Vitalis.Observability;

/// <summary>
/// Garante que toda requisição possua um identificador de correlação. O valor é lido do header
/// <c>X-Correlation-ID</c> (quando o chamador já o envia) ou gerado, sendo então propagado para
/// o log estruturado, para o span do OpenTelemetry e de volta ao cliente no header de resposta.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[HeaderName] = correlationId;
        context.TraceIdentifier = correlationId;

        Activity.Current?.SetTag("correlation.id", correlationId);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", Activity.Current?.TraceId.ToString() ?? string.Empty))
        {
            _logger.LogDebug("Requisição {Method} {Path} iniciada com correlação {CorrelationId}",
                context.Request.Method, context.Request.Path, correlationId);

            await _next(context);
        }
    }

    /// <summary>Reaproveita o header recebido ou cria um novo identificador.</summary>
    internal static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var header))
        {
            var received = header.ToString();
            if (!string.IsNullOrWhiteSpace(received))
                return received.Trim();
        }

        return Guid.NewGuid().ToString("N");
    }
}
