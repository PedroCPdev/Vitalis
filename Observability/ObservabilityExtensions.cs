using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Vitalis.Observability.HealthChecks;

namespace Vitalis.Observability;

/// <summary>
/// Registra e ativa a camada de monitoramento e observabilidade da API:
/// logging estruturado (Serilog), health checks e tracing/métricas (OpenTelemetry).
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>Tag dos health checks que compõem o probe de liveness.</summary>
    public const string LiveTag = "live";

    /// <summary>Tag dos health checks que compõem o probe de readiness.</summary>
    public const string ReadyTag = "ready";

    /// <summary>Registra os serviços de observabilidade no contêiner de dependências.</summary>
    public static WebApplicationBuilder AddVitalisObservability(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<ObservabilityOptions>()
            .Bind(builder.Configuration.GetSection(ObservabilityOptions.SectionName))
            .ValidateDataAnnotations();

        var options = builder.Configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        builder.Services.TryAddTimeProvider();
        builder.Services.AddSingleton<VitalisMetrics>();
        builder.Services.AddSingleton<IMetricsRegistry, InMemoryMetricsRegistry>();
        builder.Services.AddHttpClient(ExternalServiceHealthCheck.HealthCheckClientName);

        builder.AddStructuredLogging(options);
        builder.Services.AddVitalisHealthChecks(options);
        builder.Services.AddVitalisTracingAndMetrics(options);

        return builder;
    }

    /// <summary>Configura o logging estruturado com Serilog (console + arquivo com rolling diário).</summary>
    public static WebApplicationBuilder AddStructuredLogging(
        this WebApplicationBuilder builder, ObservabilityOptions options)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Service", options.ServiceName)
                .Enrich.WithProperty("Version", options.ServiceVersion)
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.File(
                    path: options.LogFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: options.LogFileRetainedFileCount,
                    outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        });

        return builder;
    }

    /// <summary>
    /// Registra os health checks da API, do banco de dados Oracle e dos serviços externos
    /// configurados na seção <c>Observability:ExternalServices</c>.
    /// </summary>
    public static IServiceCollection AddVitalisHealthChecks(
        this IServiceCollection services, ObservabilityOptions options)
    {
        var healthChecks = services.AddHealthChecks();

        healthChecks.AddCheck<ApiHealthCheck>(
            name: "api",
            failureStatus: HealthStatus.Unhealthy,
            tags: [LiveTag, "self"]);

        healthChecks.AddCheck<DatabaseHealthCheck>(
            name: "oracle-database",
            failureStatus: HealthStatus.Unhealthy,
            tags: [ReadyTag, "db"]);

        foreach (var externalService in options.ExternalServices)
        {
            var serviceOptions = externalService;

            healthChecks.Add(new HealthCheckRegistration(
                name: serviceOptions.Name,
                factory: sp => new ExternalServiceHealthCheck(
                    sp.GetRequiredService<IHttpClientFactory>(),
                    serviceOptions,
                    sp.GetRequiredService<ILogger<ExternalServiceHealthCheck>>()),
                failureStatus: serviceOptions.Critical ? HealthStatus.Unhealthy : HealthStatus.Degraded,
                tags: [ReadyTag, "external"]));
        }

        return services;
    }

    /// <summary>Configura o tracing distribuído e as métricas de desempenho com OpenTelemetry.</summary>
    public static IServiceCollection AddVitalisTracingAndMetrics(
        this IServiceCollection services, ObservabilityOptions options)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(options.ServiceName, serviceVersion: options.ServiceVersion)
                .AddTelemetrySdk())
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(VitalisMetrics.ActivitySourceName)
                    .AddSource("Microsoft.EntityFrameworkCore")
                    .AddAspNetCoreInstrumentation(instrumentation =>
                    {
                        instrumentation.RecordException = true;
                        // O tráfego dos próprios endpoints de monitoramento não vira trace.
                        instrumentation.Filter = httpContext =>
                            !httpContext.Request.Path.StartsWithSegments("/health") &&
                            !httpContext.Request.Path.StartsWithSegments("/metrics");
                    })
                    .AddHttpClientInstrumentation();

                if (options.Tracing.ConsoleExporter)
                    tracing.AddConsoleExporter();

                if (!string.IsNullOrWhiteSpace(options.Tracing.OtlpEndpoint))
                    tracing.AddOtlpExporter(exporter =>
                        exporter.Endpoint = new Uri(options.Tracing.OtlpEndpoint));
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(VitalisMetrics.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddPrometheusExporter();

                if (!string.IsNullOrWhiteSpace(options.Tracing.OtlpEndpoint))
                    metrics.AddOtlpExporter(exporter =>
                        exporter.Endpoint = new Uri(options.Tracing.OtlpEndpoint));
            });

        return services;
    }

    /// <summary>
    /// Ativa os middlewares de observabilidade e mapeia os endpoints de monitoramento:
    /// <c>/health</c>, <c>/health/live</c>, <c>/health/ready</c>, <c>/metrics</c> e <c>/metrics/summary</c>.
    /// </summary>
    public static WebApplication UseVitalisObservability(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestMetricsMiddleware>();

        app.UseSerilogRequestLogging(logging =>
        {
            logging.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0000} ms";

            logging.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconhecido");
                diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
            };
        });

        return app;
    }

    /// <summary>Mapeia os endpoints de health check e de métricas.</summary>
    public static WebApplication MapVitalisMonitoringEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteResponse
        }).AllowAnonymous();

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(LiveTag),
            ResponseWriter = HealthCheckResponseWriter.WriteResponse
        }).AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyTag),
            ResponseWriter = HealthCheckResponseWriter.WriteResponse
        }).AllowAnonymous();

        // Formato de scraping do Prometheus (OpenTelemetry).
        app.MapPrometheusScrapingEndpoint("/metrics").AllowAnonymous();

        // Visão resumida e legível das métricas de desempenho.
        app.MapGet("/metrics/summary", (IMetricsRegistry registry) => Results.Ok(registry.GetSnapshot()))
            .WithName("MetricsSummary")
            .AllowAnonymous();

        return app;
    }

    private static IServiceCollection TryAddTimeProvider(this IServiceCollection services)
    {
        if (services.All(descriptor => descriptor.ServiceType != typeof(TimeProvider)))
            services.AddSingleton(TimeProvider.System);

        return services;
    }
}
