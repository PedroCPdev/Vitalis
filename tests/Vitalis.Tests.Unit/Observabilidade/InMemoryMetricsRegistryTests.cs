using Vitalis.Observability;

namespace Vitalis.Tests.Unit.Observabilidade;

/// <summary>Testes do agregador de métricas de desempenho exposto em <c>/metrics/summary</c>.</summary>
public class InMemoryMetricsRegistryTests
{
    [Fact]
    public void GetSnapshot_SemNenhumaRequisicaoRegistrada_RetornaContadoresZerados()
    {
        // Arrange
        var registro = new InMemoryMetricsRegistry();

        // Act
        var snapshot = registro.GetSnapshot();

        // Assert
        Assert.Equal(0, snapshot.TotalRequests);
        Assert.Equal(0, snapshot.TotalErrors);
        Assert.Equal(0d, snapshot.ErrorRate);
        Assert.Empty(snapshot.Endpoints);
    }

    [Fact]
    public void Record_ComRequisicoesDeSucessoEErro_CalculaATaxaDeErrosCorretamente()
    {
        // Arrange
        var registro = new InMemoryMetricsRegistry();

        // Act
        registro.Record("/api/responsavel", "GET", 200, 10);
        registro.Record("/api/responsavel", "GET", 200, 20);
        registro.Record("/api/responsavel", "GET", 500, 30);
        registro.Record("/api/responsavel", "GET", 404, 40);
        var snapshot = registro.GetSnapshot();

        // Assert
        Assert.Equal(4, snapshot.TotalRequests);
        Assert.Equal(2, snapshot.TotalErrors);
        Assert.Equal(0.5d, snapshot.ErrorRate);
    }

    [Fact]
    public void Record_ComVariasLatencias_CalculaOTempoMedioDeResposta()
    {
        // Arrange
        var registro = new InMemoryMetricsRegistry();

        // Act
        registro.Record("/api/lembretes", "GET", 200, 100);
        registro.Record("/api/lembretes", "GET", 200, 200);
        registro.Record("/api/lembretes", "GET", 200, 300);
        var snapshot = registro.GetSnapshot();

        // Assert
        Assert.Equal(200d, snapshot.AverageResponseTimeMs);
    }

    [Fact]
    public void Record_ComMetodosDiferentesNaMesmaRota_AgrupaAsMetricasSeparadamente()
    {
        // Arrange
        var registro = new InMemoryMetricsRegistry();

        // Act
        registro.Record("/api/responsavel", "GET", 200, 10);
        registro.Record("/api/responsavel", "POST", 201, 20);
        var snapshot = registro.GetSnapshot();

        // Assert
        Assert.Equal(2, snapshot.Endpoints.Count);
        Assert.Contains(snapshot.Endpoints, e => e.Endpoint == "GET /api/responsavel");
        Assert.Contains(snapshot.Endpoints, e => e.Endpoint == "POST /api/responsavel");
    }

    [Fact]
    public void GetSnapshot_ComVariosEndpoints_RegistraMinimoEMaximoPorEndpoint()
    {
        // Arrange
        var registro = new InMemoryMetricsRegistry();
        registro.Record("/api/responsavel", "GET", 200, 15);
        registro.Record("/api/responsavel", "GET", 200, 85);

        // Act
        var endpoint = registro.GetSnapshot().Endpoints.Single();

        // Assert
        Assert.Equal(15d, endpoint.MinResponseTimeMs);
        Assert.Equal(85d, endpoint.MaxResponseTimeMs);
        Assert.Equal(2, endpoint.TotalRequests);
    }

    [Fact]
    public void GetSnapshot_ComVariosEndpoints_OrdenaDoMaisAcessadoParaOMenosAcessado()
    {
        // Arrange
        var registro = new InMemoryMetricsRegistry();
        registro.Record("/api/lembretes", "GET", 200, 10);
        registro.Record("/api/responsavel", "GET", 200, 10);
        registro.Record("/api/responsavel", "GET", 200, 10);

        // Act
        var snapshot = registro.GetSnapshot();

        // Assert
        Assert.Equal("GET /api/responsavel", snapshot.Endpoints[0].Endpoint);
    }

    [Fact]
    public void Reset_AposRegistrarRequisicoes_LimpaTodosOsContadores()
    {
        // Arrange
        var registro = new InMemoryMetricsRegistry();
        registro.Record("/api/responsavel", "GET", 200, 10);

        // Act
        registro.Reset();
        var snapshot = registro.GetSnapshot();

        // Assert
        Assert.Equal(0, snapshot.TotalRequests);
        Assert.Empty(snapshot.Endpoints);
    }

    [Fact]
    public void Record_ComTempoDeRespostaNegativo_LancaArgumentOutOfRangeException()
    {
        // Arrange
        var registro = new InMemoryMetricsRegistry();

        // Act
        var excecao = Record.Exception(() => registro.Record("/api/responsavel", "GET", 200, -1));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(excecao);
    }

    [Fact]
    public void Record_ComEndpointVazio_LancaArgumentException()
    {
        // Arrange
        var registro = new InMemoryMetricsRegistry();

        // Act
        var excecao = Record.Exception(() => registro.Record("  ", "GET", 200, 10));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(excecao);
    }

    [Fact]
    public void Percentile_ComAmostrasConhecidas_RetornaOValorDoPercentil95()
    {
        // Arrange
        var amostras = Enumerable.Range(1, 100).Select(i => (double)i).ToArray();

        // Act
        var p95 = InMemoryMetricsRegistry.Percentile(amostras, 95);

        // Assert
        Assert.Equal(95d, p95);
    }

    [Fact]
    public void Percentile_SemAmostras_RetornaZero()
    {
        // Arrange
        var amostras = Array.Empty<double>();

        // Act
        var p95 = InMemoryMetricsRegistry.Percentile(amostras, 95);

        // Assert
        Assert.Equal(0d, p95);
    }

    [Fact]
    public void Record_AcimaDoLimiteDeAmostras_MantemAContagemTotalDeRequisicoes()
    {
        // Arrange
        var registro = new InMemoryMetricsRegistry();
        var totalDeRequisicoes = InMemoryMetricsRegistry.MaxSamplesPerEndpoint + 50;

        // Act
        for (var i = 0; i < totalDeRequisicoes; i++)
            registro.Record("/api/responsavel", "GET", 200, i);
        var snapshot = registro.GetSnapshot();

        // Assert
        Assert.Equal(totalDeRequisicoes, snapshot.TotalRequests);
    }

    [Fact]
    public void GetSnapshot_ComUmTimeProviderControlado_CalculaOUptimeEmSegundos()
    {
        // Arrange
        var tempo = new FakeTimeProvider(new DateTimeOffset(2026, 5, 1, 10, 0, 0, TimeSpan.Zero));
        var registro = new InMemoryMetricsRegistry(tempo);

        // Act
        tempo.Advance(TimeSpan.FromSeconds(30));
        var snapshot = registro.GetSnapshot();

        // Assert
        Assert.Equal(30d, snapshot.UptimeSeconds);
    }
}
