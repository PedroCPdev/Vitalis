using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Vitalis.Observability;
using Vitalis.Observability.HealthChecks;

namespace Vitalis.Tests.Unit.Observabilidade;

/// <summary>
/// Testes do health check de serviços externos. O <see cref="HttpMessageHandler"/> é
/// mockado para simular respostas do backend Java sem nenhuma chamada de rede real.
/// </summary>
public class ExternalServiceHealthCheckTests
{
    private const string UrlDoServico = "http://localhost:8080/actuator/health";

    [Fact]
    public async Task CheckHealthAsync_QuandoOServicoRespondeComSucesso_RetornaHealthy()
    {
        // Arrange
        var check = CriarCheck(RespostaComStatus(HttpStatusCode.OK));

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, resultado.Status);
        Assert.Equal(200, resultado.Data["statusCode"]);
    }

    [Fact]
    public async Task CheckHealthAsync_QuandoOServicoNaoCriticoRespondeComErro_RetornaDegraded()
    {
        // Arrange
        var check = CriarCheck(RespostaComStatus(HttpStatusCode.ServiceUnavailable), critical: false);

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Degraded, resultado.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_QuandoOServicoCriticoRespondeComErro_RetornaUnhealthy()
    {
        // Arrange
        var check = CriarCheck(RespostaComStatus(HttpStatusCode.InternalServerError), critical: true);

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, resultado.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_QuandoOServicoNaoCriticoEstaInacessivel_RetornaDegradedComOErro()
    {
        // Arrange
        var check = CriarCheck(new HttpRequestException("Connection refused"), critical: false);

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Degraded, resultado.Status);
        Assert.IsType<HttpRequestException>(resultado.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_QuandoOServicoCriticoEstaInacessivel_RetornaUnhealthy()
    {
        // Arrange
        var check = CriarCheck(new HttpRequestException("Connection refused"), critical: true);

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, resultado.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_QuandoARequisicaoExcedeOTimeout_RetornaDegradedSemPropagarAExcecao()
    {
        // Arrange
        var check = CriarCheck(new TaskCanceledException("Tempo limite excedido"), critical: false);

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Degraded, resultado.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_EmQualquerCenario_PublicaNomeEUrlDoServicoNosDados()
    {
        // Arrange
        var check = CriarCheck(RespostaComStatus(HttpStatusCode.OK));

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal("pethub-java", resultado.Data["service"]);
        Assert.Equal(UrlDoServico, resultado.Data["url"]);
    }

    private static HttpResponseMessage RespostaComStatus(HttpStatusCode status) => new(status);

    private static ExternalServiceHealthCheck CriarCheck(object respostaOuExcecao, bool critical = false)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var setup = handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());

        if (respostaOuExcecao is Exception excecao)
            setup.ThrowsAsync(excecao);
        else
            setup.ReturnsAsync((HttpResponseMessage)respostaOuExcecao);

        handler.Protected().Setup("Dispose", ItExpr.IsAny<bool>());

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(ExternalServiceHealthCheck.HealthCheckClientName))
            .Returns(() => new HttpClient(handler.Object, disposeHandler: false));

        var options = new ExternalServiceOptions
        {
            Name = "pethub-java",
            Url = UrlDoServico,
            TimeoutSeconds = 2,
            Critical = critical
        };

        return new ExternalServiceHealthCheck(
            factory.Object, options, NullLogger<ExternalServiceHealthCheck>.Instance);
    }
}
