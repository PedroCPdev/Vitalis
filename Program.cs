// Importa o Entity Framework para configuração do contexto de dados
using Microsoft.EntityFrameworkCore;
// Importa as verificações de saúde da aplicação
using Vitalis.Health;
// Importa os middlewares customizados da aplicação
using Vitalis.Middlewares;
// Importa a classe estática de métricas e tracing customizados
using Vitalis.Observabilidade;
// Importa os repositórios da aplicação
using Vitalis.Repositories;
// Importa o namespace principal do Serilog
using Serilog;
// Importa os recursos do OpenTelemetry
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

// Inicializa o construtor da aplicação Web ASP.NET Core
var builder = WebApplication.CreateBuilder(args);

// Configuração do Logger global do Serilog
Log.Logger = new LoggerConfiguration()
    // Define o nível mínimo de detalhamento dos logs como Information
    .MinimumLevel.Information()
    // Habilita a captura de propriedades injetadas pelo LogContext (ex: CorrelationId)
    .Enrich.FromLogContext()
    // Configura a gravação no Console com template formatado
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    // Configura a gravação em arquivos de texto na pasta logs/ com rotação diária
    .WriteTo.File("logs/vitalis-.log", rollingInterval: RollingInterval.Day)
    // Cria a instância do logger
    .CreateLogger();

// Substitui o provedor de logging padrão da Microsoft pelo Serilog
builder.Host.UseSerilog();

// Registra o contexto do Entity Framework apontando para o banco Oracle
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection")));

// Registra os repositórios da aplicação
builder.Services.AddScoped<IResponsavelRepository,         ResponsavelRepository>();
builder.Services.AddScoped<IResponsavelEnderecoRepository, ResponsavelEnderecoRepository>();
builder.Services.AddScoped<IResponsavelContatoRepository,  ResponsavelContatoRepository>();
builder.Services.AddScoped<ILembreteRepository,            LembreteRepository>();

builder.Services.AddControllers();
builder.Services.AddControllersWithViews();

// Habilita a leitura de metadados das rotas para gerar documentação OpenAPI
builder.Services.AddEndpointsApiExplorer();
// Adiciona o serviço gerador de interface gráfica e documentação do Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "Vitalis API",
        Version     = "v1",
        Description = "API do domínio do Responsavel — PetHub"
    });
});

// Registra o HttpClient usado pela verificação de saúde do serviço externo
builder.Services.AddHttpClient();

// Registra os serviços de Health Check e adiciona as verificações da aplicação
builder.Services.AddHealthChecks()
    .AddCheck<BancoDadosHealthCheck>("banco_dados")
    .AddCheck<ServicoExternoHealthCheck>("servico_externo");

// Configuração e Registro do OpenTelemetry (Tracing & Metrics)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(AplicacaoMetricas.NomeServico))
    .WithTracing(tracing =>
    {
        tracing
            // Auto-instrumentação para requisições HTTP recebidas do ASP.NET Core
            .AddAspNetCoreInstrumentation()
            // Auto-instrumentação para chamadas HTTP de saída (HttpClient)
            .AddHttpClientInstrumentation()
            // Escuta a fonte de Spans customizados criada na nossa aplicação
            .AddSource(AplicacaoMetricas.NomeServico)
            // Exporta os dados de Tracing no Console
            .AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            // Auto-instrumentação para métricas padrão do ASP.NET Core
            // (fornece tempo de resposta e status das requisições)
            .AddAspNetCoreInstrumentation()
            // Escuta o Meter customizado da aplicação
            .AddMeter(AplicacaoMetricas.NomeServico)
            // Exporta as medições no Console
            .AddConsoleExporter();
    });

// Constrói e inicializa a aplicação com as dependências registradas
var app = builder.Build();

// Habilita o middleware de logging de requisições HTTP do Serilog
app.UseSerilogRequestLogging();
// Registra o middleware customizado para gerenciamento do Correlation ID
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vitalis API v1");
    c.RoutePrefix = "swagger";
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

// Mapeia o endpoint nativo de diagnóstico de saúde
app.MapHealthChecks("/health");

app.MapStaticAssets();
app.MapControllers();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Inicia a execução do servidor web e escuta as requisições
app.Run();

// Exposta para permitir que os testes de integração usem WebApplicationFactory<Program>
public partial class Program;
