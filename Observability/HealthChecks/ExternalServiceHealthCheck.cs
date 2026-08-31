using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Vitalis.Observability.HealthChecks;

/// <summary>
/// Verifica a disponibilidade de um serviço externo (por exemplo, o backend Java do PetHub)
/// através de uma requisição HTTP GET. Serviços não críticos indisponíveis resultam em
/// <see cref="HealthStatus.Degraded"/>; críticos, em <see cref="HealthStatus.Unhealthy"/>.
/// </summary>
public class ExternalServiceHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExternalServiceOptions _options;
    private readonly ILogger<ExternalServiceHealthCheck> _logger;

    public ExternalServiceHealthCheck(
        IHttpClientFactory httpClientFactory,
        ExternalServiceOptions options,
        ILogger<ExternalServiceHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var data = new Dictionary<string, object>
        {
            ["service"] = _options.Name,
            ["url"] = _options.Url,
            ["critical"] = _options.Critical
        };

        try
        {
            using var client = _httpClientFactory.CreateClient(HealthCheckClientName);
            client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            using var response = await client.GetAsync(_options.Url, cancellationToken);
            stopwatch.Stop();

            data["statusCode"] = (int)response.StatusCode;
            data["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 3);

            if (response.IsSuccessStatusCode)
                return HealthCheckResult.Healthy($"Serviço '{_options.Name}' disponível.", data);

            _logger.LogWarning("Serviço externo {Service} respondeu {StatusCode}",
                _options.Name, (int)response.StatusCode);

            return Unavailable(
                $"Serviço '{_options.Name}' respondeu {(int)response.StatusCode}.", null, data);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            stopwatch.Stop();
            data["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 3);

            _logger.LogWarning(ex, "Serviço externo {Service} inacessível em {Url}",
                _options.Name, _options.Url);

            return Unavailable($"Serviço '{_options.Name}' inacessível.", ex, data);
        }
    }

    /// <summary>Nome do <see cref="HttpClient"/> nomeado usado pelos health checks externos.</summary>
    public const string HealthCheckClientName = "health-checks";

    private HealthCheckResult Unavailable(string message, Exception? exception, IReadOnlyDictionary<string, object> data)
        => _options.Critical
            ? HealthCheckResult.Unhealthy(message, exception, data)
            : HealthCheckResult.Degraded(message, exception, data);
}
