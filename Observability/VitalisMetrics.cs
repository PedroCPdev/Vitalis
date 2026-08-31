using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Vitalis.Observability;

/// <summary>
/// Fonte única de traces (<see cref="ActivitySource"/>) e métricas (<see cref="Meter"/>) da aplicação.
/// Os instrumentos aqui declarados são coletados pelo OpenTelemetry e expostos em /metrics.
/// </summary>
public sealed class VitalisMetrics : IDisposable
{
    public const string ActivitySourceName = "Vitalis.Api";
    public const string MeterName = "Vitalis.Api";

    /// <summary>ActivitySource usado para criar spans manuais nas camadas da aplicação.</summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "3.0.0");

    private readonly Meter _meter;
    private readonly Counter<long> _requestsTotal;
    private readonly Counter<long> _errorsTotal;
    private readonly Histogram<double> _requestDuration;

    public VitalisMetrics()
    {
        _meter = new Meter(MeterName, "3.0.0");

        _requestsTotal = _meter.CreateCounter<long>(
            "vitalis.requests.total", "{request}",
            "Total de requisições HTTP processadas pela API");

        _errorsTotal = _meter.CreateCounter<long>(
            "vitalis.requests.errors.total", "{request}",
            "Total de requisições HTTP que resultaram em erro (status >= 400)");

        _requestDuration = _meter.CreateHistogram<double>(
            "vitalis.request.duration", "ms",
            "Tempo de resposta das requisições HTTP em milissegundos");
    }

    /// <summary>Registra uma requisição concluída nos instrumentos de métrica.</summary>
    public void RecordRequest(string endpoint, string method, int statusCode, double elapsedMilliseconds)
    {
        var tags = new TagList
        {
            { "http.route", endpoint },
            { "http.request.method", method },
            { "http.response.status_code", statusCode }
        };

        _requestsTotal.Add(1, tags);
        _requestDuration.Record(elapsedMilliseconds, tags);

        if (statusCode >= 400)
            _errorsTotal.Add(1, tags);
    }

    /// <summary>Inicia um span manual para instrumentar uma operação de negócio.</summary>
    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
        => ActivitySource.StartActivity(name, kind);

    public void Dispose() => _meter.Dispose();
}
