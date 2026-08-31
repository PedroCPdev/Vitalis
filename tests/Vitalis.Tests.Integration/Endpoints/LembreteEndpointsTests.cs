using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Vitalis.Models;
using Vitalis.Tests.Integration.Fixtures;

namespace Vitalis.Tests.Integration.Endpoints;

/// <summary>
/// Testes de integração dos lembretes, incluindo o fluxo usado pelo backend Java
/// (criação autenticada por <c>X-Service-Token</c>).
/// </summary>
[Collection(VitalisApiCollection.Name)]
public class LembreteEndpointsTests : IDisposable
{
    private readonly VitalisWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LembreteEndpointsTests(VitalisWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.LimparBanco();
        _client = factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task PostLembrete_ComServiceTokenValido_Retorna201EPersisteOLembrete()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        using var clientAutenticado = _factory.CreateClientComServiceToken();

        // Act
        var resposta = await clientAutenticado.PostAsJsonAsync("/api/lembretes",
            DadosDeIntegracao.NovoLembrete(responsavelId));

        // Assert
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(criado.GetProperty("id").GetInt64() > 0);
    }

    [Fact]
    public async Task PostLembrete_SemServiceToken_Retorna401Unauthorized()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/lembretes",
            DadosDeIntegracao.NovoLembrete(responsavelId));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task PostLembrete_ComServiceTokenInvalido_Retorna401Unauthorized()
    {
        // Arrange
        using var clientComTokenErrado = _factory.CreateClientComServiceToken("token-invalido");

        // Act
        var resposta = await clientComTokenErrado.PostAsJsonAsync("/api/lembretes",
            DadosDeIntegracao.NovoLembrete(responsavelId: 1));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task PostLembrete_QuandoCriado_NasceComStatusPendente()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        using var clientAutenticado = _factory.CreateClientComServiceToken();

        // Act
        var criado = await CriarLembreteAsync(clientAutenticado, responsavelId);
        var lembretes = await _client.GetFromJsonAsync<JsonElement>("/api/lembretes");

        // Assert
        var lembrete = lembretes.EnumerateArray().Single(l => l.GetProperty("id").GetInt64() == criado);
        Assert.Equal((int)StatusLembrete.PENDENTE, lembrete.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task GetLembretes_SemLembretesCadastrados_Retorna200ComListaVazia()
    {
        // Arrange & Act
        var resposta = await _client.GetAsync("/api/lembretes");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(0, corpo.GetArrayLength());
    }

    [Fact]
    public async Task GetLembreteById_ComIdInexistente_Retorna404()
    {
        // Arrange & Act
        var resposta = await _client.GetAsync("/api/lembretes/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task GetLembretesPorResponsavel_ComLembretesDeVariosResponsaveis_RetornaApenasOsDoResponsavel()
    {
        // Arrange
        var primeiro = await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "11111111111", email: "a@pethub.com");
        var segundo = await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "22222222222", email: "b@pethub.com");
        using var clientAutenticado = _factory.CreateClientComServiceToken();
        await CriarLembreteAsync(clientAutenticado, primeiro);
        await CriarLembreteAsync(clientAutenticado, segundo);

        // Act
        var lembretes = await _client.GetFromJsonAsync<JsonElement>($"/api/lembretes/responsavel/{primeiro}");

        // Assert
        Assert.Equal(1, lembretes.GetArrayLength());
        Assert.Equal(primeiro, lembretes[0].GetProperty("responsavelId").GetInt64());
    }

    [Fact]
    public async Task GetLembretesPorResponsavelETipo_ComTipoInformado_RetornaApenasOsLembretesDaqueleTipo()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        using var clientAutenticado = _factory.CreateClientComServiceToken();
        await CriarLembreteAsync(clientAutenticado, responsavelId, TipoLembrete.VACINA);
        await CriarLembreteAsync(clientAutenticado, responsavelId, TipoLembrete.EXAME);

        // Act
        var lembretes = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/lembretes/responsavel/{responsavelId}/tipo/VACINA");

        // Assert
        Assert.Equal(1, lembretes.GetArrayLength());
    }

    [Fact]
    public async Task GetLembretesPorResponsavelETipo_ComTipoInexistente_Retorna400()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);

        // Act
        var resposta = await _client.GetAsync($"/api/lembretes/responsavel/{responsavelId}/tipo/BANHO");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task PatchStatus_ComLembreteExistente_Retorna204EAtualizaOStatus()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        using var clientAutenticado = _factory.CreateClientComServiceToken();
        var lembreteId = await CriarLembreteAsync(clientAutenticado, responsavelId);

        // Act
        var resposta = await _client.PatchAsJsonAsync($"/api/lembretes/{lembreteId}/status",
            new { status = (int)StatusLembrete.ENVIADO });
        var lembrete = await _client.GetFromJsonAsync<JsonElement>($"/api/lembretes/{lembreteId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal((int)StatusLembrete.ENVIADO, lembrete.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task PatchStatus_ComLembreteInexistente_Retorna404()
    {
        // Arrange & Act
        var resposta = await _client.PatchAsJsonAsync("/api/lembretes/999999/status",
            new { status = (int)StatusLembrete.FALHOU });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task DeleteLembrete_ComLembreteExistente_Retorna204ERemoveORegistro()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        using var clientAutenticado = _factory.CreateClientComServiceToken();
        var lembreteId = await CriarLembreteAsync(clientAutenticado, responsavelId);

        // Act
        var resposta = await _client.DeleteAsync($"/api/lembretes/{lembreteId}");
        var consulta = await _client.GetAsync($"/api/lembretes/{lembreteId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);
    }

    [Fact]
    public async Task DeleteLembrete_ComLembreteInexistente_Retorna404()
    {
        // Arrange & Act
        var resposta = await _client.DeleteAsync("/api/lembretes/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    private static async Task<long> CriarLembreteAsync(
        HttpClient clientAutenticado, long responsavelId, TipoLembrete tipo = TipoLembrete.VACINA)
    {
        var resposta = await clientAutenticado.PostAsJsonAsync("/api/lembretes",
            DadosDeIntegracao.NovoLembrete(responsavelId, tipo));
        resposta.EnsureSuccessStatusCode();

        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return criado.GetProperty("id").GetInt64();
    }
}
