using Vitalis.Repositories;
using Vitalis.Tests.Unit.Fixtures;

namespace Vitalis.Tests.Unit.Repositorios;

/// <summary>
/// Testes da regra de negócio "somente um endereço principal por responsável".
/// </summary>
[Collection(InMemoryDatabaseCollection.Name)]
public class ResponsavelEnderecoRepositoryTests
{
    private readonly InMemoryDatabaseFixture _fixture;

    public ResponsavelEnderecoRepositoryTests(InMemoryDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public void Add_QuandoEhOPrimeiroEnderecoDoResponsavel_MarcaComoPrincipalAutomaticamente()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repositorio = new ResponsavelEnderecoRepository(context);
        var endereco = TestData.NovoEndereco(id: 1, responsavelId: 1, principal: false);

        // Act
        repositorio.Add(endereco);

        // Assert
        Assert.True(endereco.Principal);
    }

    [Fact]
    public void Add_QuandoONovoEnderecoEhPrincipal_DesmarcaOsDemaisEnderecos()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repositorio = new ResponsavelEnderecoRepository(context);
        repositorio.Add(TestData.NovoEndereco(id: 1, responsavelId: 1));

        // Act
        repositorio.Add(TestData.NovoEndereco(id: 2, responsavelId: 1, principal: true));

        // Assert
        var principais = context.Enderecos.Where(e => e.ResponsavelId == 1 && e.Principal).ToList();
        Assert.Single(principais);
        Assert.Equal(2, principais[0].Id);
    }

    [Fact]
    public void Add_QuandoONovoEnderecoNaoEhPrincipal_PreservaOPrincipalExistente()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repositorio = new ResponsavelEnderecoRepository(context);
        repositorio.Add(TestData.NovoEndereco(id: 1, responsavelId: 1));

        // Act
        repositorio.Add(TestData.NovoEndereco(id: 2, responsavelId: 1, principal: false));

        // Assert
        Assert.Equal(1, context.Enderecos.Single(e => e.Principal).Id);
    }

    [Fact]
    public void SetPrincipal_ComOutroEnderecoJaPrincipal_TransfereAMarcacao()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repositorio = new ResponsavelEnderecoRepository(context);
        repositorio.Add(TestData.NovoEndereco(id: 1, responsavelId: 1));
        repositorio.Add(TestData.NovoEndereco(id: 2, responsavelId: 1));

        // Act
        repositorio.SetPrincipal(responsavelId: 1, enderecoId: 2);

        // Assert
        Assert.False(context.Enderecos.Single(e => e.Id == 1).Principal);
        Assert.True(context.Enderecos.Single(e => e.Id == 2).Principal);
    }

    [Fact]
    public void GetByResponsavelId_ComEnderecosDeVariosResponsaveis_RetornaApenasOsDoResponsavel()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Enderecos.AddRange(
            TestData.NovoEndereco(id: 1, responsavelId: 1),
            TestData.NovoEndereco(id: 2, responsavelId: 2));
        context.SaveChanges();
        var repositorio = new ResponsavelEnderecoRepository(context);

        // Act
        var enderecos = repositorio.GetByResponsavelId(1).ToList();

        // Assert
        Assert.Single(enderecos);
        Assert.Equal(1, enderecos[0].ResponsavelId);
    }

    [Fact]
    public void Delete_AoRemoverOEnderecoPrincipal_PromoveOutroEnderecoAPrincipal()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repositorio = new ResponsavelEnderecoRepository(context);
        repositorio.Add(TestData.NovoEndereco(id: 1, responsavelId: 1));
        repositorio.Add(TestData.NovoEndereco(id: 2, responsavelId: 1));
        repositorio.SetPrincipal(1, 2);

        // Act
        repositorio.Delete(2);

        // Assert
        var restante = context.Enderecos.Single();
        Assert.Equal(1, restante.Id);
        Assert.True(restante.Principal);
    }
}
