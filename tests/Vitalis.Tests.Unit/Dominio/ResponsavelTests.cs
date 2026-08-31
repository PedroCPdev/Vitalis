using System.ComponentModel.DataAnnotations;
using Vitalis.Tests.Unit.Fixtures;

namespace Vitalis.Tests.Unit.Dominio;

/// <summary>Testes das regras de validação da entidade de domínio <see cref="Responsavel"/>.</summary>
public class ResponsavelTests
{
    [Fact]
    public void Validar_ResponsavelComTodosOsCamposObrigatorios_RetornaValido()
    {
        // Arrange
        var responsavel = TestData.NovoResponsavel();

        // Act
        var resultados = Validar(responsavel);

        // Assert
        Assert.Empty(resultados);
    }

    [Fact]
    public void Validar_ResponsavelSemNome_RetornaErroDeCampoObrigatorio()
    {
        // Arrange
        var responsavel = TestData.NovoResponsavel();
        responsavel.Nome = string.Empty;

        // Act
        var resultados = Validar(responsavel);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(Responsavel.Nome)));
    }

    [Fact]
    public void Validar_ResponsavelComCpfAcimaDe11Caracteres_RetornaErroDeTamanho()
    {
        // Arrange
        var responsavel = TestData.NovoResponsavel(cpf: "123456789012345");

        // Act
        var resultados = Validar(responsavel);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(Responsavel.Cpf)));
    }

    [Fact]
    public void Validar_ResponsavelComNomeAcimaDe150Caracteres_RetornaErroDeTamanho()
    {
        // Arrange
        var responsavel = TestData.NovoResponsavel(nome: new string('a', 151));

        // Act
        var resultados = Validar(responsavel);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(Responsavel.Nome)));
    }

    [Fact]
    public void NovoResponsavel_QuandoInstanciado_IniciaAtivoComColecoesVazias()
    {
        // Arrange & Act
        var responsavel = new Responsavel
        {
            Nome = "Lucas Figueiredo",
            Cpf = "98765432100",
            Email = "lucas@pethub.com",
            Senha = TestData.SenhaEmTextoPuro
        };

        // Assert
        Assert.True(responsavel.Ativo);
        Assert.Empty(responsavel.Enderecos);
        Assert.Empty(responsavel.Contatos);
        Assert.Empty(responsavel.Lembretes);
    }

    private static IReadOnlyList<ValidationResult> Validar(Responsavel responsavel)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(responsavel, new ValidationContext(responsavel), resultados, validateAllProperties: true);
        return resultados;
    }
}
