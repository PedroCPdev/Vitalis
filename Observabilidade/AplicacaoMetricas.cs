// Importa utilitários de diagnósticos nativos do .NET para métricas e tracing
using System.Diagnostics;
using System.Diagnostics.Metrics;

// Declara o namespace para os componentes de observabilidade da aplicação
namespace Vitalis.Observabilidade;

// Classe centralizadora de Métricas Customizadas e Traces da aplicação
public static class AplicacaoMetricas
{
    // Define o nome do serviço utilizado para identificação no OpenTelemetry
    public const string NomeServico = "Vitalis.API";

    // Inicializa o Meter nativo do .NET para registro de métricas customizadas
    public static readonly Meter MeterAplicacao = new(NomeServico, "1.0.0");

    // Cria um Contador para registrar o total de responsáveis cadastrados na API
    public static readonly Counter<long> ResponsaveisCadastradosContador =
        MeterAplicacao.CreateCounter<long>(
            name: "responsaveis_cadastrados_total",
            unit: "{responsaveis}",
            description: "Contagem total de responsáveis cadastrados na API");

    // Cria um Contador para registrar o total de lembretes criados na API
    public static readonly Counter<long> LembretesCriadosContador =
        MeterAplicacao.CreateCounter<long>(
            name: "lembretes_criados_total",
            unit: "{lembretes}",
            description: "Contagem total de lembretes criados na API");

    // Inicializa o ActivitySource para criação de Spans manuais de Tracing Distribuído
    public static readonly ActivitySource ActivitySourceAplicacao = new(NomeServico);
}
