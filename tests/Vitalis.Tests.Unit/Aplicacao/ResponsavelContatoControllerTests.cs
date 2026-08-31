using Microsoft.AspNetCore.Mvc;
using Moq;
using Vitalis.Repositories;
using Vitalis.Tests.Unit.Fixtures;

namespace Vitalis.Tests.Unit.Aplicacao;

/// <summary>Testes unitários da camada de aplicação de contatos do responsável.</summary>
public class ResponsavelContatoControllerTests
{
    private readonly Mock<IResponsavelContatoRepository> _repositorio = new(MockBehavior.Strict);

    private ResponsavelContatoController CriarController()
        => ApiConfigurationFixture.ComHttpContext(new ResponsavelContatoController(_repositorio.Object));

    [Fact]
    public void GetAll_QuandoOResponsavelPossuiContatos_RetornaOkComALista()
    {
        // Arrange
        _repositorio.Setup(r => r.GetByResponsavelId(1))
            .Returns([TestData.NovoContato(id: 1, responsavelId: 1, principal: true)]);
        var controller = CriarController();

        // Act
        var resultado = controller.GetAll(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Single(Assert.IsAssignableFrom<IEnumerable<ResponsavelContato>>(ok.Value));
    }

    [Fact]
    public void GetById_QuandoOContatoNaoExiste_RetornaNotFound()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(999)).Returns((ResponsavelContato?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.GetById(1, 999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public void Add_ComContatoValido_AssociaAoResponsavelERetornaCreated()
    {
        // Arrange
        var contato = TestData.NovoContato(id: 0, responsavelId: 0);
        _repositorio.Setup(r => r.Add(contato)).Callback<ResponsavelContato>(c => c.Id = 3);
        var controller = CriarController();

        // Act
        var resultado = controller.Add(responsavelId: 8, contato);

        // Assert
        Assert.IsType<CreatedAtActionResult>(resultado);
        Assert.Equal(8, contato.ResponsavelId);
        _repositorio.Verify(r => r.Add(contato), Times.Once);
    }

    [Fact]
    public void Update_QuandoOContatoExiste_PreservaOsIdentificadoresERetornaNoContent()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(4)).Returns(TestData.NovoContato(id: 4, responsavelId: 2));
        _repositorio.Setup(r => r.Update(It.IsAny<ResponsavelContato>()));
        var controller = CriarController();
        var atualizado = TestData.NovoContato(id: 0, responsavelId: 0, telefone: "11912345678");

        // Act
        var resultado = controller.Update(responsavelid: 2, id: 4, atualizado);

        // Assert
        Assert.IsType<NoContentResult>(resultado);
        Assert.Equal(4, atualizado.Id);
        Assert.Equal(2, atualizado.ResponsavelId);
    }

    [Fact]
    public void Delete_QuandoOContatoPertenceAoResponsavel_RemoveERetornaNoContent()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(4)).Returns(TestData.NovoContato(id: 4, responsavelId: 2));
        _repositorio.Setup(r => r.Delete(4));
        var controller = CriarController();

        // Act
        var resultado = controller.Delete(2, 4);

        // Assert
        Assert.IsType<NoContentResult>(resultado);
        _repositorio.Verify(r => r.Delete(4), Times.Once);
    }

    [Fact]
    public void SetPrincipal_QuandoOContatoPertenceAoResponsavel_DelegaParaORepositorio()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(4)).Returns(TestData.NovoContato(id: 4, responsavelId: 2));
        _repositorio.Setup(r => r.SetPrincipal(2, 4));
        var controller = CriarController();

        // Act
        var resultado = controller.SetPrincipal(2, 4);

        // Assert
        Assert.IsType<NoContentResult>(resultado);
        _repositorio.Verify(r => r.SetPrincipal(2, 4), Times.Once);
    }
}
