// Importa o validador de Data Annotations usado pelas entidades
using System.ComponentModel.DataAnnotations;
// Importa o FluentAssertions para sintaxe expressiva de asserção
using FluentAssertions;
// Importa o xUnit para anotações e execução de testes
using Xunit;

namespace Vitalis.Tests.Unit.Dominio;

// Suíte de testes unitários das regras de Endereço e Contato do Responsável
public class ResponsavelEnderecoTests
{
    [Fact]
    public void Validar_EnderecoCompleto_DeveRetornarSemErros()
    {
        // Arrange
        var endereco = NovoEnderecoValido();

        // Act
        var erros = Validar(endereco);

        // Assert
        erros.Should().BeEmpty();
    }

    [Fact]
    public void Validar_EstadoAcimaDeDoisCaracteres_DeveRetornarErroNoCampoEstado()
    {
        // Arrange
        var endereco = NovoEnderecoValido();
        endereco.Estado = "São Paulo";

        // Act
        var erros = Validar(endereco);

        // Assert
        erros.Should().Contain(e => e.MemberNames.Contains(nameof(ResponsavelEndereco.Estado)));
    }

    [Fact]
    public void Validar_CepAcimaDeOitoCaracteres_DeveRetornarErroNoCampoCep()
    {
        // Arrange
        var endereco = NovoEnderecoValido();
        endereco.Cep = "013101000000";

        // Act
        var erros = Validar(endereco);

        // Assert
        erros.Should().Contain(e => e.MemberNames.Contains(nameof(ResponsavelEndereco.Cep)));
    }

    [Fact]
    public void Validar_SemComplemento_DeveRetornarSemErrosPorSerCampoOpcional()
    {
        // Arrange
        var endereco = NovoEnderecoValido();
        endereco.Complemento = null;

        // Act
        var erros = Validar(endereco);

        // Assert
        erros.Should().BeEmpty();
    }

    [Fact]
    public void NovoEndereco_QuandoInstanciado_NaoDeveSerPrincipalPorPadrao()
    {
        // Arrange & Act
        var endereco = NovoEnderecoValido();

        // Assert
        endereco.Principal.Should().BeFalse();
    }

    [Fact]
    public void Validar_ContatoSemTelefone_DeveRetornarErroNoCampoTelefone()
    {
        // Arrange
        var contato = new ResponsavelContato
        {
            ResponsavelId = 1,
            Tipo = "CELULAR",
            Telefone = null!
        };

        // Act
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(contato, new ValidationContext(contato), erros, true);

        // Assert
        erros.Should().Contain(e => e.MemberNames.Contains(nameof(ResponsavelContato.Telefone)));
    }

    // Cria um endereço válido reutilizado na preparação dos cenários
    private static ResponsavelEndereco NovoEnderecoValido() => new()
    {
        ResponsavelId = 1,
        Logradouro    = "Av. Paulista",
        Numero        = "1000",
        Complemento   = "Sala 42",
        Bairro        = "Bela Vista",
        Cidade        = "São Paulo",
        Estado        = "SP",
        Cep           = "01310100"
    };

    // Executa a validação por Data Annotations e devolve os erros encontrados
    private static List<ValidationResult> Validar(ResponsavelEndereco endereco)
    {
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(endereco, new ValidationContext(endereco), erros, true);
        return erros;
    }
}
