using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vitalis.Observability;

namespace Vitalis.Tests.Unit.Observabilidade;

/// <summary>Testes do middleware que coleta as métricas de desempenho das requisições.</summary>
public class RequestMetricsMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ComRequisicaoBemSucedida_RegistraAMetricaSemErro()
    {
        // Arrange
        var registro = new InMemoryMetricsRegistry();
        var context = CriarContexto("/api/responsavel", "GET", statusCode: 200);
        var middleware = CriarMiddleware(registro, _ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var snapshot = registro.GetSnapshot();
        Assert.Equal(1, snapshot.TotalRequests);
        Assert.Equal(0, snapshot.TotalErrors);
    }

    [Fact]
    public async Task InvokeAsync_ComRespostaDeErro_ContabilizaARequisicaoComoErro()
    {
        // Arrange
        var registro = new InMemoryMetricsRegistry();
        var context = CriarContexto("/api/responsavel/404", "GET", statusCode: 404);
        var middleware = CriarMiddleware(registro, _ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(1, registro.GetSnapshot().TotalErrors);
    }

    [Fact]
    public async Task InvokeAsync_QuandoOProximoMiddlewareLancaExcecao_RegistraAMetricaEPropagaAExcecao()
    {
        // Arrange
        var registro = new InMemoryMetricsRegistry();
        var context = CriarContexto("/api/responsavel", "GET", statusCode: 500);
        var middleware = CriarMiddleware(registro, _ => throw new InvalidOperationException("falha"));

        // Act
        var excecao = await Record.ExceptionAsync(() => middleware.InvokeAsync(context));

        // Assert
        Assert.IsType<InvalidOperationException>(excecao);
        Assert.Equal(1, registro.GetSnapshot().TotalRequests);
    }

    [Fact]
    public void ResolveEndpointName_ComRotaRegistrada_UsaOTemplateDaRota()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/responsavel/42";
        context.SetEndpoint(new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("api/responsavel/{id:long}"),
            order: 0,
            new EndpointMetadataCollection(),
            displayName: "responsavel-por-id"));

        // Act
        var nome = RequestMetricsMiddleware.ResolveEndpointName(context);

        // Assert
        Assert.Equal("/api/responsavel/{id:long}", nome);
    }

    [Fact]
    public void ResolveEndpointName_SemRotaRegistrada_UsaOCaminhoDaRequisicao()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/caminho-desconhecido";

        // Act
        var nome = RequestMetricsMiddleware.ResolveEndpointName(context);

        // Assert
        Assert.Equal("/caminho-desconhecido", nome);
    }

    private static DefaultHttpContext CriarContexto(string path, string metodo, int statusCode)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Method = metodo;
        context.Response.StatusCode = statusCode;
        return context;
    }

    private static RequestMetricsMiddleware CriarMiddleware(IMetricsRegistry registro, RequestDelegate proximo)
        => new(
            proximo,
            new VitalisMetrics(),
            registro,
            Options.Create(new ObservabilityOptions()),
            NullLogger<RequestMetricsMiddleware>.Instance);
}
