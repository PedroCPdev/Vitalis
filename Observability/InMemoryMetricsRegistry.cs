using System.Collections.Concurrent;

namespace Vitalis.Observability;

/// <summary>
/// Implementação thread-safe de <see cref="IMetricsRegistry"/>. Mantém contadores por endpoint
/// e uma janela deslizante das últimas amostras de latência para o cálculo do percentil 95.
/// </summary>
public sealed class InMemoryMetricsRegistry : IMetricsRegistry
{
    /// <summary>Número máximo de amostras de latência guardadas por endpoint.</summary>
    public const int MaxSamplesPerEndpoint = 500;

    private readonly ConcurrentDictionary<string, EndpointCounters> _endpoints = new();
    private readonly TimeProvider _timeProvider;
    private readonly long _startedAtTicks;

    public InMemoryMetricsRegistry() : this(TimeProvider.System) { }

    public InMemoryMetricsRegistry(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _startedAtTicks = timeProvider.GetUtcNow().UtcTicks;
    }

    public void Record(string endpoint, string method, int statusCode, double elapsedMilliseconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(method);

        if (elapsedMilliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(elapsedMilliseconds),
                "O tempo de resposta não pode ser negativo.");

        var key = $"{method.ToUpperInvariant()} {endpoint}";
        var counters = _endpoints.GetOrAdd(key, _ => new EndpointCounters());
        counters.Add(statusCode, elapsedMilliseconds);
    }

    public MetricsSnapshot GetSnapshot()
    {
        var perEndpoint = _endpoints
            .Select(entry => entry.Value.ToMetrics(entry.Key))
            .OrderByDescending(m => m.TotalRequests)
            .ThenBy(m => m.Endpoint, StringComparer.Ordinal)
            .ToList();

        var totalRequests = perEndpoint.Sum(m => m.TotalRequests);
        var totalErrors = perEndpoint.Sum(m => m.TotalErrors);

        var allSamples = _endpoints.Values.SelectMany(c => c.SnapshotSamples()).ToArray();
        var average = allSamples.Length == 0 ? 0d : allSamples.Average();
        var p95 = Percentile(allSamples, 95);

        var now = _timeProvider.GetUtcNow();
        var uptime = (now.UtcTicks - _startedAtTicks) / (double)TimeSpan.TicksPerSecond;

        return new MetricsSnapshot(
            CollectedAt: now,
            UptimeSeconds: Math.Round(uptime, 3),
            TotalRequests: totalRequests,
            TotalErrors: totalErrors,
            ErrorRate: Rate(totalErrors, totalRequests),
            AverageResponseTimeMs: Math.Round(average, 3),
            P95ResponseTimeMs: Math.Round(p95, 3),
            Endpoints: perEndpoint);
    }

    public void Reset() => _endpoints.Clear();

    internal static double Rate(long errors, long total)
        => total == 0 ? 0d : Math.Round((double)errors / total, 4);

    /// <summary>Percentil pelo método do vizinho mais próximo (nearest-rank).</summary>
    internal static double Percentile(double[] samples, int percentile)
    {
        if (samples.Length == 0) return 0d;

        var ordered = samples.Order().ToArray();
        var rank = (int)Math.Ceiling(percentile / 100d * ordered.Length);
        var index = Math.Clamp(rank - 1, 0, ordered.Length - 1);
        return ordered[index];
    }

    private sealed class EndpointCounters
    {
        private readonly Lock _gate = new();
        private readonly double[] _samples = new double[MaxSamplesPerEndpoint];
        private int _sampleCount;
        private int _nextSampleIndex;
        private long _total;
        private long _errors;
        private double _sum;
        private double _min = double.MaxValue;
        private double _max = double.MinValue;

        public void Add(int statusCode, double elapsedMilliseconds)
        {
            lock (_gate)
            {
                _total++;
                if (statusCode >= 400) _errors++;

                _sum += elapsedMilliseconds;
                _min = Math.Min(_min, elapsedMilliseconds);
                _max = Math.Max(_max, elapsedMilliseconds);

                _samples[_nextSampleIndex] = elapsedMilliseconds;
                _nextSampleIndex = (_nextSampleIndex + 1) % MaxSamplesPerEndpoint;
                if (_sampleCount < MaxSamplesPerEndpoint) _sampleCount++;
            }
        }

        public double[] SnapshotSamples()
        {
            lock (_gate)
            {
                return _samples.Take(_sampleCount).ToArray();
            }
        }

        public EndpointMetrics ToMetrics(string endpoint)
        {
            lock (_gate)
            {
                var average = _total == 0 ? 0d : _sum / _total;
                var samples = _samples.Take(_sampleCount).ToArray();

                return new EndpointMetrics(
                    Endpoint: endpoint,
                    TotalRequests: _total,
                    TotalErrors: _errors,
                    ErrorRate: Rate(_errors, _total),
                    AverageResponseTimeMs: Math.Round(average, 3),
                    MinResponseTimeMs: _total == 0 ? 0d : Math.Round(_min, 3),
                    MaxResponseTimeMs: _total == 0 ? 0d : Math.Round(_max, 3),
                    P95ResponseTimeMs: Math.Round(Percentile(samples, 95), 3));
            }
        }
    }
}
