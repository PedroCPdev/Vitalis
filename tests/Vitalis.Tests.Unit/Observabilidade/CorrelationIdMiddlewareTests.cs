using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Vitalis.Observability;

namespace Vitalis.Tests.Unit.Observabilidade;

/// <summary>Testes da correlação de requisições usada no logging estruturado.</summary>
public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_QuandoOClienteEnviaOHeaderDeCorrelacao_ReaproveitaOValorRecebido()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "correlacao-do-cliente";
        var middleware = CriarMiddleware();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("correlacao-do-cliente", context.Items[CorrelationIdMiddleware.HeaderName]);
        Assert.Equal("correlacao-do-cliente", context.TraceIdentifier);
    }

    [Fact]
    public async Task InvokeAsync_QuandoOClienteNaoEnviaOHeader_GeraUmNovoIdentificador()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = CriarMiddleware();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var correlationId = Assert.IsType<string>(context.Items[CorrelationIdMiddleware.HeaderName]);
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
    }

    [Fact]
    public async Task InvokeAsync_ComHeaderEmBranco_GeraUmNovoIdentificador()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "   ";
        var middleware = CriarMiddleware();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.NotEqual("   ", context.Items[CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task InvokeAsync_EmQualquerRequisicao_ChamaOProximoMiddlewareDaCadeia()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var chamouOProximo = false;
        var middleware = new CorrelationIdMiddleware(
            _ => { chamouOProximo = true; return Task.CompletedTask; },
            NullLogger<CorrelationIdMiddleware>.Instance);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(chamouOProximo);
    }

    [Fact]
    public void ResolveCorrelationId_ComHeaderContendoEspacos_RemoveOsEspacosDasBordas()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "  abc-123  ";

        // Act
        var correlationId = CorrelationIdMiddleware.ResolveCorrelationId(context);

        // Assert
        Assert.Equal("abc-123", correlationId);
    }

    [Fact]
    public void ResolveCorrelationId_EmDuasRequisicoesSemHeader_GeraIdentificadoresDistintos()
    {
        // Arrange & Act
        var primeiro = CorrelationIdMiddleware.ResolveCorrelationId(new DefaultHttpContext());
        var segundo = CorrelationIdMiddleware.ResolveCorrelationId(new DefaultHttpContext());

        // Assert
        Assert.NotEqual(primeiro, segundo);
    }

    private static CorrelationIdMiddleware CriarMiddleware()
        => new(_ => Task.CompletedTask, NullLogger<CorrelationIdMiddleware>.Instance);
}
