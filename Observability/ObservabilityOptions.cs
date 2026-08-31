namespace Vitalis.Observability;

/// <summary>
/// Configurações da camada de monitoramento e observabilidade, mapeadas
/// a partir da seção "Observability" do appsettings.json.
/// </summary>
public class ObservabilityOptions
{
    public const string SectionName = "Observability";

    /// <summary>Nome lógico do serviço usado em traces, métricas e logs.</summary>
    public string ServiceName { get; set; } = "vitalis-api";

    /// <summary>Versão do serviço publicada nos recursos do OpenTelemetry.</summary>
    public string ServiceVersion { get; set; } = "3.0.0";

    /// <summary>Caminho (com rolling diário) do arquivo de log gerado pelo Serilog.</summary>
    public string LogFilePath { get; set; } = "logs/vitalis-.log";

    /// <summary>Quantidade de arquivos de log diários mantidos em disco.</summary>
    public int LogFileRetainedFileCount { get; set; } = 7;

    /// <summary>Acima deste tempo (ms) a requisição é registrada como Warning.</summary>
    public int SlowRequestThresholdMs { get; set; } = 1000;

    public TracingOptions Tracing { get; set; } = new();

    /// <summary>Serviços externos verificados pelos health checks.</summary>
    public List<ExternalServiceOptions> ExternalServices { get; set; } = [];
}

public class TracingOptions
{
    /// <summary>Exporta os spans no console — útil em desenvolvimento.</summary>
    public bool ConsoleExporter { get; set; }

    /// <summary>Endpoint OTLP (Jaeger, Tempo, Application Insights…). Vazio desabilita o exporter.</summary>
    public string OtlpEndpoint { get; set; } = string.Empty;
}

public class ExternalServiceOptions
{
    /// <summary>Nome do health check registrado para o serviço.</summary>
    public string Name { get; set; } = null!;

    /// <summary>URL consultada para verificar a disponibilidade do serviço.</summary>
    public string Url { get; set; } = null!;

    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>Quando true, a indisponibilidade derruba a saúde da API (Unhealthy em vez de Degraded).</summary>
    public bool Critical { get; set; }
}
