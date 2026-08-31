using System.ComponentModel.DataAnnotations;
using Vitalis.Models;
using Vitalis.Tests.Unit.Fixtures;

namespace Vitalis.Tests.Unit.Dominio;

/// <summary>Testes das regras de domínio da entidade <see cref="Lembrete"/>.</summary>
public class LembreteTests
{
    [Fact]
    public void NovoLembrete_QuandoInstanciado_IniciaComStatusPendente()
    {
        // Arrange & Act
        var lembrete = new Lembrete
        {
            ResponsavelId = 1,
            PetId = 7,
            Tipo = TipoLembrete.VACINA,
            DataAgendada = new DateOnly(2026, 10, 1),
            Mensagem = "Vacina V10"
        };

        // Assert
        Assert.Equal(StatusLembrete.PENDENTE, lembrete.Status);
    }

    [Fact]
    public void Validar_LembreteSemMensagem_RetornaErroDeCampoObrigatorio()
    {
        // Arrange
        var lembrete = TestData.NovoLembrete();
        lembrete.Mensagem = null!;

        // Act
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(lembrete, new ValidationContext(lembrete), resultados, validateAllProperties: true);

        // Assert
        Assert.Contains(resultados, r => r.MemberNames.Contains(nameof(Lembrete.Mensagem)));
    }

    [Theory]
    [InlineData(TipoLembrete.VACINA)]
    [InlineData(TipoLembrete.CONSULTA)]
    [InlineData(TipoLembrete.EXAME)]
    [InlineData(TipoLembrete.MEDICAMENTO)]
    [InlineData(TipoLembrete.HIDRATACAO)]
    public void NovoLembrete_ParaCadaTipoSuportado_PreservaOTipoInformado(TipoLembrete tipo)
    {
        // Arrange & Act
        var lembrete = TestData.NovoLembrete(tipo: tipo);

        // Assert
        Assert.Equal(tipo, lembrete.Tipo);
    }

    [Theory]
    [InlineData("VACINA", TipoLembrete.VACINA)]
    [InlineData("HIDRATACAO", TipoLembrete.HIDRATACAO)]
    public void ParseTipoLembrete_ComNomeValido_ConverteParaOEnumCorrespondente(string texto, TipoLembrete esperado)
    {
        // Arrange & Act
        var convertido = Enum.Parse<TipoLembrete>(texto);

        // Assert
        Assert.Equal(esperado, convertido);
    }

    [Fact]
    public void ParseTipoLembrete_ComNomeInexistente_NaoConverte()
    {
        // Arrange
        const string tipoInvalido = "BANHO";

        // Act
        var conseguiuConverter = Enum.TryParse<TipoLembrete>(tipoInvalido, ignoreCase: false, out _);

        // Assert
        Assert.False(conseguiuConverter);
    }
}
