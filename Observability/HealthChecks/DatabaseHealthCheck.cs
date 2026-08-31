using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Vitalis.Observability.HealthChecks;

/// <summary>
/// Verifica a conectividade com o banco de dados Oracle através do <see cref="AppDbContext"/>,
/// medindo a latência do handshake e reportando o provider em uso.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    /// <summary>Acima desta latência (ms) o banco é reportado como degradado.</summary>
    public const int SlowConnectionThresholdMs = 2000;

    /// <summary>Tempo máximo de espera pelo handshake antes de considerar o banco indisponível.</summary>
    public static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(5);

    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(AppDbContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ConnectionTimeout);

        try
        {
            var canConnect = await _context.Database.CanConnectAsync(timeout.Token);
            stopwatch.Stop();

            var data = new Dictionary<string, object>
            {
                ["provider"] = _context.Database.ProviderName ?? "desconhecido",
                ["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 3)
            };

            if (!canConnect)
            {
                _logger.LogError("Health check do banco falhou: conexão recusada pelo provider {Provider}",
                    data["provider"]);

                return HealthCheckResult.Unhealthy(
                    "Não foi possível estabelecer conexão com o banco de dados.", data: data);
            }

            if (stopwatch.Elapsed.TotalMilliseconds > SlowConnectionThresholdMs)
            {
                _logger.LogWarning("Banco de dados respondeu lentamente: {LatencyMs} ms", data["latencyMs"]);

                return HealthCheckResult.Degraded(
                    $"Banco de dados acessível, porém lento ({data["latencyMs"]} ms).", data: data);
            }

            return HealthCheckResult.Healthy("Conexão com o banco de dados estabelecida.", data);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogError("Health check do banco de dados expirou após {TimeoutSeconds}s",
                ConnectionTimeout.TotalSeconds);

            return HealthCheckResult.Unhealthy(
                $"Tempo limite de {ConnectionTimeout.TotalSeconds}s excedido ao conectar no banco de dados.",
                data: new Dictionary<string, object>
                {
                    ["provider"] = _context.Database.ProviderName ?? "desconhecido",
                    ["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 3)
                });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Health check do banco de dados lançou exceção");

            return HealthCheckResult.Unhealthy(
                "Erro ao verificar a conexão com o banco de dados.",
                ex,
                new Dictionary<string, object>
                {
                    ["latencyMs"] = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 3)
                });
        }
    }
}
