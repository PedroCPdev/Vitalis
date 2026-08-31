using System.Net.Http.Json;
using System.Text.Json;
using Vitalis.Models;

namespace Vitalis.Tests.Integration.Fixtures;

/// <summary>Payloads e utilitários reutilizados na etapa <b>Arrange</b> dos testes de integração.</summary>
public static class DadosDeIntegracao
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public const string SenhaEmTextoPuro = "SenhaSegura@123";

    public static object NovoCadastro(
        string nome = "Pedro Chasci",
        string cpf = "12345678901",
        string email = "pedro@pethub.com",
        string senha = SenhaEmTextoPuro)
        => new { nome, cpf, email, senha };

    public static object NovoLogin(string email = "pedro@pethub.com", string senha = SenhaEmTextoPuro)
        => new { email, senha };

    public static object NovoEndereco(bool principal = false)
        => new
        {
            logradouro = "Av. Paulista",
            numero = "1000",
            complemento = "Sala 42",
            bairro = "Bela Vista",
            cidade = "São Paulo",
            estado = "SP",
            cep = "01310100",
            principal
        };

    public static object NovoContato(string telefone = "11999998888", bool principal = false)
        => new { tipo = "CELULAR", telefone, principal };

    public static object NovoLembrete(long responsavelId, TipoLembrete tipo = TipoLembrete.VACINA)
        => new
        {
            responsavelId,
            petId = 7,
            // A API serializa e vincula os enums pelo valor numérico.
            tipo = (int)tipo,
            dataAgendada = "2026-09-15",
            mensagem = "Vacina antirrábica agendada",
            referenciaId = 99,
            referenciaTipo = "VACINA"
        };

    /// <summary>Cadastra um responsável pela API e devolve o identificador gerado.</summary>
    public static async Task<long> CadastrarResponsavelAsync(
        HttpClient client, string cpf = "12345678901", string email = "pedro@pethub.com")
    {
        var resposta = await client.PostAsJsonAsync("/api/responsavel/cadastro",
            NovoCadastro(cpf: cpf, email: email));
        resposta.EnsureSuccessStatusCode();

        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return criado.GetProperty("id").GetInt64();
    }
}
