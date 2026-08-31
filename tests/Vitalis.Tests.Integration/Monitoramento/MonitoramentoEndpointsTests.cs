using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Vitalis.Observability;
using Vitalis.Tests.Integration.Fixtures;

namespace Vitalis.Tests.Integration.Monitoramento;

/// <summary>
/// Testes de integração da camada de monitoramento: health checks, correlação de
/// requisições e exposição das métricas de desempenho.
/// </summary>
[Collection(VitalisApiCollection.Name)]
public class MonitoramentoEndpointsTests : IDisposable
{
    private readonly HttpClient _client;

    public MonitoramentoEndpointsTests(VitalisWebApplicationFactory factory)
        => _client = factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetHealth_ComServicoExternoNaoCriticoIndisponivel_Retorna200ComStatusDegraded()
    {
        // Arrange & Act
        var resposta = await _client.GetAsync("/health");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert: serviço externo não crítico fora do ar degrada, mas não derruba a API.
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("Degraded", corpo.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetHealth_ComAApiEOBancoDisponiveis_ReportaAmbosOsChecksComoHealthy()
    {
        // Arrange & Act
        var corpo = await _client.GetFromJsonAsync<JsonElement>("/health");
        var checks = corpo.GetProperty("checks").EnumerateArray()
            .ToDictionary(c => c.GetProperty("name").GetString()!, c => c.GetProperty("status").GetString());

        // Assert
        Assert.Equal("Healthy", checks["api"]);
        Assert.Equal("Healthy", checks["oracle-database"]);
    }

    [Fact]
    public async Task GetHealth_ComServicoExternoForaDoAr_MarcaApenasAquelaVerificacaoComoDegraded()
    {
        // Arrange & Act
        var corpo = await _client.GetFromJsonAsync<JsonElement>("/health");
        var externo = corpo.GetProperty("checks").EnumerateArray()
            .Single(c => c.GetProperty("name").GetString() == VitalisWebApplicationFactory.ServicoExternoFake);

        // Assert
        Assert.Equal("Degraded", externo.GetProperty("status").GetString());
        Assert.Equal(VitalisWebApplicationFactory.UrlDoServicoExternoFake,
            externo.GetProperty("data").GetProperty("url").GetString());
    }

    [Fact]
    public async Task GetHealth_EmQualquerChamada_DetalhaOsChecksDaApiDoBancoEDoServicoExterno()
    {
        // Arrange & Act
        var corpo = await _client.GetFromJsonAsync<JsonElement>("/health");
        var nomes = corpo.GetProperty("checks").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ToList();

        // Assert
        Assert.Contains("api", nomes);
        Assert.Contains("oracle-database", nomes);
        Assert.Contains(VitalisWebApplicationFactory.ServicoExternoFake, nomes);
    }

    [Fact]
    public async Task GetHealthLive_EmQualquerChamada_VerificaApenasOProbeDeLiveness()
    {
        // Arrange & Act
        var resposta = await _client.GetAsync("/health/live");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var check = corpo.GetProperty("checks").EnumerateArray().Single();
        Assert.Equal("api", check.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetHealthReady_EmQualquerChamada_VerificaBancoEServicosExternos()
    {
        // Arrange & Act
        var corpo = await _client.GetFromJsonAsync<JsonElement>("/health/ready");
        var nomes = corpo.GetProperty("checks").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ToList();

        // Assert
        Assert.Contains("oracle-database", nomes);
        Assert.DoesNotContain("api", nomes);
    }

    [Fact]
    public async Task GetHealth_EmQualquerChamada_ReportaADuracaoTotalDaVerificacao()
    {
        // Arrange & Act
        var corpo = await _client.GetFromJsonAsync<JsonElement>("/health");

        // Assert
        Assert.True(corpo.GetProperty("totalDurationMs").GetDouble() >= 0);
    }

    [Fact]
    public async Task GetMetricsSummary_AposAlgumasRequisicoes_Retorna200ComOsContadoresAcumulados()
    {
        // Arrange
        await _client.GetAsync("/api/responsavel");
        await _client.GetAsync("/api/lembretes");

        // Act
        var resposta = await _client.GetAsync("/metrics/summary");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.True(corpo.GetProperty("totalRequests").GetInt64() > 0);
        Assert.True(corpo.GetProperty("endpoints").GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetMetricsSummary_AposUmaRequisicaoComErro_ContabilizaATaxaDeErros()
    {
        // Arrange
        await _client.GetAsync("/api/responsavel/999999");

        // Act
        var corpo = await _client.GetFromJsonAsync<JsonElement>("/metrics/summary");

        // Assert
        Assert.True(corpo.GetProperty("totalErrors").GetInt64() > 0);
        Assert.True(corpo.GetProperty("errorRate").GetDouble() > 0);
    }

    [Fact]
    public async Task GetMetricsSummary_ParaUmaRotaParametrizada_AgrupaPeloTemplateDaRota()
    {
        // Arrange
        await _client.GetAsync("/api/responsavel/123");
        await _client.GetAsync("/api/responsavel/456");

        // Act
        var corpo = await _client.GetFromJsonAsync<JsonElement>("/metrics/summary");
        var rotas = corpo.GetProperty("endpoints").EnumerateArray()
            .Select(e => e.GetProperty("endpoint").GetString())
            .ToList();

        // Assert
        Assert.Contains(rotas, rota => rota is not null && rota.Contains("{id:long}"));
    }

    [Fact]
    public async Task GetMetrics_NoFormatoPrometheus_Retorna200ComAsMetricasDaAplicacao()
    {
        // Arrange
        await _client.GetAsync("/api/responsavel");

        // Act
        var resposta = await _client.GetAsync("/metrics");
        var corpo = await resposta.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("vitalis_requests_total", corpo);
    }

    [Fact]
    public async Task GetMetrics_NoFormatoPrometheus_ExpoeOHistogramaDeTempoDeResposta()
    {
        // Arrange
        await _client.GetAsync("/api/lembretes");

        // Act
        var corpo = await _client.GetStringAsync("/metrics");

        // Assert
        Assert.Contains("vitalis_request_duration", corpo);
    }

    [Fact]
    public async Task Requisicao_ComHeaderDeCorrelacao_EcoaOMesmoIdentificadorNaResposta()
    {
        // Arrange
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/responsavel");
        requisicao.Headers.Add(CorrelationIdMiddleware.HeaderName, "correlacao-de-teste-42");

        // Act
        var resposta = await _client.SendAsync(requisicao);

        // Assert
        Assert.Equal("correlacao-de-teste-42",
            resposta.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
    }

    [Fact]
    public async Task Requisicao_SemHeaderDeCorrelacao_GeraEDevolveUmIdentificadorNaResposta()
    {
        // Arrange & Act
        var resposta = await _client.GetAsync("/api/responsavel");

        // Assert
        var correlationId = resposta.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
    }

    [Fact]
    public async Task Requisicoes_EmSequencia_RecebemIdentificadoresDeCorrelacaoDistintos()
    {
        // Arrange & Act
        var primeira = await _client.GetAsync("/api/responsavel");
        var segunda = await _client.GetAsync("/api/responsavel");

        // Assert
        Assert.NotEqual(
            primeira.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single(),
            segunda.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
    }

    [Fact]
    public async Task GetSwagger_ComAApiNoAr_Retorna200ComODocumentoOpenApi()
    {
        // Arrange & Act
        var resposta = await _client.GetAsync("/swagger/v1/swagger.json");
        var corpo = await resposta.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains("Vitalis API", corpo);
    }
}
