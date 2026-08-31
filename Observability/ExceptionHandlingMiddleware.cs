using System.Diagnostics;
using System.Text.Json;

namespace Vitalis.Observability;

/// <summary>
/// Converte exceções não tratadas em uma resposta JSON padronizada, registrando o erro
/// no log estruturado e marcando o span atual como falho para o tracing distribuído.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var value)
                ? value?.ToString() ?? context.TraceIdentifier
                : context.TraceIdentifier;

            _logger.LogError(ex,
                "Erro não tratado em {Method} {Path} (correlação {CorrelationId})",
                context.Request.Method, context.Request.Path, correlationId);

            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json; charset=utf-8";

            var payload = JsonSerializer.Serialize(new
            {
                erro = "Erro interno no servidor",
                correlationId,
                timestamp = DateTimeOffset.UtcNow
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
