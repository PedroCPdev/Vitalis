using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Vitalis.Tests.Integration.Fixtures;

namespace Vitalis.Tests.Integration.Endpoints;

/// <summary>
/// Testes de integração dos recursos aninhados de endereço e contato, incluindo a regra
/// de "principal único" e o isolamento entre responsáveis diferentes.
/// </summary>
[Collection(VitalisApiCollection.Name)]
public class EnderecoContatoEndpointsTests : IDisposable
{
    private readonly VitalisWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EnderecoContatoEndpointsTests(VitalisWebApplicationFactory factory)
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
    public async Task PostEndereco_ComDadosValidos_Retorna201EMarcaOPrimeiroComoPrincipal()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);

        // Act
        var resposta = await _client.PostAsJsonAsync(
            $"/api/responsavel/{responsavelId}/enderecos", DadosDeIntegracao.NovoEndereco());
        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.True(criado.GetProperty("principal").GetBoolean());
    }

    [Fact]
    public async Task PostEndereco_SemOsCamposObrigatorios_Retorna400()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        var enderecoIncompleto = new { logradouro = "Av. Paulista" };

        // Act
        var resposta = await _client.PostAsJsonAsync(
            $"/api/responsavel/{responsavelId}/enderecos", enderecoIncompleto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task GetEnderecos_AposCadastrarDois_Retorna200ComOsDoisEnderecos()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        await _client.PostAsJsonAsync($"/api/responsavel/{responsavelId}/enderecos", DadosDeIntegracao.NovoEndereco());
        await _client.PostAsJsonAsync($"/api/responsavel/{responsavelId}/enderecos", DadosDeIntegracao.NovoEndereco());

        // Act
        var enderecos = await _client.GetFromJsonAsync<JsonElement>($"/api/responsavel/{responsavelId}/enderecos");

        // Assert
        Assert.Equal(2, enderecos.GetArrayLength());
    }

    [Fact]
    public async Task GetEnderecoById_QuandoPertenceAOutroResponsavel_Retorna404()
    {
        // Arrange
        var dono = await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "11111111111", email: "dono@pethub.com");
        var estranho = await DadosDeIntegracao.CadastrarResponsavelAsync(_client, cpf: "22222222222", email: "outro@pethub.com");
        var enderecoId = await CriarEnderecoAsync(dono);

        // Act
        var resposta = await _client.GetAsync($"/api/responsavel/{estranho}/enderecos/{enderecoId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task PatchEnderecoPrincipal_ComDoisEnderecos_TransfereAMarcacaoDePrincipal()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        var primeiro = await CriarEnderecoAsync(responsavelId);
        var segundo = await CriarEnderecoAsync(responsavelId);

        // Act
        var resposta = await _client.PatchAsync(
            $"/api/responsavel/{responsavelId}/enderecos/{segundo}/principal", content: null);
        var enderecos = await _client.GetFromJsonAsync<JsonElement>($"/api/responsavel/{responsavelId}/enderecos");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.False(BuscarPorId(enderecos, primeiro).GetProperty("principal").GetBoolean());
        Assert.True(BuscarPorId(enderecos, segundo).GetProperty("principal").GetBoolean());
    }

    [Fact]
    public async Task DeleteEndereco_ComEnderecoExistente_Retorna204ERemoveORegistro()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        var enderecoId = await CriarEnderecoAsync(responsavelId);

        // Act
        var resposta = await _client.DeleteAsync($"/api/responsavel/{responsavelId}/enderecos/{enderecoId}");
        var consulta = await _client.GetAsync($"/api/responsavel/{responsavelId}/enderecos/{enderecoId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);
    }

    [Fact]
    public async Task PostContato_ComDadosValidos_Retorna201EMarcaOPrimeiroComoPrincipal()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);

        // Act
        var resposta = await _client.PostAsJsonAsync(
            $"/api/responsavel/{responsavelId}/contatos", DadosDeIntegracao.NovoContato());
        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.True(criado.GetProperty("principal").GetBoolean());
    }

    [Fact]
    public async Task PatchContatoPrincipal_ComDoisContatos_TransfereAMarcacaoDePrincipal()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        var primeiro = await CriarContatoAsync(responsavelId, "11999998888");
        var segundo = await CriarContatoAsync(responsavelId, "11955554444");

        // Act
        var resposta = await _client.PatchAsync(
            $"/api/responsavel/{responsavelId}/contatos/{segundo}/principal", content: null);
        var contatos = await _client.GetFromJsonAsync<JsonElement>($"/api/responsavel/{responsavelId}/contatos");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.False(BuscarPorId(contatos, primeiro).GetProperty("principal").GetBoolean());
        Assert.True(BuscarPorId(contatos, segundo).GetProperty("principal").GetBoolean());
    }

    [Fact]
    public async Task GetContatoById_ComIdInexistente_Retorna404()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);

        // Act
        var resposta = await _client.GetAsync($"/api/responsavel/{responsavelId}/contatos/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task GetResponsavelById_ComEnderecoEContatoCadastrados_RetornaOsRelacionamentosAninhados()
    {
        // Arrange
        var responsavelId = await DadosDeIntegracao.CadastrarResponsavelAsync(_client);
        await CriarEnderecoAsync(responsavelId);
        await CriarContatoAsync(responsavelId, "11999998888");

        // Act
        var responsavel = await _client.GetFromJsonAsync<JsonElement>($"/api/responsavel/{responsavelId}");

        // Assert
        Assert.Equal(1, responsavel.GetProperty("enderecos").GetArrayLength());
        Assert.Equal(1, responsavel.GetProperty("contatos").GetArrayLength());
    }

    private async Task<long> CriarEnderecoAsync(long responsavelId)
    {
        var resposta = await _client.PostAsJsonAsync(
            $"/api/responsavel/{responsavelId}/enderecos", DadosDeIntegracao.NovoEndereco());
        resposta.EnsureSuccessStatusCode();

        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return criado.GetProperty("id").GetInt64();
    }

    private async Task<long> CriarContatoAsync(long responsavelId, string telefone)
    {
        var resposta = await _client.PostAsJsonAsync(
            $"/api/responsavel/{responsavelId}/contatos", DadosDeIntegracao.NovoContato(telefone));
        resposta.EnsureSuccessStatusCode();

        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return criado.GetProperty("id").GetInt64();
    }

    private static JsonElement BuscarPorId(JsonElement colecao, long id)
        => colecao.EnumerateArray().Single(item => item.GetProperty("id").GetInt64() == id);
}
