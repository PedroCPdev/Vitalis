using System.ComponentModel.DataAnnotations;
using Vitalis.Tests.Unit.Fixtures;

namespace Vitalis.Tests.Unit.Dominio;

/// <summary>Testes das regras de validação de <see cref="ResponsavelEndereco"/> e <see cref="ResponsavelContato"/>.</summary>
public class ResponsavelEnderecoTests
{
    [Fact]
    public void Validar_EnderecoCompleto_RetornaValido()
    {
        // Arrange
        var endereco = TestData.NovoEndereco();

        // Act
        var resultados = Validar(endereco);

        // Assert
        Assert.Empty(resultados);
    }

    [Fact]
    public void Validar_EnderecoComEstadoAcimaDeDoisCaracteres_RetornaErroDeTamanho()
    {
        // Arrange
        var endereco = TestData.NovoEndereco();
        endereco.Estado = "São Paulo";

        // Act
        var resultados = Validar(endereco);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(ResponsavelEndereco.Estado)));
    }

    [Fact]
    public void Validar_EnderecoComCepAcimaDeOitoCaracteres_RetornaErroDeTamanho()
    {
        // Arrange
        var endereco = TestData.NovoEndereco();
        endereco.Cep = "013101000000";

        // Act
        var resultados = Validar(endereco);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(ResponsavelEndereco.Cep)));
    }

    [Fact]
    public void Validar_EnderecoSemComplemento_RetornaValidoPorSerCampoOpcional()
    {
        // Arrange
        var endereco = TestData.NovoEndereco();
        endereco.Complemento = null;

        // Act
        var resultados = Validar(endereco);

        // Assert
        Assert.Empty(resultados);
    }

    [Fact]
    public void NovoEndereco_QuandoInstanciado_NaoEhPrincipalPorPadrao()
    {
        // Arrange & Act
        var endereco = new ResponsavelEndereco
        {
            Logradouro = "Rua das Flores",
            Numero = "10",
            Bairro = "Centro",
            Cidade = "Campinas",
            Estado = "SP",
            Cep = "13010000"
        };

        // Assert
        Assert.False(endereco.Principal);
    }

    [Fact]
    public void Validar_ContatoSemTelefone_RetornaErroDeCampoObrigatorio()
    {
        // Arrange
        var contato = TestData.NovoContato();
        contato.Telefone = null!;

        // Act
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(contato, new ValidationContext(contato), resultados, validateAllProperties: true);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(ResponsavelContato.Telefone)));
    }

    private static IReadOnlyList<ValidationResult> Validar(ResponsavelEndereco endereco)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(endereco, new ValidationContext(endereco), resultados, validateAllProperties: true);
        return resultados;
    }
}
