// Importa utilitários de diagnóstico para medir o tempo da verificação
using System.Diagnostics;
// Importa o namespace nativo de diagnósticos da Microsoft
using Microsoft.Extensions.Diagnostics.HealthChecks;

// Declara o namespace para os componentes de infraestrutura de saúde
namespace Vitalis.Health;

// Implementa IHealthCheck para verificar a disponibilidade do serviço externo (backend Java do PetHub)
public class ServicoExternoHealthCheck : IHealthCheck
{
    // Fábrica usada para criar o HttpClient da verificação
    private readonly IHttpClientFactory _httpClientFactory;
    // Configuração de onde é lida a URL do serviço externo
    private readonly IConfiguration _configuration;

    // Recebe as dependências via Injeção de Dependência
    public ServicoExternoHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    // Executa assincronamente a verificação da saúde da dependência
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Lê do appsettings.json a URL de health do backend Java
        var url = _configuration["ServicosExternos:PethubJava"];

        // Sem URL configurada não há o que verificar
        if (string.IsNullOrWhiteSpace(url))
            return HealthCheckResult.Unhealthy("URL do serviço externo não configurada.");

        // Inicia a contagem do tempo de resposta do serviço externo
        var cronometro = Stopwatch.StartNew();

        try
        {
            // Cria o cliente HTTP e consulta o serviço externo
            var client = _httpClientFactory.CreateClient();
            var resposta = await client.GetAsync(url, cancellationToken);

            // Encerra a contagem do tempo da verificação
            cronometro.Stop();

            // Avalia o status HTTP devolvido pelo serviço externo
            if (resposta.IsSuccessStatusCode)
            {
                // Retorna status Healthy com metadados de latência e status HTTP
                return HealthCheckResult.Healthy(
                    "Serviço externo (pethub-java) disponível.",
                    data: new Dictionary<string, object>
                    {
                        { "LatenciaMs", cronometro.ElapsedMilliseconds },
                        { "StatusCode", (int)resposta.StatusCode }
                    });
            }

            // Retorna status Unhealthy informando o código HTTP recebido
            return HealthCheckResult.Unhealthy(
                $"Serviço externo (pethub-java) respondeu {(int)resposta.StatusCode}.");
        }
        catch (Exception ex)
        {
            // Retorna status Unhealthy anexando a exceção que impediu a chamada
            return HealthCheckResult.Unhealthy("Serviço externo (pethub-java) inacessível.", ex);
        }
    }
}
