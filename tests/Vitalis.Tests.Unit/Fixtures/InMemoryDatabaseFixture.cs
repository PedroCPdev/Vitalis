using Microsoft.EntityFrameworkCore;
using Vitalis.Models;

namespace Vitalis.Tests.Unit.Fixtures;

/// <summary>
/// Fixture compartilhada pelos testes de repositório. Cria instâncias isoladas do
/// <see cref="AppDbContext"/> sobre o provider InMemory, evitando qualquer dependência
/// de um banco Oracle real durante os testes unitários.
/// </summary>
public sealed class InMemoryDatabaseFixture : IDisposable
{
    private readonly List<AppDbContext> _contexts = [];

    /// <summary>Cria um contexto vazio sobre um banco em memória exclusivo.</summary>
    public AppDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? $"vitalis-{Guid.NewGuid():N}")
            .EnableSensitiveDataLogging()
            .Options;

        var context = new AppDbContext(options);
        _contexts.Add(context);
        return context;
    }

    /// <summary>Cria um contexto já populado com um responsável, endereço, contato e lembrete.</summary>
    public AppDbContext CreateSeededContext(string? databaseName = null)
    {
        var context = CreateContext(databaseName);

        var responsavel = TestData.NovoResponsavel(id: 1, nome: "Ana Flavia", cpf: "11122233344");
        context.Responsavels.Add(responsavel);
        context.Enderecos.Add(TestData.NovoEndereco(id: 1, responsavelId: 1, principal: true));
        context.Contatos.Add(TestData.NovoContato(id: 1, responsavelId: 1, principal: true));
        context.Lembretes.Add(TestData.NovoLembrete(id: 1, responsavelId: 1, tipo: TipoLembrete.VACINA));
        context.SaveChanges();

        return context;
    }

    public void Dispose()
    {
        foreach (var context in _contexts)
        {
            // Alguns testes descartam o contexto propositalmente para simular falhas.
            try
            {
                context.Database.EnsureDeleted();
                context.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Contexto já descartado pelo teste — nada a limpar.
            }
        }

        _contexts.Clear();
    }
}

/// <summary>
/// Collection fixture: permite que várias classes de teste compartilhem a mesma
/// <see cref="InMemoryDatabaseFixture"/>, reduzindo o custo de setup.
/// </summary>
[CollectionDefinition(Name)]
public class InMemoryDatabaseCollection : ICollectionFixture<InMemoryDatabaseFixture>
{
    public const string Name = "Banco em memória";
}
