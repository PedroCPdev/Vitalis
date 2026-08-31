using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Vitalis.Tests.Integration.Fixtures;

/// <summary>
/// Sobe a API completa em memória com <see cref="WebApplicationFactory{TEntryPoint}"/>,
/// substituindo o banco Oracle por um provider InMemory. Toda a pipeline real é exercitada:
/// roteamento, model binding, validação, middlewares de observabilidade e health checks.
/// </summary>
public class VitalisWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>Token de serviço usado pelos testes que exercitam os endpoints de integração.</summary>
    public const string ServiceToken = "token-de-integracao-2026";

    /// <summary>Nome do health check do serviço externo simulado nos testes.</summary>
    public const string ServicoExternoFake = "servico-externo-fake";

    /// <summary>
    /// Porta reservada e sempre fechada (RFC 6335 "discard"), garantindo que o serviço
    /// externo esteja consistentemente indisponível durante os testes.
    /// </summary>
    public const string UrlDoServicoExternoFake = "http://127.0.0.1:9/health";

    private readonly string _databaseName = $"vitalis-integracao-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // As opções de observabilidade são lidas na montagem do contêiner (antes de
        // ConfigureAppConfiguration), por isso precisam vir por UseSetting.
        builder.UseSetting("ServiceToken", ServiceToken);
        builder.UseSetting("Observability:LogFilePath",
            Path.Combine(Path.GetTempPath(), "vitalis-testes", "teste-.log"));
        builder.UseSetting("Observability:ExternalServices:0:Name", ServicoExternoFake);
        builder.UseSetting("Observability:ExternalServices:0:Url", UrlDoServicoExternoFake);
        builder.UseSetting("Observability:ExternalServices:0:TimeoutSeconds", "1");
        builder.UseSetting("Observability:ExternalServices:0:Critical", "false");

        builder.ConfigureServices(services =>
        {
            RemoverRegistroDoOracle(services);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    /// <summary>
    /// Remove todo o registro do <see cref="AppDbContext"/> feito no <c>Program.cs</c> — inclusive a
    /// configuração de opções que aplica o <c>UseOracle</c> —, já que o EF Core não permite dois
    /// providers de banco no mesmo provedor de serviços.
    /// </summary>
    private static void RemoverRegistroDoOracle(IServiceCollection services)
    {
        var registrosDoContexto = services
            .Where(descriptor =>
                descriptor.ServiceType == typeof(AppDbContext) ||
                descriptor.ServiceType == typeof(DbContextOptions) ||
                descriptor.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                EhConfiguracaoDeOpcoesDoContexto(descriptor.ServiceType))
            .ToList();

        foreach (var registro in registrosDoContexto)
            services.Remove(registro);
    }

    /// <summary>
    /// Identifica o <c>IDbContextOptionsConfiguration&lt;AppDbContext&gt;</c> registrado pelo
    /// <c>AddDbContext</c> — é ele que carrega o <c>UseOracle</c> e precisa sair junto.
    /// </summary>
    private static bool EhConfiguracaoDeOpcoesDoContexto(Type serviceType)
        => serviceType.IsGenericType
           && serviceType.GetGenericTypeDefinition().Name == "IDbContextOptionsConfiguration`1"
           && serviceType.GenericTypeArguments[0] == typeof(AppDbContext);

    /// <summary>Cria um escopo com o <see cref="AppDbContext"/> da aplicação sob teste.</summary>
    public IServiceScope CreateDbScope() => Services.CreateScope();

    /// <summary>Executa uma ação sobre o banco em memória da aplicação.</summary>
    public void ComBanco(Action<AppDbContext> acao)
    {
        using var scope = CreateDbScope();
        acao(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>Limpa todas as tabelas entre cenários de teste.</summary>
    public void LimparBanco() => ComBanco(context =>
    {
        context.Lembretes.RemoveRange(context.Lembretes);
        context.Contatos.RemoveRange(context.Contatos);
        context.Enderecos.RemoveRange(context.Enderecos);
        context.Responsavels.RemoveRange(context.Responsavels);
        context.SaveChanges();
        context.ChangeTracker.Clear();
    });

    /// <summary>Cria um <see cref="HttpClient"/> já com o header <c>X-Service-Token</c> preenchido.</summary>
    public HttpClient CreateClientComServiceToken(string token = ServiceToken)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Service-Token", token);
        return client;
    }
}

/// <summary>
/// Collection fixture: a aplicação é iniciada uma única vez e compartilhada por todas as
/// classes de teste de integração, reduzindo drasticamente o tempo total de execução.
/// </summary>
[CollectionDefinition(Name)]
public class VitalisApiCollection : ICollectionFixture<VitalisWebApplicationFactory>
{
    public const string Name = "API Vitalis em memória";
}
