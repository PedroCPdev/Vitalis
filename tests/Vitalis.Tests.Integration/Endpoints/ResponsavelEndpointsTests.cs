using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Vitalis.Tests.Integration.Fixtures;

namespace Vitalis.Tests.Integration.Endpoints;

/// <summary>
/// Testes de integração dos endpoints de Responsável: fluxo HTTP completo, autenticação
/// por <c>X-Service-Token</c>, respostas de sucesso e tratamento de erros.
/// </summary>
[Collection(VitalisApiCollection.Name)]
public class ResponsavelEndpointsTests : IDisposable
{
    private readonly VitalisWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ResponsavelEndpointsTests(VitalisWebApplicationFactory factory)
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
    public async Task PostCadastro_ComDadosValidos_Retorna201ComLocationDoNovoRecurso()
    {
        // Arrange
        var payload = DadosDeIntegracao.NovoCadastro();

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/responsavel/cadastro", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.NotNull(resposta.Headers.Location);
    }

    [Fact]
    public async Task PostCadastro_ComDadosValidos_NaoRetornaASenhaNoCorpoDaResposta()
    {
        // Arrange
        var payload = DadosDeIntegracao.NovoCadastro();

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/responsavel/cadastro", payload);
        var corpo = await resposta.Content.ReadAsStringAsync();

        // Assert
        Assert.DoesNotContain("senha", corpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(DadosDeIntegracao.SenhaEmTextoPuro, corpo);
    }

    [Fact]
    public async Task PostCadastro_ComEmailInvalido_Retorna400ComOsErrosDeValidacao()
    {
        // Arrange
        var payload = DadosDeIntegracao.NovoCadastro(email: "email-sem-arroba");

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/responsavel/cadastro", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task PostCadastro_SemOsCamposObrigatorios_Retorna400()
    {
        // Arrange
        var payloadIncompleto = new { nome = "Somente o nome" };

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/responsavel/cadastro", payloadIncompleto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task PostCadastro_ComJsonMalformado_Retorna400()
    {
        // Arrange
        var conteudo = new StringContent("{ isso não é json }", Encoding.UTF8, "application/json");

        // Act
        var resposta = await _client.PostAsync("/api/responsavel/cadastro", conteudo);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task PostCadastro_ComCpfJaCadastrado_Retorna409Conflict()
    {
        // Arrange
        await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "99988877766", email: "primeiro@pethub.com");

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/responsavel/cadastro",
            DadosDeIntegracao.NovoCadastro(cpf: "99988877766", email: "segundo@pethub.com"));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task GetById_ComResponsavelCadastrado_Retorna200ComOsDados()
    {
        // Arrange
        var id = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);

        // Act
        var resposta = await _client.GetAsync($"/api/responsavel/{id}");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(id, corpo.GetProperty("id").GetInt64());
        Assert.Equal("Pedro Chasci", corpo.GetProperty("nome").GetString());
    }

    [Fact]
    public async Task GetById_ComIdInexistente_Retorna404ComMensagemDeErro()
    {
        // Arrange
        const long idInexistente = 999_999;

        // Act
        var resposta = await _client.GetAsync($"/api/responsavel/{idInexistente}");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("Responsavel não encontrado", corpo.GetProperty("erro").GetString());
    }

    [Fact]
    public async Task GetAll_ComVariosResponsaveisCadastrados_Retorna200ComTodosOsRegistros()
    {
        // Arrange
        await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "11111111111", email: "a@pethub.com");
        await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "22222222222", email: "b@pethub.com");

        // Act
        var resposta = await _client.GetAsync("/api/responsavel");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(2, corpo.GetArrayLength());
    }

    [Fact]
    public async Task GetBuscarPorCpf_SemOServiceToken_Retorna401Unauthorized()
    {
        // Arrange
        await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "33344455566", email: "c@pethub.com");

