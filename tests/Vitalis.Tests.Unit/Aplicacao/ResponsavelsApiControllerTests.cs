using Microsoft.AspNetCore.Mvc;
using Moq;
using Vitalis.Repositories;
using Vitalis.Tests.Unit.Fixtures;

namespace Vitalis.Tests.Unit.Aplicacao;

/// <summary>
/// Testes unitários da camada de aplicação do domínio Responsável.
/// O repositório é substituído por um mock (Moq), isolando o controller do banco de dados.
/// </summary>
public class ResponsavelsApiControllerTests : IClassFixture<ApiConfigurationFixture>
{
    private readonly ApiConfigurationFixture _fixture;
    private readonly Mock<IResponsavelRepository> _repositorio = new(MockBehavior.Strict);

    public ResponsavelsApiControllerTests(ApiConfigurationFixture fixture) => _fixture = fixture;

    private ResponsavelsApiController CriarController(string? serviceToken = null)
        => ApiConfigurationFixture.ComHttpContext(
            new ResponsavelsApiController(_repositorio.Object, _fixture.Configuration), serviceToken);

    [Fact]
    public void GetAll_QuandoExistemResponsaveisCadastrados_RetornaOkComTodosOsRegistros()
    {
        // Arrange
        _repositorio.Setup(r => r.GetAll()).Returns(
        [
            TestData.NovoResponsavel(id: 1, cpf: "11111111111", email: "a@pethub.com"),
            TestData.NovoResponsavel(id: 2, cpf: "22222222222", email: "b@pethub.com")
        ]);
        var controller = CriarController();

        // Act
        var resultado = controller.GetAll();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(resultado);
        var itens = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
        Assert.Equal(2, itens.Count());
        _repositorio.Verify(r => r.GetAll(), Times.Once);
    }

