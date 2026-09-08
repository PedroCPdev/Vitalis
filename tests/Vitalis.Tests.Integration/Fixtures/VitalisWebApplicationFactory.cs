// Importa as abstrações de hospedagem usadas pelo host de testes
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
// Importa o Entity Framework para substituir o provider de banco
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Vitalis.Tests.Integration.Fixtures;

// Sobe a API completa em memória com WebApplicationFactory, trocando o banco Oracle
// por um provider InMemory para que os testes não dependam de infraestrutura externa
public class VitalisWebApplicationFactory : WebApplicationFactory<Program>
{
    // Token de serviço usado pelos testes dos endpoints de integração com o backend Java
    public const string ServiceToken = "token-de-integracao-2026";

    // Nome exclusivo do banco em memória desta execução
    private readonly string _nomeDoBanco = $"vitalis-integracao-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Define o Service Token e aponta o serviço externo para uma porta sempre fechada
        builder.UseSetting("ServiceToken", ServiceToken);
        builder.UseSetting("ServicosExternos:PethubJava", "http://127.0.0.1:9/health");

        builder.ConfigureServices(services =>
        {
            RemoverRegistroDoOracle(services);

            // Registra o contexto apontando para o banco em memória
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_nomeDoBanco));
        });
    }

    // Remove todo o registro do AppDbContext feito no Program.cs — inclusive a configuração
    // de opções que aplica o UseOracle —, já que o EF Core não permite dois providers
    private static void RemoverRegistroDoOracle(IServiceCollection services)
    {
        var registros = services
            .Where(descriptor =>
                descriptor.ServiceType == typeof(AppDbContext) ||
                descriptor.ServiceType == typeof(DbContextOptions) ||
                descriptor.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                EhConfiguracaoDeOpcoesDoContexto(descriptor.ServiceType))
            .ToList();

        foreach (var registro in registros)
            services.Remove(registro);
    }

    // Identifica o IDbContextOptionsConfiguration<AppDbContext> registrado pelo AddDbContext
    private static bool EhConfiguracaoDeOpcoesDoContexto(Type serviceType)
        => serviceType.IsGenericType
           && serviceType.GetGenericTypeDefinition().Name == "IDbContextOptionsConfiguration`1"
           && serviceType.GenericTypeArguments[0] == typeof(AppDbContext);

    // Limpa todas as tabelas entre os cenários de teste
    public void LimparBanco()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Lembretes.RemoveRange(context.Lembretes);
        context.Contatos.RemoveRange(context.Contatos);
        context.Enderecos.RemoveRange(context.Enderecos);
        context.Responsavels.RemoveRange(context.Responsavels);
        context.SaveChanges();
        context.ChangeTracker.Clear();
    }

    // Cria um HttpClient já com o header X-Service-Token preenchido
    public HttpClient CreateClientComServiceToken(string token = ServiceToken)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Service-Token", token);
        return client;
    }
}

// Collection fixture: a API é iniciada uma única vez e compartilhada por todas as
// classes de teste de integração, reduzindo o tempo total de execução
[CollectionDefinition(Name)]
public class VitalisApiCollection : ICollectionFixture<VitalisWebApplicationFactory>
{
    public const string Name = "API Vitalis em memória";
}
