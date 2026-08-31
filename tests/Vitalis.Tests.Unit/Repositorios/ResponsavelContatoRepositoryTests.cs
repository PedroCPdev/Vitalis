using Vitalis.Repositories;
using Vitalis.Tests.Unit.Fixtures;

namespace Vitalis.Tests.Unit.Repositorios;

/// <summary>
/// Testes da regra de negócio "somente um contato principal por responsável".
/// </summary>
[Collection(InMemoryDatabaseCollection.Name)]
public class ResponsavelContatoRepositoryTests
{
    private readonly InMemoryDatabaseFixture _fixture;

    public ResponsavelContatoRepositoryTests(InMemoryDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public void Add_QuandoEhOPrimeiroContatoDoResponsavel_MarcaComoPrincipalAutomaticamente()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repositorio = new ResponsavelContatoRepository(context);
        var contato = TestData.NovoContato(id: 1, responsavelId: 1, principal: false);

        // Act
        repositorio.Add(contato);

        // Assert
        Assert.True(contato.Principal);
    }

    [Fact]
    public void Add_QuandoONovoContatoEhPrincipal_DesmarcaOsDemaisContatos()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repositorio = new ResponsavelContatoRepository(context);
        repositorio.Add(TestData.NovoContato(id: 1, responsavelId: 1));

        // Act
        repositorio.Add(TestData.NovoContato(id: 2, responsavelId: 1, principal: true, telefone: "11955554444"));

        // Assert
        Assert.Single(context.Contatos.Where(c => c.Principal));
        Assert.Equal(2, context.Contatos.Single(c => c.Principal).Id);
    }

    [Fact]
    public void SetPrincipal_ComOutroContatoJaPrincipal_TransfereAMarcacao()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repositorio = new ResponsavelContatoRepository(context);
        repositorio.Add(TestData.NovoContato(id: 1, responsavelId: 1));
        repositorio.Add(TestData.NovoContato(id: 2, responsavelId: 1, telefone: "11933332222"));

        // Act
        repositorio.SetPrincipal(responsavelId: 1, contatoId: 2);

        // Assert
        Assert.False(context.Contatos.Single(c => c.Id == 1).Principal);
        Assert.True(context.Contatos.Single(c => c.Id == 2).Principal);
    }

    [Fact]
    public void GetById_ComIdInexistente_RetornaNulo()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repositorio = new ResponsavelContatoRepository(context);

        // Act
        var contato = repositorio.GetById(999);

        // Assert
        Assert.Null(contato);
    }

    [Fact]
    public void Delete_AoRemoverOContatoPrincipal_PromoveOutroContatoAPrincipal()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repositorio = new ResponsavelContatoRepository(context);
        repositorio.Add(TestData.NovoContato(id: 1, responsavelId: 1));
        repositorio.Add(TestData.NovoContato(id: 2, responsavelId: 1, telefone: "11911112222"));
        repositorio.SetPrincipal(1, 2);

        // Act
        repositorio.Delete(2);

        // Assert
        var restante = context.Contatos.Single();
        Assert.Equal(1, restante.Id);
        Assert.True(restante.Principal);
    }
}
