using Microsoft.AspNetCore.Mvc;
using Moq;
using Vitalis.Models;
using Vitalis.Repositories;
using Vitalis.Tests.Unit.Fixtures;

namespace Vitalis.Tests.Unit.Aplicacao;

/// <summary>
/// Testes unitários da camada de aplicação de Lembretes, incluindo a proteção
/// por <c>X-Service-Token</c> usada na integração com o backend Java.
/// </summary>
public class LembretesApiControllerTests : IClassFixture<ApiConfigurationFixture>
{
    private readonly ApiConfigurationFixture _fixture;
    private readonly Mock<ILembreteRepository> _repositorio = new(MockBehavior.Strict);

    public LembretesApiControllerTests(ApiConfigurationFixture fixture) => _fixture = fixture;

    private LembretesApiController CriarController(string? serviceToken = null)
        => ApiConfigurationFixture.ComHttpContext(
            new LembretesApiController(_repositorio.Object, _fixture.Configuration), serviceToken);

    [Fact]
    public void GetAll_QuandoExistemLembretes_RetornaOkComTodosOsRegistros()
    {
        // Arrange
        _repositorio.Setup(r => r.GetAll()).Returns(
        [
            TestData.NovoLembrete(id: 1),
            TestData.NovoLembrete(id: 2, tipo: TipoLembrete.EXAME)
        ]);
        var controller = CriarController();

        // Act
        var resultado = controller.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Equal(2, Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value).Count());
    }

    [Fact]
    public void GetById_QuandoLembreteExiste_RetornaOkComOLembrete()
    {
        // Arrange
        var lembrete = TestData.NovoLembrete(id: 1);
        _repositorio.Setup(r => r.GetById(1)).Returns(lembrete);
        var controller = CriarController();

        // Act
        var resultado = controller.GetById(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Same(lembrete, ok.Value);
    }

    [Fact]
    public void GetById_QuandoLembreteNaoExiste_RetornaNotFound()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(999)).Returns((Lembrete?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public void GetByResponsavel_QuandoResponsavelPossuiLembretes_RetornaOkApenasComOsSeusLembretes()
    {
        // Arrange
        _repositorio.Setup(r => r.GetByResponsavelId(1)).Returns([TestData.NovoLembrete(id: 1, responsavelId: 1)]);
        var controller = CriarController();

        // Act
        var resultado = controller.GetByResponsavel(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(resultado);
        var lembretes = Assert.IsAssignableFrom<IEnumerable<Lembrete>>(ok.Value);
        Assert.All(lembretes, l => Assert.Equal(1, l.ResponsavelId));
    }

    [Fact]
    public void GetByResponsavelETipo_ComTipoInformado_DelegaOFiltroParaORepositorio()
    {
        // Arrange
        _repositorio.Setup(r => r.GetByResponsavelIdETipo(1, TipoLembrete.VACINA))
            .Returns([TestData.NovoLembrete(tipo: TipoLembrete.VACINA)]);
        var controller = CriarController();

        // Act
        var resultado = controller.GetByResponsavelETipo(1, TipoLembrete.VACINA);

        // Assert
        Assert.IsType<OkObjectResult>(resultado);
        _repositorio.Verify(r => r.GetByResponsavelIdETipo(1, TipoLembrete.VACINA), Times.Once);
    }

    [Fact]
    public void Criar_ComServiceTokenValido_RetornaCreatedEPersisteOLembrete()
    {
        // Arrange
        var dto = TestData.NovoCriarLembreteDto();
        _repositorio.Setup(r => r.Add(It.IsAny<Lembrete>())).Callback<Lembrete>(l => l.Id = 20);
        var controller = CriarController(ApiConfigurationFixture.ServiceTokenValido);

        // Act
        var resultado = controller.Criar(dto);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(resultado);
        Assert.Equal(20L, created.RouteValues!["id"]);
        _repositorio.Verify(r => r.Add(It.Is<Lembrete>(l =>
            l.ResponsavelId == dto.ResponsavelId &&
            l.PetId == dto.PetId &&
            l.Tipo == dto.Tipo &&
            l.Mensagem == dto.Mensagem)), Times.Once);
    }

    [Fact]
    public void Criar_SemServiceToken_RetornaUnauthorizedSemPersistir()
    {
        // Arrange
        var controller = CriarController();

        // Act
        var resultado = controller.Criar(TestData.NovoCriarLembreteDto());

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(resultado);
        _repositorio.Verify(r => r.Add(It.IsAny<Lembrete>()), Times.Never);
    }

    [Fact]
    public void Criar_ComServiceTokenInvalido_RetornaUnauthorized()
    {
        // Arrange
        var controller = CriarController("token-invalido");

        // Act
        var resultado = controller.Criar(TestData.NovoCriarLembreteDto());

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(resultado);
    }

    [Fact]
    public void Criar_ComModelStateInvalido_RetornaBadRequest()
    {
        // Arrange
        var controller = CriarController(ApiConfigurationFixture.ServiceTokenValido);
        controller.ModelState.AddModelError(nameof(CriarLembreteDto.Mensagem), "Mensagem obrigatória");

        // Act
        var resultado = controller.Criar(TestData.NovoCriarLembreteDto());

        // Assert
        Assert.IsType<BadRequestObjectResult>(resultado);
        _repositorio.Verify(r => r.Add(It.IsAny<Lembrete>()), Times.Never);
    }

    [Fact]
    public void AtualizarStatus_QuandoLembreteExiste_AtualizaERetornaNoContent()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(1)).Returns(TestData.NovoLembrete(id: 1));
        _repositorio.Setup(r => r.AtualizarStatus(1, StatusLembrete.ENVIADO));
        var controller = CriarController();

        // Act
        var resultado = controller.AtualizarStatus(1, new AtualizarStatusDto { Status = StatusLembrete.ENVIADO });

        // Assert
        Assert.IsType<NoContentResult>(resultado);
        _repositorio.Verify(r => r.AtualizarStatus(1, StatusLembrete.ENVIADO), Times.Once);
    }

    [Fact]
    public void AtualizarStatus_QuandoLembreteNaoExiste_RetornaNotFoundSemAtualizar()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(999)).Returns((Lembrete?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.AtualizarStatus(999, new AtualizarStatusDto { Status = StatusLembrete.FALHOU });

        // Assert
        Assert.IsType<NotFoundObjectResult>(resultado);
        _repositorio.Verify(r => r.AtualizarStatus(It.IsAny<long>(), It.IsAny<StatusLembrete>()), Times.Never);
    }

    [Fact]
    public void Delete_QuandoLembreteExiste_RemoveERetornaNoContent()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(1)).Returns(TestData.NovoLembrete(id: 1));
        _repositorio.Setup(r => r.Delete(1));
        var controller = CriarController();

        // Act
        var resultado = controller.Delete(1);

        // Assert
        Assert.IsType<NoContentResult>(resultado);
        _repositorio.Verify(r => r.Delete(1), Times.Once);
    }

    [Fact]
    public void Delete_QuandoLembreteNaoExiste_RetornaNotFoundSemRemover()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(999)).Returns((Lembrete?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.Delete(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(resultado);
        _repositorio.Verify(r => r.Delete(It.IsAny<long>()), Times.Never);
    }
}
