using Vitalis.Models;

namespace Vitalis.Tests.Unit.Fixtures;

/// <summary>
/// Fábrica de objetos usados na etapa <b>Arrange</b> dos testes, mantendo os cenários
/// legíveis e evitando duplicação de dados de exemplo.
/// </summary>
public static class TestData
{
    public const string SenhaEmTextoPuro = "SenhaSegura@123";

    public static Responsavel NovoResponsavel(
        long id = 1,
        string nome = "Pedro Chasci",
        string cpf = "12345678901",
        string email = "pedro@pethub.com",
        string? senha = null,
        bool ativo = true)
        => new()
        {
            Id = id,
            Nome = nome,
            Cpf = cpf,
            Email = email,
            Senha = senha ?? SenhaEmTextoPuro,
            Ativo = ativo,
            CreatedAt = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc)
        };

    /// <summary>Responsável com a senha já protegida por BCrypt, como fica após a persistência.</summary>
    public static Responsavel NovoResponsavelComSenhaHasheada(
        long id = 1,
        string email = "pedro@pethub.com",
        string senha = SenhaEmTextoPuro,
        bool ativo = true)
    {
        var responsavel = NovoResponsavel(id: id, email: email, ativo: ativo);
        responsavel.Senha = BCrypt.Net.BCrypt.HashPassword(senha);
        return responsavel;
    }

    public static ResponsavelEndereco NovoEndereco(
        long id = 1,
        long responsavelId = 1,
        bool principal = false,
        string cidade = "São Paulo")
        => new()
        {
            Id = id,
            ResponsavelId = responsavelId,
            Logradouro = "Av. Paulista",
            Numero = "1000",
            Complemento = "Sala 42",
            Bairro = "Bela Vista",
            Cidade = cidade,
            Estado = "SP",
            Cep = "01310100",
            Principal = principal
        };

    public static ResponsavelContato NovoContato(
        long id = 1,
        long responsavelId = 1,
        bool principal = false,
        string telefone = "11999998888",
        string tipo = "CELULAR")
        => new()
        {
            Id = id,
            ResponsavelId = responsavelId,
            Tipo = tipo,
            Telefone = telefone,
            Principal = principal
        };

    public static Lembrete NovoLembrete(
        long id = 1,
        long responsavelId = 1,
        long petId = 7,
        TipoLembrete tipo = TipoLembrete.VACINA,
        StatusLembrete status = StatusLembrete.PENDENTE)
        => new()
        {
            Id = id,
            ResponsavelId = responsavelId,
            PetId = petId,
            Tipo = tipo,
            DataAgendada = new DateOnly(2026, 9, 15),
            Mensagem = "Vacina antirrábica agendada",
            Status = status,
            ReferenciaId = 99,
            ReferenciaTipo = "VACINA",
            CreatedAt = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc)
        };

    public static CadastrarResponsavelDto NovoCadastroDto(
        string nome = "Pedro Chasci",
        string cpf = "12345678901",
        string email = "pedro@pethub.com",
        string senha = SenhaEmTextoPuro)
        => new() { Nome = nome, Cpf = cpf, Email = email, Senha = senha };

    public static CriarLembreteDto NovoCriarLembreteDto(
        long responsavelId = 1,
        long petId = 7,
        TipoLembrete tipo = TipoLembrete.CONSULTA)
        => new()
        {
            ResponsavelId = responsavelId,
            PetId = petId,
            Tipo = tipo,
            DataAgendada = new DateOnly(2026, 9, 20),
            Mensagem = "Consulta de retorno",
            ReferenciaId = 55,
            ReferenciaTipo = "CONSULTA"
        };
}
