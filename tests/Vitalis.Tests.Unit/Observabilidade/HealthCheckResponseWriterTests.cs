using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Vitalis.Observability.HealthChecks;

namespace Vitalis.Tests.Unit.Observabilidade;

/// <summary>Testes do serializador que monta a resposta JSON dos endpoints de health check.</summary>
public class HealthCheckResponseWriterTests
{
    [Fact]
    public void Serialize_ComTodosOsChecksSaudaveis_ReportaStatusHealthy()
    {
        // Arrange
        var report = CriarRelatorio(("api", HealthStatus.Healthy, "API respondendo normalmente.", null));

        // Act
        var json = JsonDocument.Parse(HealthCheckResponseWriter.Serialize(report));

        // Assert
        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public void Serialize_ComUmCheckIndisponivel_ReportaStatusUnhealthy()
    {
        // Arrange
        var report = CriarRelatorio(
            ("api", HealthStatus.Healthy, "ok", null),
            ("oracle-database", HealthStatus.Unhealthy, "banco fora do ar", null));

        // Act
        var json = JsonDocument.Parse(HealthCheckResponseWriter.Serialize(report));

        // Assert
        Assert.Equal("Unhealthy", json.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public void Serialize_ComVariosChecks_DetalhaCadaVerificacaoNoArrayDeChecks()
    {
        // Arrange
        var report = CriarRelatorio(
            ("api", HealthStatus.Healthy, "ok", null),
            ("pethub-java", HealthStatus.Degraded, "serviço lento", null));

        // Act
        var json = JsonDocument.Parse(HealthCheckResponseWriter.Serialize(report));
        var checks = json.RootElement.GetProperty("checks").EnumerateArray().ToList();

        // Assert
        Assert.Equal(2, checks.Count);
        Assert.Contains(checks, c => c.GetProperty("name").GetString() == "pethub-java"
                                     && c.GetProperty("status").GetString() == "Degraded");
    }

    [Fact]
    public void Serialize_ComCheckQueFalhouPorExcecao_IncluiAMensagemDeErro()
    {
        // Arrange
        var report = CriarRelatorio(
            ("oracle-database", HealthStatus.Unhealthy, "erro", new TimeoutException("Tempo limite excedido")));

        // Act
        var json = JsonDocument.Parse(HealthCheckResponseWriter.Serialize(report));
        var check = json.RootElement.GetProperty("checks").EnumerateArray().Single();

        // Assert
        Assert.Equal("Tempo limite excedido", check.GetProperty("error").GetString());
    }

    [Fact]
    public void Serialize_ComDescricaoAcentuada_PreservaAAcentuacaoSemEscapar()
    {
        // Arrange
        var report = CriarRelatorio(("api", HealthStatus.Healthy, "Conexão estabelecida com sucesso", null));

        // Act
        var json = HealthCheckResponseWriter.Serialize(report);

        // Assert
        Assert.Contains("Conexão estabelecida com sucesso", json);
    }

    [Fact]
    public void Serialize_ComRelatorioSemChecks_RetornaArrayDeChecksVazio()
    {
        // Arrange
        var report = new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.Zero);

        // Act
        var json = JsonDocument.Parse(HealthCheckResponseWriter.Serialize(report));

        // Assert
        Assert.Empty(json.RootElement.GetProperty("checks").EnumerateArray());
    }

    private static HealthReport CriarRelatorio(
        params (string Nome, HealthStatus Status, string Descricao, Exception? Excecao)[] checks)
    {
        var entries = checks.ToDictionary(
            c => c.Nome,
            c => new HealthReportEntry(
                c.Status,
                c.Descricao,
                TimeSpan.FromMilliseconds(5),
                c.Excecao,
                data: new Dictionary<string, object> { ["origem"] = "teste" },
                tags: ["ready"]));

        var statusGeral = entries.Values.Select(e => e.Status).DefaultIfEmpty(HealthStatus.Healthy).Min();
        return new HealthReport(entries, statusGeral, TimeSpan.FromMilliseconds(12));
    }
}
