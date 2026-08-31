using Vitalis.Models;
using Vitalis.Repositories;
using Vitalis.Tests.Unit.Fixtures;

namespace Vitalis.Tests.Unit.Repositorios;

/// <summary>Testes do repositório de lembretes sobre o provider InMemory.</summary>
[Collection(InMemoryDatabaseCollection.Name)]
public class LembreteRepositoryTests
{
    private readonly InMemoryDatabaseFixture _fixture;

    public LembreteRepositoryTests(InMemoryDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public void Add_ComLembreteNovo_ForcaOStatusPendenteEDefineACriacao()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repositorio = new LembreteRepository(context);
        var lembrete = TestData.NovoLembrete(id: 0, status: StatusLembrete.ENVIADO);

        // Act
        repositorio.Add(lembrete);

        // Assert
        Assert.Equal(StatusLembrete.PENDENTE, lembrete.Status);
        Assert.NotEqual(default, lembrete.CreatedAt);
    }

    [Fact]
    public void GetByResponsavelId_ComLembretesDeVariosResponsaveis_RetornaApenasOsDoResponsavelInformado()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Lembretes.AddRange(
            TestData.NovoLembrete(id: 1, responsavelId: 1),
            TestData.NovoLembrete(id: 2, responsavelId: 2),
            TestData.NovoLembrete(id: 3, responsavelId: 1));
        context.SaveChanges();
        var repositorio = new LembreteRepository(context);

        // Act
        var lembretes = repositorio.GetByResponsavelId(1).ToList();

        // Assert
        Assert.Equal(2, lembretes.Count);
        Assert.All(lembretes, l => Assert.Equal(1, l.ResponsavelId));
    }

    [Fact]
    public void GetByResponsavelIdETipo_ComTipoInformado_FiltraPorResponsavelETipo()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Lembretes.AddRange(
            TestData.NovoLembrete(id: 1, responsavelId: 1, tipo: TipoLembrete.VACINA),
            TestData.NovoLembrete(id: 2, responsavelId: 1, tipo: TipoLembrete.EXAME));
        context.SaveChanges();
        var repositorio = new LembreteRepository(context);

        // Act
        var lembretes = repositorio.GetByResponsavelIdETipo(1, TipoLembrete.VACINA).ToList();

        // Assert
        Assert.Single(lembretes);
        Assert.Equal(TipoLembrete.VACINA, lembretes[0].Tipo);
    }

    [Fact]
    public void GetAll_ComVariosLembretes_OrdenaDaDataAgendadaMaisRecenteParaAMaisAntiga()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        // GetAll faz Include do Responsavel, então o principal precisa existir.
        context.Responsavels.Add(TestData.NovoResponsavel(id: 1));
        var antigo = TestData.NovoLembrete(id: 1);
        antigo.DataAgendada = new DateOnly(2026, 1, 1);
        var recente = TestData.NovoLembrete(id: 2);
        recente.DataAgendada = new DateOnly(2026, 12, 1);
        context.Lembretes.AddRange(antigo, recente);
        context.SaveChanges();
        var repositorio = new LembreteRepository(context);

        // Act
        var lembretes = repositorio.GetAll().ToList();

        // Assert
        Assert.Equal(2, lembretes[0].Id);
        Assert.Equal(1, lembretes[1].Id);
    }

    [Fact]
    public void AtualizarStatus_ComLembreteExistente_PersisteONovoStatus()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Lembretes.Add(TestData.NovoLembrete(id: 1));
        context.SaveChanges();
        var repositorio = new LembreteRepository(context);

        // Act
        repositorio.AtualizarStatus(1, StatusLembrete.ENVIADO);

        // Assert
        Assert.Equal(StatusLembrete.ENVIADO, context.Lembretes.Single().Status);
    }

    [Fact]
    public void AtualizarStatus_ComIdInexistente_NaoAlteraNenhumRegistro()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Lembretes.Add(TestData.NovoLembrete(id: 1));
        context.SaveChanges();
        var repositorio = new LembreteRepository(context);

        // Act
        repositorio.AtualizarStatus(999, StatusLembrete.FALHOU);

        // Assert
        Assert.Equal(StatusLembrete.PENDENTE, context.Lembretes.Single().Status);
    }

    [Fact]
    public void Delete_ComLembreteExistente_RemoveORegistro()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        context.Lembretes.Add(TestData.NovoLembrete(id: 1));
        context.SaveChanges();
        var repositorio = new LembreteRepository(context);

        // Act
        repositorio.Delete(1);

        // Assert
        Assert.Empty(context.Lembretes);
    }
}
