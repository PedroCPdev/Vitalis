namespace Vitalis.Observability;

/// <summary>
/// Agregador em memória das métricas de desempenho expostas em /metrics/summary.
/// Complementa o exporter Prometheus com uma visão legível por humanos.
/// </summary>
public interface IMetricsRegistry
{
    /// <summary>Contabiliza uma requisição já finalizada.</summary>
    void Record(string endpoint, string method, int statusCode, double elapsedMilliseconds);

    /// <summary>Devolve o retrato atual das métricas acumuladas.</summary>
    MetricsSnapshot GetSnapshot();

    /// <summary>Zera todos os contadores acumulados.</summary>
    void Reset();
}
