// Importa utilitários de diagnóstico para medir o tempo da verificação
using System.Diagnostics;
// Importa o Entity Framework para testar a conexão com o banco
using Microsoft.EntityFrameworkCore;
// Importa o namespace nativo de diagnósticos da Microsoft
using Microsoft.Extensions.Diagnostics.HealthChecks;

// Declara o namespace para os componentes de infraestrutura de saúde
namespace Vitalis.Health;

// Implementa IHealthCheck para verificar a conectividade com o banco de dados Oracle
public class BancoDadosHealthCheck : IHealthCheck
{
    // Contexto do Entity Framework usado para testar a conexão
    private readonly AppDbContext _context;

    // Recebe o contexto do banco via Injeção de Dependência
    public BancoDadosHealthCheck(AppDbContext context)
    {
        _context = context;
    }

    // Executa assincronamente a verificação da saúde da dependência
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Inicia a contagem do tempo gasto para abrir a conexão
        var cronometro = Stopwatch.StartNew();

        try
        {
            // Verifica se o banco de dados Oracle está acessível
            bool conexaoOk = await _context.Database.CanConnectAsync(cancellationToken);

            // Encerra a contagem do tempo da verificação
            cronometro.Stop();

            // Avalia o resultado da conexão e retorna o status correspondente
            if (conexaoOk)
            {
                // Retorna status Healthy (Saudável) com metadados adicionais de latência
                return HealthCheckResult.Healthy(
                    "Conexão com o Banco de Dados estabelecida com sucesso.",
                    data: new Dictionary<string, object> { { "LatenciaMs", cronometro.ElapsedMilliseconds } });
            }

            // Retorna status Unhealthy (Insaudável) alertando falha na infraestrutura
            return HealthCheckResult.Unhealthy("Falha ao conectar no Banco de Dados (Oracle).");
        }
        catch (Exception ex)
        {
            // Retorna status Unhealthy anexando a exceção que impediu a conexão
            return HealthCheckResult.Unhealthy("Falha ao conectar no Banco de Dados (Oracle).", ex);
        }
    }
}
