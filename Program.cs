using Microsoft.EntityFrameworkCore;
using Serilog;
using Vitalis.Observability;
using Vitalis.Repositories;

// Logger de bootstrap: garante que falhas ocorridas antes da configuração
// definitiva do Serilog também sejam registradas.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Monitoramento e observabilidade: Serilog, health checks e OpenTelemetry.
    builder.AddVitalisObservability();

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection")));

    builder.Services.AddScoped<IResponsavelRepository, ResponsavelRepository>();
    builder.Services.AddScoped<IResponsavelEnderecoRepository, ResponsavelEnderecoRepository>();
    builder.Services.AddScoped<IResponsavelContatoRepository, ResponsavelContatoRepository>();
    builder.Services.AddScoped<ILembreteRepository, LembreteRepository>();

    builder.Services.AddControllers();
    builder.Services.AddControllersWithViews();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title       = "Vitalis API",
            Version     = "v1",
            Description = "API do domínio do Responsavel — PetHub"
        });
    });

    var app = builder.Build();

    app.UseVitalisObservability();

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

    // O host de testes serve apenas HTTP; o redirecionamento tornaria as asserções inúteis.
    if (!app.Environment.IsEnvironment("Testing"))
        app.UseHttpsRedirection();

    app.UseRouting();
    app.UseAuthorization();

    app.MapStaticAssets();
    app.MapControllers();
    app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.MapVitalisMonitoringEndpoints();

    Log.Information("Vitalis API iniciando no ambiente {Environment}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "A Vitalis API encerrou de forma inesperada");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Exposta para permitir que os testes de integração usem <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program;
