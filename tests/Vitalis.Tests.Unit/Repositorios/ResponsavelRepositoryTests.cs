using Vitalis.Repositories;
using Vitalis.Tests.Unit.Fixtures;

namespace Vitalis.Tests.Unit.Repositorios;

/// <summary>
/// Testes do repositório de responsáveis sobre o provider InMemory, compartilhando a
/// <see cref="InMemoryDatabaseFixture"/> através de uma collection fixture.
/// </summary>
[Collection(InMemoryDatabaseCollection.Name)]
public class ResponsavelRepositoryTests
{
    private readonly InMemoryDatabaseFixture _fixture;

    public ResponsavelRepositoryTests(InMemoryDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public void Add_ComResponsavelNovo_ArmazenaASenhaComHashBCrypt()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repositorio = new ResponsavelRepository(context);
        var responsavel = TestData.NovoResponsavel(id: 0);

        // Act
        repositorio.Add(responsavel);

        // Assert
        Assert.NotEqual(TestData.SenhaEmTextoPuro, responsavel.Senha);
        Assert.True(BCrypt.Net.BCrypt.Verify(TestData.SenhaEmTextoPuro, responsavel.Senha));
    }

    [Fact]
    public void Add_ComResponsavelNovo_DefineADataDeCriacao()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repositorio = new ResponsavelRepository(context);
        var responsavel = TestData.NovoResponsavel(id: 0);
        var antes = DateTime.UtcNow.AddSeconds(-1);

        // Act
        repositorio.Add(responsavel);

        // Assert
        Assert.InRange(responsavel.CreatedAt, antes, DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void GetByCpf_ComCpfDeResponsavelAtivo_RetornaOResponsavel()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Responsavels.Add(TestData.NovoResponsavel(id: 1, cpf: "55566677788"));
        context.SaveChanges();
        var repositorio = new ResponsavelRepository(context);

        // Act
        var encontrado = repositorio.GetByCpf("55566677788");

        // Assert
        Assert.NotNull(encontrado);
        Assert.Equal(1, encontrado.Id);
    }

    [Fact]
    public void GetByCpf_ComCpfDeResponsavelInativo_RetornaNulo()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Responsavels.Add(TestData.NovoResponsavel(id: 1, cpf: "55566677788", ativo: false));
        context.SaveChanges();
        var repositorio = new ResponsavelRepository(context);

        // Act
        var encontrado = repositorio.GetByCpf("55566677788");

        // Assert
        Assert.Null(encontrado);
    }

    [Fact]
    public void GetById_ComResponsavelExistente_CarregaEnderecosEContatos()
    {
        // Arrange
        using var context = _fixture.CreateSeededContext();
        var repositorio = new ResponsavelRepository(context);

        // Act
        var encontrado = repositorio.GetById(1);

        // Assert
        Assert.NotNull(encontrado);
        Assert.Single(encontrado.Enderecos);
        Assert.Single(encontrado.Contatos);
    }

    [Fact]
    public void GetByEmail_ComEmailInexistente_RetornaNulo()
    {
        // Arrange
        using var context = _fixture.CreateSeededContext();
        var repositorio = new ResponsavelRepository(context);

        // Act
        var encontrado = repositorio.GetByEmail("nao-existe@pethub.com");

        // Assert
        Assert.Null(encontrado);
    }

    [Fact]
    public void Update_QuandoASenhaNaoEhInformada_PreservaASenhaJaArmazenada()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repositorio = new ResponsavelRepository(context);
        var original = TestData.NovoResponsavel(id: 0);
        repositorio.Add(original);
        var hashOriginal = original.Senha;

        context.ChangeTracker.Clear();
        var alteracao = TestData.NovoResponsavel(id: original.Id, nome: "Nome Alterado", senha: "ignorada");

        // Act
        repositorio.Update(alteracao);

        // Assert
        Assert.Equal(hashOriginal, alteracao.Senha);
        Assert.Equal("Nome Alterado", context.Responsavels.Single().Nome);
    }

    [Fact]
    public void Delete_ComResponsavelExistente_RemoveORegistro()
    {
        // Arrange
        using var context = _fixture.CreateSeededContext();
        var repositorio = new ResponsavelRepository(context);

        // Act
        repositorio.Delete(1);

        // Assert
        Assert.Empty(context.Responsavels);
    }

    [Fact]
    public void Delete_ComIdInexistente_NaoAlteraOsRegistrosExistentes()
    {
        // Arrange
        using var context = _fixture.CreateSeededContext();
        var repositorio = new ResponsavelRepository(context);

        // Act
        repositorio.Delete(999);

        // Assert
        Assert.Single(context.Responsavels);
    }
}