    [Fact]
    public void GetById_QuandoResponsavelExiste_RetornaOkComOsDadosDoResponsavel()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(1)).Returns(TestData.NovoResponsavel(id: 1));
        var controller = CriarController();

        // Act
        var resultado = controller.GetById(1);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public void GetById_QuandoResponsavelNaoExiste_RetornaNotFound()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(999)).Returns((Responsavel?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.GetById(999);

        // Assert
        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public void BuscarPorCpf_ComServiceTokenValidoECpfExistente_RetornaOkComOResponsavel()
    {
        // Arrange
        _repositorio.Setup(r => r.GetByCpf("12345678901")).Returns(TestData.NovoResponsavel());
        var controller = CriarController(ApiConfigurationFixture.ServiceTokenValido);

        // Act
        var resultado = controller.BuscarPorCpf("12345678901");

        // Assert
        Assert.IsType<OkObjectResult>(resultado);
        _repositorio.Verify(r => r.GetByCpf("12345678901"), Times.Once);
    }

    [Fact]
    public void BuscarPorCpf_ComServiceTokenInvalido_RetornaUnauthorizedSemConsultarORepositorio()
    {
        // Arrange
        var controller = CriarController("token-errado");

        // Act
        var resultado = controller.BuscarPorCpf("12345678901");

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(resultado);
        _repositorio.Verify(r => r.GetByCpf(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void BuscarPorCpf_ComCpfVazio_RetornaBadRequest()
    {
        // Arrange
        var controller = CriarController(ApiConfigurationFixture.ServiceTokenValido);

        // Act
        var resultado = controller.BuscarPorCpf("   ");

        // Assert
        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public void BuscarPorCpf_QuandoCpfNaoEstaCadastrado_RetornaNotFound()
    {
        // Arrange
        _repositorio.Setup(r => r.GetByCpf("00000000000")).Returns((Responsavel?)null);
        var controller = CriarController(ApiConfigurationFixture.ServiceTokenValido);

        // Act
        var resultado = controller.BuscarPorCpf("00000000000");

        // Assert
        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public void Cadastrar_ComDadosValidos_RetornaCreatedEPersisteOResponsavel()
    {
        // Arrange
        var dto = TestData.NovoCadastroDto();
        _repositorio.Setup(r => r.GetByCpf(dto.Cpf)).Returns((Responsavel?)null);
        _repositorio.Setup(r => r.Add(It.IsAny<Responsavel>()))
            .Callback<Responsavel>(r => r.Id = 10);
        var controller = CriarController();

        // Act
        var resultado = controller.Cadastrar(dto);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(resultado);
        Assert.Equal(nameof(ResponsavelsApiController.GetById), created.ActionName);
        Assert.Equal(10L, created.RouteValues!["id"]);
        _repositorio.Verify(r => r.Add(It.Is<Responsavel>(x => x.Cpf == dto.Cpf && x.Ativo)), Times.Once);
    }

    [Fact]
    public void Cadastrar_ComCpfJaExistente_RetornaConflictSemPersistir()
    {
        // Arrange
        var dto = TestData.NovoCadastroDto();
        _repositorio.Setup(r => r.GetByCpf(dto.Cpf)).Returns(TestData.NovoResponsavel());
        var controller = CriarController();

        // Act
        var resultado = controller.Cadastrar(dto);

        // Assert
        Assert.IsType<ConflictObjectResult>(resultado);
        _repositorio.Verify(r => r.Add(It.IsAny<Responsavel>()), Times.Never);
    }

    [Fact]
    public void Cadastrar_ComModelStateInvalido_RetornaBadRequest()
    {
        // Arrange
        var controller = CriarController();
        controller.ModelState.AddModelError(nameof(CadastrarResponsavelDto.Email), "E-mail inválido");

        // Act
        var resultado = controller.Cadastrar(TestData.NovoCadastroDto());

        // Assert
        Assert.IsType<BadRequestObjectResult>(resultado);
        _repositorio.Verify(r => r.Add(It.IsAny<Responsavel>()), Times.Never);
    }

    [Fact]
    public void Login_ComCredenciaisValidas_RetornaOkComOsDadosDoResponsavel()
    {
        // Arrange
        var responsavel = TestData.NovoResponsavelComSenhaHasheada();
        _repositorio.Setup(r => r.GetByEmail(responsavel.Email)).Returns(responsavel);
        var controller = CriarController();

        // Act
        var resultado = controller.Login(new LoginDto
        {
            Email = responsavel.Email,
            Senha = TestData.SenhaEmTextoPuro
        });

        // Assert
        Assert.IsType<OkObjectResult>(resultado);
    }

    [Fact]
    public void Login_ComSenhaIncorreta_RetornaUnauthorized()
    {
        // Arrange
        var responsavel = TestData.NovoResponsavelComSenhaHasheada();
        _repositorio.Setup(r => r.GetByEmail(responsavel.Email)).Returns(responsavel);
        var controller = CriarController();

        // Act
        var resultado = controller.Login(new LoginDto { Email = responsavel.Email, Senha = "senha-errada" });

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(resultado);
    }

    [Fact]
    public void Login_ComEmailNaoCadastrado_RetornaUnauthorized()
    {
        // Arrange
        _repositorio.Setup(r => r.GetByEmail("ninguem@pethub.com")).Returns((Responsavel?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.Login(new LoginDto
        {
            Email = "ninguem@pethub.com",
            Senha = TestData.SenhaEmTextoPuro
        });

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(resultado);
    }

    [Fact]
    public void Login_ComContaDesativada_RetornaUnauthorized()
    {
        // Arrange
        var responsavel = TestData.NovoResponsavelComSenhaHasheada(ativo: false);
        _repositorio.Setup(r => r.GetByEmail(responsavel.Email)).Returns(responsavel);
        var controller = CriarController();

        // Act
        var resultado = controller.Login(new LoginDto
        {
            Email = responsavel.Email,
            Senha = TestData.SenhaEmTextoPuro
        });

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(resultado);
    }

    [Fact]
    public void Update_QuandoResponsavelExiste_AtualizaOsDadosERetornaNoContent()
    {
        // Arrange
        var existente = TestData.NovoResponsavel(id: 5);
        _repositorio.Setup(r => r.GetById(5)).Returns(existente);
        _repositorio.Setup(r => r.Update(existente));
        var controller = CriarController();
        var dto = TestData.NovoCadastroDto(nome: "Nome Atualizado", email: "novo@pethub.com");

        // Act
        var resultado = controller.Update(5, dto);

        // Assert
        Assert.IsType<NoContentResult>(resultado);
        Assert.Equal("Nome Atualizado", existente.Nome);
        Assert.Equal("novo@pethub.com", existente.Email);
        _repositorio.Verify(r => r.Update(existente), Times.Once);
    }

    [Fact]
    public void Update_QuandoResponsavelNaoExiste_RetornaNotFoundSemAtualizar()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(404)).Returns((Responsavel?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.Update(404, TestData.NovoCadastroDto());

        // Assert
        Assert.IsType<NotFoundObjectResult>(resultado);
        _repositorio.Verify(r => r.Update(It.IsAny<Responsavel>()), Times.Never);
    }

    [Fact]
    public void Delete_QuandoResponsavelExiste_RemoveERetornaNoContent()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(3)).Returns(TestData.NovoResponsavel(id: 3));
        _repositorio.Setup(r => r.Delete(3));
        var controller = CriarController();

        // Act
        var resultado = controller.Delete(3);

        // Assert
        Assert.IsType<NoContentResult>(resultado);
        _repositorio.Verify(r => r.Delete(3), Times.Once);
    }

    [Fact]
    public void Delete_QuandoResponsavelNaoExiste_RetornaNotFoundSemRemover()
    {
        // Arrange
        _repositorio.Setup(r => r.GetById(404)).Returns((Responsavel?)null);
        var controller = CriarController();

        // Act
        var resultado = controller.Delete(404);

        // Assert
        Assert.IsType<NotFoundObjectResult>(resultado);
        _repositorio.Verify(r => r.Delete(It.IsAny<long>()), Times.Never);
    }
}
