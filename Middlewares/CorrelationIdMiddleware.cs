// Importa abstrações de contexto do ASP.NET Core
using Microsoft.AspNetCore.Http;
// Importa o contexto de logs do Serilog
using Serilog.Context;

// Declara o namespace dos middlewares da aplicação
namespace Vitalis.Middlewares;

// Middleware responsável pela gestão e propagação do Correlation ID
public class CorrelationIdMiddleware
{
    // Armazena o próximo delegate da pipeline de execução HTTP
    private readonly RequestDelegate _next;

    // Define a chave padrão do cabeçalho HTTP para rastreabilidade
    public const string CorrelationIdHeader = "X-Correlation-ID";

    // Construtor que recebe a referência para o próximo middleware
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    // Método invocado automaticamente a cada requisição HTTP recebida
    public async Task InvokeAsync(HttpContext context)
    {
        // Obtém o Correlation ID do cabeçalho da requisição ou gera um novo GUID
        string correlationId =
            context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        // Insere o Correlation ID nos cabeçalhos da resposta HTTP
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Injeta a propriedade 'CorrelationId' no contexto de logs durante a execução da requisição
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Transfere o controle para o próximo middleware na pipeline
            await _next(context);
        }
    }
}
