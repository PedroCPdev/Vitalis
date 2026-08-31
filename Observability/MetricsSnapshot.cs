namespace Vitalis.Observability;

/// <summary>Métricas de desempenho agregadas de um endpoint específico.</summary>
public sealed record EndpointMetrics(
    string Endpoint,
    long TotalRequests,
    long TotalErrors,
    double ErrorRate,
    double AverageResponseTimeMs,
    double MinResponseTimeMs,
    double MaxResponseTimeMs,
    double P95ResponseTimeMs);

/// <summary>Retrato instantâneo das métricas de desempenho da API.</summary>
public sealed record MetricsSnapshot(
    DateTimeOffset CollectedAt,
    double UptimeSeconds,
    long TotalRequests,
    long TotalErrors,
    double ErrorRate,
    double AverageResponseTimeMs,
    double P95ResponseTimeMs,
    IReadOnlyList<EndpointMetrics> Endpoints);
