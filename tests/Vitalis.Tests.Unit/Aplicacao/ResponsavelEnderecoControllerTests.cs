using Microsoft.AspNetCore.Mvc;
using Moq;
using Vitalis.Repositories;
using Vitalis.Tests.Unit.Fixtures;

namespace Vitalis.Tests.Unit.Aplicacao;

/// <summary>
/// Testes unitários dos endereços. Além do caminho feliz, cobrem a regra de escopo:
/// um endereço só pode ser acessado através do responsável a que pertence.
/// </summary>
public class ResponsavelEnderecoControllerTests
{
    private readonly Mock<IResponsavelEnderecoRepository> _repositorio = new(MockBehavior.Strict);

    private ResponsavelEnderecoController CriarController()
        => ApiConfigurationFixture.ComHttpContext(new ResponsavelEnderecoController(_repositorio.Object));

    [Fact]
    public void GetAll_QuandoOResponsavelPossuiEnderecos_RetornaOkComALista()
    {
        // Arrange
        _repositorio.Setup(r => r.GetByResponsavelId(1))
            .Returns([TestData.NovoEndereco(id: 1, responsavelId: 1, principal: true)]);
        var controller = CriarController();

        // Act
        var resultado = controller.GetAll(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<ResponsavelEndereco>>(ok.Value));
    }

    [Fact]
    public void GetById_QuandoOEnderecoPertenceAoResponsavel_RetornaOkComOEndereco()
    {
        // Arrange
        var endereco = TestData.NovoEndereco(id: 1, responsavelId: 1);
        _repositorio.Setup(r => r.GetById(1)).Returns(endereco);
        var controller = CriarController();

        // Act
        var resultado = controller.GetById(1, 1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Same(endereco, ok.Value);
    }

    [Fact]
    public void GetById_QuandoOEnderecoPertenceAOutroResponsavel_RetornaNotFound()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(1)).Returns(TestData.NovoEndereco(id: 1, responsavelId: 99));
        var controller = CriarController();

        // Act
        var resultado = controller.GetById(responsavelId: 1, id: 1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public void Add_ComEnderecoValido_AssociaAoResponsavelERetornaCreated()
    {
        // Arrange
        var endereco = TestData.NovoEndereco(id: 0, responsavelId: 0);
        _repositorio.Setup(r => r.Add(endereco)).Callback<ResponsavelEndereco>(e => e.Id = 7);
        var controller = CriarController();

        // Act
        var resultado = controller.Add(responsavelId: 42, endereco);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(resultado);
        Assert.Equal(42L, created.RouteValues!["responsavelId"]);
        Assert.Equal(42, endereco.ResponsavelId);
    }

    [Fact]
    public void Add_ComModelStateInvalido_RetornaBadRequestSemPersistir()
    {
        // Arrange
        var controller = CriarController();
        controller.ModelState.AddModelError(nameof(ResponsavelEndereco.Cep), "CEP obrigatório");

        // Act
        var resultado = controller.Add(1, TestData.NovoEndereco());

        // Assert
        Assert.IsType<BadRequestObjectResult>(resultado);
        _repositorio.Verify(r => r.Add(It.IsAny<ResponsavelEndereco>()), Times.Never);
    }

    [Fact]
    public void Update_QuandoOEnderecoExiste_PreservaOsIdentificadoresERetornaNoContent()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(5)).Returns(TestData.NovoEndereco(id: 5, responsavelId: 1));
        _repositorio.Setup(r => r.Update(It.IsAny<ResponsavelEndereco>()));
        var controller = CriarController();
        var atualizado = TestData.NovoEndereco(id: 0, responsavelId: 0, cidade: "Santos");

        // Act
        var resultado = controller.Update(responsavelId: 1, id: 5, atualizado);

        // Assert
        Assert.IsType<NoContentResult>(resultado);
        Assert.Equal(5, atualizado.Id);
        Assert.Equal(1, atualizado.ResponsavelId);
        _repositorio.Verify(r => r.Update(atualizado), Times.Once);
    }

    [Fact]
    public void Update_QuandoOEnderecoNaoExiste_RetornaNotFoundSemAtualizar()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(404)).Returns((ResponsavelEndereco?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.Update(1, 404, TestData.NovoEndereco());

        // Assert
        Assert.IsType<NotFoundObjectResult>(resultado);
        _repositorio.Verify(r => r.Update(It.IsAny<ResponsavelEndereco>()), Times.Never);
    }

    [Fact]
    public void Delete_QuandoOEnderecoPertenceAoResponsavel_RemoveERetornaNoContent()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(5)).Returns(TestData.NovoEndereco(id: 5, responsavelId: 1));
        _repositorio.Setup(r => r.Delete(5));
        var controller = CriarController();

        // Act
        var resultado = controller.Delete(1, 5);

        // Assert
        Assert.IsType<NoContentResult>(resultado);
        _repositorio.Verify(r => r.Delete(5), Times.Once);
    }

    [Fact]
    public void SetPrincipal_QuandoOEnderecoPertenceAoResponsavel_DelegaParaORepositorio()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(5)).Returns(TestData.NovoEndereco(id: 5, responsavelId: 1));
        _repositorio.Setup(r => r.SetPrincipal(1, 5));
        var controller = CriarController();

        // Act
        var resultado = controller.SetPrincipal(1, 5);

        // Assert
        Assert.IsType<NoContentResult>(resultado);
        _repositorio.Verify(r => r.SetPrincipal(1, 5), Times.Once);
    }

    [Fact]
    public void SetPrincipal_QuandoOEnderecoPertenceAOutroResponsavel_RetornaNotFound()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(5)).Returns(TestData.NovoEndereco(id: 5, responsavelId: 99));
        var controller = CriarController();

        // Act
        var resultado = controller.SetPrincipal(1, 5);

        // Assert
        Assert.IsType<NotFoundObjectResult>(resultado);
        _repositorio.Verify(r => r.SetPrincipal(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
    }
}
