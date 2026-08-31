using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Vitalis.Observability;

/// <summary>
/// Cronometra cada requisição e publica as métricas de desempenho (tempo de resposta e
/// taxa de erros) tanto nos instrumentos do OpenTelemetry quanto no agregador em memória.
/// </summary>
public class RequestMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly VitalisMetrics _metrics;
    private readonly IMetricsRegistry _registry;
    private readonly ILogger<RequestMetricsMiddleware> _logger;
    private readonly int _slowRequestThresholdMs;

    public RequestMetricsMiddleware(
        RequestDelegate next,
        VitalisMetrics metrics,
        IMetricsRegistry registry,
        IOptions<ObservabilityOptions> options,
        ILogger<RequestMetricsMiddleware> logger)
    {
        _next = next;
        _metrics = metrics;
        _registry = registry;
        _logger = logger;
        _slowRequestThresholdMs = options.Value.SlowRequestThresholdMs;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var endpoint = ResolveEndpointName(context);
            var elapsed = stopwatch.Elapsed.TotalMilliseconds;
            var status = context.Response.StatusCode;

            _metrics.RecordRequest(endpoint, context.Request.Method, status, elapsed);
            _registry.Record(endpoint, context.Request.Method, status, elapsed);

            LogOutcome(context, endpoint, status, elapsed);
        }
    }

    private void LogOutcome(HttpContext context, string endpoint, int status, double elapsed)
    {
        if (status >= 500)
        {
            _logger.LogError(
                "{Method} {Endpoint} respondeu {StatusCode} em {ElapsedMilliseconds:0.000} ms",
                context.Request.Method, endpoint, status, elapsed);
        }
        else if (status >= 400 || elapsed > _slowRequestThresholdMs)
        {
            _logger.LogWarning(
                "{Method} {Endpoint} respondeu {StatusCode} em {ElapsedMilliseconds:0.000} ms",
                context.Request.Method, endpoint, status, elapsed);
        }
        else
        {
            _logger.LogInformation(
                "{Method} {Endpoint} respondeu {StatusCode} em {ElapsedMilliseconds:0.000} ms",
                context.Request.Method, endpoint, status, elapsed);
        }
    }

    /// <summary>
    /// Usa o template da rota (ex.: <c>api/responsavel/{id}</c>) para evitar explosão de
    /// cardinalidade nas métricas; cai para o path bruto quando não há rota associada.
    /// </summary>
    internal static string ResolveEndpointName(HttpContext context)
    {
        var routePattern = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;
        if (!string.IsNullOrWhiteSpace(routePattern))
            return "/" + routePattern.TrimStart('/');

        var path = context.Request.Path.HasValue ? context.Request.Path.Value! : "/";
        return string.IsNullOrWhiteSpace(path) ? "/" : path;
    }
}
