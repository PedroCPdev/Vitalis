using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Vitalis.Observability.HealthChecks;

/// <summary>
/// Verifica a saúde do próprio processo da API: tempo no ar, memória alocada e ambiente.
/// Sinaliza <see cref="HealthStatus.Degraded"/> quando o consumo de memória gerenciada
/// ultrapassa o limite configurado.
/// </summary>
public class ApiHealthCheck : IHealthCheck
{
    /// <summary>Limite de memória gerenciada (MB) acima do qual a API é considerada degradada.</summary>
    public const long MemoryThresholdMegabytes = 1024;

    private readonly IHostEnvironment _environment;
    private readonly ObservabilityOptions _options;
    private readonly TimeProvider _timeProvider;

    public ApiHealthCheck(
        IHostEnvironment environment,
        IOptions<ObservabilityOptions> options,
        TimeProvider timeProvider)
    {
        _environment = environment;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var allocatedMegabytes = GC.GetTotalMemory(forceFullCollection: false) / (1024 * 1024);
        var uptime = _timeProvider.GetUtcNow() - Process.GetCurrentProcess().StartTime.ToUniversalTime();

        var data = new Dictionary<string, object>
        {
            ["service"] = _options.ServiceName,
            ["version"] = _options.ServiceVersion,
            ["environment"] = _environment.EnvironmentName,
            ["uptimeSeconds"] = Math.Round(uptime.TotalSeconds, 1),
            ["allocatedMemoryMb"] = allocatedMegabytes
        };

        var result = allocatedMegabytes > MemoryThresholdMegabytes
            ? HealthCheckResult.Degraded(
                $"API no ar, porém consumindo {allocatedMegabytes} MB de memória gerenciada.", data: data)
            : HealthCheckResult.Healthy("API respondendo normalmente.", data);

        return Task.FromResult(result);
    }
}
