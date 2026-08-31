using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Vitalis.Observability;
using Vitalis.Observability.HealthChecks;
using Vitalis.Tests.Unit.Fixtures;

namespace Vitalis.Tests.Unit.Observabilidade;

/// <summary>Testes dos health checks da própria API e da conectividade com o banco de dados.</summary>
[Collection(InMemoryDatabaseCollection.Name)]
public class HealthChecksTests
{
    private readonly InMemoryDatabaseFixture _fixture;

    public HealthChecksTests(InMemoryDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CheckHealthAsync_ComAApiEmExecucao_RetornaHealthy()
    {
        // Arrange
        var check = CriarApiHealthCheck();

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, resultado.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ComAApiEmExecucao_PublicaNomeVersaoEAmbienteNosDados()
    {
        // Arrange
        var check = CriarApiHealthCheck();

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal("vitalis-api", resultado.Data["service"]);
        Assert.Equal("3.0.0", resultado.Data["version"]);
        Assert.Equal("Testing", resultado.Data["environment"]);
        Assert.True(resultado.Data.ContainsKey("uptimeSeconds"));
    }

    [Fact]
    public async Task CheckHealthAsync_ComBancoAcessivel_RetornaHealthyComOProvider()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var check = new DatabaseHealthCheck(context, NullLogger<DatabaseHealthCheck>.Instance);

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, resultado.Status);
        Assert.Contains("InMemory", resultado.Data["provider"]!.ToString());
    }

    [Fact]
    public async Task CheckHealthAsync_QuandoOAcessoAoBancoFalha_RetornaUnhealthyComAExcecao()
    {
        // Arrange: um contexto já descartado reproduz uma falha de infraestrutura.
        var context = _fixture.CreateContext();
        await context.DisposeAsync();
        var check = new DatabaseHealthCheck(context, NullLogger<DatabaseHealthCheck>.Instance);

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, resultado.Status);
        Assert.NotNull(resultado.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_ComBancoAcessivel_ReportaALatenciaDaConexao()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var check = new DatabaseHealthCheck(context, NullLogger<DatabaseHealthCheck>.Instance);

        // Act
        var resultado = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.True(Convert.ToDouble(resultado.Data["latencyMs"]) >= 0);
    }

    private static ApiHealthCheck CriarApiHealthCheck()
    {
        var ambiente = new Mock<IHostEnvironment>();
        ambiente.SetupGet(a => a.EnvironmentName).Returns("Testing");

        var options = Options.Create(new ObservabilityOptions
        {
            ServiceName = "vitalis-api",
            ServiceVersion = "3.0.0"
        });

        return new ApiHealthCheck(ambiente.Object, options, TimeProvider.System);
    }
}