        // Act
        var resposta = await _client.GetAsync("/api/responsavel/buscar?cpf=33344455566");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task GetBuscarPorCpf_ComServiceTokenInvalido_Retorna401Unauthorized()
    {
        // Arrange
        using var clientComTokenErrado = _factory.CreateClientComServiceToken("token-invalido");

        // Act
        var resposta = await clientComTokenErrado.GetAsync("/api/responsavel/buscar?cpf=33344455566");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task GetBuscarPorCpf_ComServiceTokenValidoECpfCadastrado_Retorna200ComOResponsavel()
    {
        // Arrange
        await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "44455566677", email: "d@pethub.com");
        using var clientAutenticado = _factory.CreateClientComServiceToken();

        // Act
        var resposta = await clientAutenticado.GetAsync("/api/responsavel/buscar?cpf=44455566677");
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("44455566677", corpo.GetProperty("cpf").GetString());
    }

    [Fact]
    public async Task GetBuscarPorCpf_ComServiceTokenValidoECpfInexistente_Retorna404()
    {
        // Arrange
        using var clientAutenticado = _factory.CreateClientComServiceToken();

        // Act
        var resposta = await clientAutenticado.GetAsync("/api/responsavel/buscar?cpf=00000000000");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task GetBuscarPorCpf_ComServiceTokenValidoESemCpf_Retorna400()
    {
        // Arrange
        using var clientAutenticado = _factory.CreateClientComServiceToken();

        // Act
        var resposta = await clientAutenticado.GetAsync("/api/responsavel/buscar?cpf=");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task PostLogin_ComCredenciaisValidas_Retorna200ComOsDadosDoResponsavel()
    {
        // Arrange
        var id = await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "55566677788", email: "login@pethub.com");

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/responsavel/login",
            DadosDeIntegracao.NovoLogin(email: "login@pethub.com"));
        var corpo = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(id, corpo.GetProperty("id").GetInt64());
    }

    [Fact]
    public async Task PostLogin_ComSenhaIncorreta_Retorna401Unauthorized()
    {
        // Arrange
        await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "66677788899", email: "senha@pethub.com");

        // Act
        var resposta = await _client.PostAsJsonAsync("/api/responsavel/login",
            DadosDeIntegracao.NovoLogin(email: "senha@pethub.com", senha: "senha-errada"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task PostLogin_ComEmailNaoCadastrado_Retorna401Unauthorized()
    {
        // Arrange & Act
        var resposta = await _client.PostAsJsonAsync("/api/responsavel/login",
            DadosDeIntegracao.NovoLogin(email: "ninguem@pethub.com"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task PutResponsavel_ComResponsavelCadastrado_Retorna204EPersisteAsAlteracoes()
    {
        // Arrange
        var id = await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "77788899900", email: "put@pethub.com");
        var alteracao = DadosDeIntegracao.NovoCadastro(
            nome: "Nome Atualizado", cpf: "77788899900", email: "atualizado@pethub.com");

        // Act
        var resposta = await _client.PutAsJsonAsync($"/api/responsavel/{id}", alteracao);
        var consulta = await _client.GetFromJsonAsync<JsonElement>($"/api/responsavel/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal("Nome Atualizado", consulta.GetProperty("nome").GetString());
    }

    [Fact]
    public async Task PutResponsavel_ComIdInexistente_Retorna404()
    {
        // Arrange & Act
        var resposta = await _client.PutAsJsonAsync("/api/responsavel/999999", DadosDeIntegracao.NovoCadastro());

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task DeleteResponsavel_ComResponsavelCadastrado_Retorna204ERemoveORegistro()
    {
        // Arrange
        var id = await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "88899900011", email: "del@pethub.com");

        // Act
        var resposta = await _client.DeleteAsync($"/api/responsavel/{id}");
        var consulta = await _client.GetAsync($"/api/responsavel/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);
    }

    [Fact]
    public async Task DeleteResponsavel_ComIdInexistente_Retorna404()
    {
        // Arrange & Act
        var resposta = await _client.DeleteAsync("/api/responsavel/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task GetById_ComIdNaoNumerico_Retorna404PorNaoCasarComARota()
    {
        // Arrange & Act
        var resposta = await _client.GetAsync("/api/responsavel/abc");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
