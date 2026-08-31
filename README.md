# Vitalis API

API RESTful desenvolvida em **ASP.NET Core (.NET 10)** responsável pelo domínio do **Responsável** no sistema PetHub.

O PetHub é um sistema veterinário composto por dois backends que compartilham o mesmo banco Oracle:

| Backend | Tecnologia | Responsabilidade |
|---|---|---|
| **Vitalis (este)** | C# .NET 10 | Cadastro e autenticação de Responsáveis, Endereços, Contatos e Lembretes |
| **pethub-java** | Java 21 + Spring Boot 3 | Veterinários, Pets, Consultas, Diagnósticos, Vacinas e Wearable IoT |

O app mobile consome ambos os backends. O Java chama a API do Vitalis para buscar responsáveis por CPF e para criar lembretes de eventos veterinários.

A partir da **3ª sprint** a aplicação passou a contar com uma camada completa de **monitoramento e observabilidade** (health checks, logging estruturado, tracing distribuído e métricas de desempenho) e com uma **suíte de testes automatizados** — 198 testes unitários e de integração escritos no padrão AAA.

---

## Tecnologias

**Aplicação**

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 10 + Oracle.EntityFrameworkCore
- BCrypt.Net-Next (hash de senhas)
- Swashbuckle (Swagger / OpenAPI 3.0)

**Monitoramento e observabilidade**

- `Microsoft.Extensions.Diagnostics.HealthChecks` — health checks da API, do banco e de serviços externos
- **Serilog** — logging estruturado com saída em console e arquivo, e correlação de requisições
- **OpenTelemetry** — tracing distribuído e métricas, com exporters Prometheus, OTLP e console

**Testes**

- **xUnit** — framework de testes
- **Moq** — mocking de dependências
- `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) — testes de integração
- `Microsoft.EntityFrameworkCore.InMemory` — banco em memória para os testes
- **coverlet** — cobertura de código

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Acesso ao banco Oracle (FIAP ou local)
- `dotnet-ef` instalado globalmente:

```bash
dotnet tool install --global dotnet-ef
export PATH="$PATH:$HOME/.dotnet/tools"
```

---

## Instalação e execução

```bash
# 1. Clonar o repositório
git clone https://github.com/pedrocpdev/Vitalis.git
cd Vitalis

# 2. Restaurar dependências
dotnet restore

# 3. Configurar a string de conexão em appsettings.json
# "OracleConnection": "User Id=SEU_USER;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/orcl"

# 4. Aplicar as migrations no banco
dotnet ef database update

# 5. Rodar a aplicação
dotnet run
```

A API sobe em `http://localhost:5192`.
O Swagger fica disponível em `http://localhost:5192/swagger`.

---

## Estrutura da solução

```
Vitalis/
├── Vitalis.slnx                  # Solução: API + projetos de teste
├── Controllers/
├── Dados/AppDbContext.cs
├── Dto/  Models/  Repositories/  Migrations/
├── Observability/                # Camada de monitoramento (3ª sprint)
│   ├── ObservabilityExtensions.cs      # Registro e ativação de tudo
│   ├── ObservabilityOptions.cs         # Configuração (seção "Observability")
│   ├── VitalisMetrics.cs               # ActivitySource + Meter da aplicação
│   ├── IMetricsRegistry.cs             # Agregador das métricas de desempenho
│   ├── InMemoryMetricsRegistry.cs
│   ├── MetricsSnapshot.cs
│   ├── CorrelationIdMiddleware.cs      # X-Correlation-ID
│   ├── RequestMetricsMiddleware.cs     # Tempo de resposta e taxa de erros
│   ├── ExceptionHandlingMiddleware.cs  # Erros 500 padronizados
│   └── HealthChecks/
│       ├── ApiHealthCheck.cs
│       ├── DatabaseHealthCheck.cs
│       ├── ExternalServiceHealthCheck.cs
│       └── HealthCheckResponseWriter.cs
└── tests/
    ├── Vitalis.Tests.Unit/             # Testes unitários (137)
    │   ├── Dominio/  Aplicacao/  Repositorios/  Observabilidade/
    │   └── Fixtures/
    └── Vitalis.Tests.Integration/      # Testes de integração (61)
        ├── Endpoints/  Monitoramento/
        └── Fixtures/
```

---

## Monitoramento e Observabilidade

### Health Checks

A API expõe três endpoints construídos sobre `Microsoft.Extensions.Diagnostics.HealthChecks`:

| Endpoint | O que verifica | Uso típico |
|---|---|---|
| `GET /health` | Todas as verificações registradas | Painel de monitoramento |
| `GET /health/live` | Apenas a saúde do processo da API (tag `live`) | *Liveness probe* — reinicia o contêiner se falhar |
| `GET /health/ready` | Banco de dados e serviços externos (tag `ready`) | *Readiness probe* — tira o pod do balanceador |

**Verificações registradas**

| Nome | O que faz | Falha resulta em |
|---|---|---|
| `api` | Uptime, ambiente, versão e memória gerenciada do processo | `Degraded` acima de 1 GB alocado |
| `oracle-database` | `CanConnectAsync` no `AppDbContext`, com timeout de 5s e medição de latência | `Unhealthy` (ou `Degraded` acima de 2s) |
| *(por serviço configurado)* | `GET` HTTP no serviço externo (ex.: backend Java) | `Degraded` se não crítico, `Unhealthy` se `Critical: true` |

**Códigos de status HTTP**

| Status geral | HTTP |
|---|---|
| `Healthy` / `Degraded` | `200 OK` |
| `Unhealthy` | `503 Service Unavailable` |

Um serviço externo não crítico fora do ar **degrada** a API, mas não a derruba.

**Exemplo de resposta de `GET /health`:**

```json
{
  "status": "Degraded",
  "totalDurationMs": 132.41,
  "timestamp": "2026-08-31T23:31:17.2234377+00:00",
  "checks": [
    {
      "name": "api",
      "status": "Healthy",
      "description": "API respondendo normalmente.",
      "durationMs": 3.598,
      "tags": ["live", "self"],
      "data": {
        "service": "vitalis-api",
        "version": "3.0.0",
        "environment": "Development",
        "uptimeSeconds": 31.2,
        "allocatedMemoryMb": 14
      }
    },
    {
      "name": "oracle-database",
      "status": "Healthy",
      "description": "Conexão com o banco de dados estabelecida.",
      "durationMs": 42.1,
      "tags": ["ready", "db"],
      "data": { "provider": "Oracle.EntityFrameworkCore", "latencyMs": 41.8 }
    },
    {
      "name": "pethub-java",
      "status": "Degraded",
      "description": "Serviço 'pethub-java' inacessível.",
      "durationMs": 98.6,
      "tags": ["ready", "external"],
      "data": {
        "service": "pethub-java",
        "url": "http://localhost:8080/actuator/health",
        "critical": false,
        "latencyMs": 79.5
      },
      "error": "Connection refused (localhost:8080)"
    }
  ]
}
```

Como monitorar rapidamente pelo terminal:

```bash
curl -s http://localhost:5192/health | jq
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5192/health/ready
```

### Logging estruturado (Serilog)

O Serilog substitui o logger padrão e grava em **console** e em **arquivo com rotação diária** (`logs/vitalis-YYYYMMDD.log`, 7 arquivos retidos).

- **Níveis usados:** `Information` no fluxo normal, `Warning` para respostas `4xx` e requisições lentas (acima de `SlowRequestThresholdMs`), `Error` para respostas `5xx` e exceções não tratadas, `Fatal` se a aplicação não subir.
- **Enriquecimento:** todo evento carrega `Service`, `Version`, `MachineName`, `EnvironmentName`, `CorrelationId` e `TraceId`.
- **Correlação de requisições:** o `CorrelationIdMiddleware` lê o header `X-Correlation-ID` da requisição (ou gera um novo), propaga o valor para os logs e para o span do OpenTelemetry, e o devolve no header da resposta — permitindo rastrear uma mesma chamada do app mobile até o log da API.

```bash
# A resposta ecoa o mesmo identificador enviado
curl -i -H "X-Correlation-ID: chamada-teste-42" http://localhost:5192/api/responsavel
```

Exemplo de linha de log:

```
[23:31:24 INF] [chamada-teste-42] GET /api/responsavel respondeu 200 em 12.480 ms {"MachineName": "vm", "Service": "vitalis-api", "Version": "3.0.0"}
```

Os níveis são configuráveis pela seção `Serilog:MinimumLevel` do `appsettings.json`.

### Tracing distribuído e métricas (OpenTelemetry)

O tracing instrumenta automaticamente ASP.NET Core, `HttpClient` e Entity Framework Core, além do `ActivitySource` próprio da aplicação (`Vitalis.Api`). Os endpoints de monitoramento são filtrados para não poluir os traces.

**Exporters disponíveis** (seção `Observability:Tracing`):

| Configuração | Efeito |
|---|---|
| `ConsoleExporter: true` | Imprime os spans no console (padrão em Development) |
| `OtlpEndpoint: "http://localhost:4317"` | Envia traces e métricas via OTLP para Jaeger, Tempo, Grafana, Application Insights etc. |

**Métricas de desempenho**

| Endpoint | Formato | Conteúdo |
|---|---|---|
| `GET /metrics` | Prometheus (texto) | Métricas da aplicação + instrumentação de ASP.NET Core, HttpClient e runtime .NET |
| `GET /metrics/summary` | JSON | Resumo legível: tempo de resposta e taxa de erros, no total e por endpoint |

Instrumentos publicados pela aplicação:

| Métrica | Tipo | Descrição |
|---|---|---|
| `vitalis.requests.total` | Counter | Requisições HTTP processadas |
| `vitalis.requests.errors.total` | Counter | Requisições com status `>= 400` |
| `vitalis.request.duration` | Histogram | Tempo de resposta em milissegundos |

Todos são rotulados por rota, método HTTP e status. As métricas usam o **template da rota** (`/api/responsavel/{id:long}`) em vez da URL concreta, evitando explosão de cardinalidade.

Exemplo de `GET /metrics/summary`:

```json
{
  "collectedAt": "2026-08-31T23:31:24.80+00:00",
  "uptimeSeconds": 53.8,
  "totalRequests": 124,
  "totalErrors": 6,
  "errorRate": 0.0484,
  "averageResponseTimeMs": 18.42,
  "p95ResponseTimeMs": 96.11,
  "endpoints": [
    {
      "endpoint": "GET /api/responsavel",
      "totalRequests": 80,
      "totalErrors": 0,
      "errorRate": 0,
      "averageResponseTimeMs": 11.2,
      "minResponseTimeMs": 4.1,
      "maxResponseTimeMs": 88.7,
      "p95ResponseTimeMs": 42.3
    }
  ]
}
```

Para coletar com o Prometheus, aponte um job para `/metrics`:

```yaml
scrape_configs:
  - job_name: vitalis-api
    metrics_path: /metrics
    static_configs:
      - targets: ["localhost:5192"]
```

### Configuração da observabilidade

Seção `Observability` do `appsettings.json`:

```json
{
  "Observability": {
    "ServiceName": "vitalis-api",
    "ServiceVersion": "3.0.0",
    "LogFilePath": "logs/vitalis-.log",
    "LogFileRetainedFileCount": 7,
    "SlowRequestThresholdMs": 1000,
    "Tracing": {
      "ConsoleExporter": false,
      "OtlpEndpoint": ""
    },
    "ExternalServices": [
      {
        "Name": "pethub-java",
        "Url": "http://localhost:8080/actuator/health",
        "TimeoutSeconds": 5,
        "Critical": false
      }
    ]
  }
}
```

Para monitorar outro serviço externo, basta adicionar um item em `ExternalServices` — um health check é registrado automaticamente para cada entrada.

---

## Testes automatizados

A solução tem **198 testes** divididos em dois projetos, separados por camada:

| Projeto | Testes | Escopo |
|---|---|---|
| `tests/Vitalis.Tests.Unit` | 137 | Domínio, Aplicação, Repositórios e Observabilidade — dependências isoladas com Moq e banco InMemory |
| `tests/Vitalis.Tests.Integration` | 61 | Fluxo HTTP completo via `WebApplicationFactory` |

### Como executar

```bash
# Todos os testes da solução
dotnet test

# Somente os testes unitários
dotnet test tests/Vitalis.Tests.Unit

# Somente os testes de integração
dotnet test tests/Vitalis.Tests.Integration

# Com relatório de cobertura (gera coverage.cobertura.xml em TestResults/)
dotnet test --collect:"XPlat Code Coverage"

# Saída detalhada, com o nome de cada teste executado
dotnet test --logger "console;verbosity=detailed"

# Filtrar por classe ou por nome
dotnet test --filter FullyQualifiedName~LembretesApiControllerTests
dotnet test --filter DisplayName~Unauthorized
```

Os testes **não precisam do banco Oracle**: os unitários usam o provider InMemory e os de integração substituem o `AppDbContext` por um banco em memória exclusivo de cada execução.

Para visualizar a cobertura em HTML:

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

### Padrão AAA e nomenclatura

Todos os testes seguem o padrão **AAA (Arrange, Act, Assert)**, com as três etapas explicitamente comentadas:

```csharp
[Fact]
public void Cadastrar_ComCpfJaExistente_RetornaConflictSemPersistir()
{
    // Arrange
    var dto = TestData.NovoCadastroDto();
    _repositorio.Setup(r => r.GetByCpf(dto.Cpf)).Returns(TestData.NovoResponsavel());
    var controller = CriarController();

    // Act
    var resultado = controller.Cadastrar(dto);

    // Assert
    Assert.IsType<ConflictObjectResult>(resultado);
    _repositorio.Verify(r => r.Add(It.IsAny<Responsavel>()), Times.Never);
}
```

A nomenclatura é sempre **`MetodoTestado_Cenario_ResultadoEsperado`**:

| Exemplo | Leitura |
|---|---|
| `GetById_QuandoResponsavelNaoExiste_RetornaNotFound` | Método `GetById`, cenário "responsável não existe", esperado `404` |
| `PostLembrete_SemServiceToken_Retorna401Unauthorized` | Endpoint de criação de lembrete sem o token de integração |
| `Add_QuandoEhOPrimeiroEnderecoDoResponsavel_MarcaComoPrincipalAutomaticamente` | Regra de negócio do endereço principal |

### Organização e fixtures

Os projetos são organizados por camada, e o contexto compartilhado entre testes é montado com **Fixtures** e **Collection Fixtures** do xUnit:

| Fixture | Tipo | Compartilha |
|---|---|---|
| `InMemoryDatabaseFixture` | `ICollectionFixture` (`"Banco em memória"`) | Criação de contextos EF InMemory, isolados e com seed opcional, para todos os testes de repositório |
| `ApiConfigurationFixture` | `IClassFixture` | `IConfiguration` com o `ServiceToken` e montagem do `HttpContext` dos controllers |
| `VitalisWebApplicationFactory` | `ICollectionFixture` (`"API Vitalis em memória"`) | Uma única instância da API em memória para todos os testes de integração |
| `TestData` / `DadosDeIntegracao` | Estáticos | Fábricas de entidades e payloads usados na etapa *Arrange* |

### O que os testes cobrem

**Unitários — Domínio** (`tests/Vitalis.Tests.Unit/Dominio`)
Validações de `Responsavel`, `ResponsavelEndereco`, `ResponsavelContato` e `Lembrete`: campos obrigatórios, limites de tamanho (CPF, nome, UF, CEP), valores padrão e conversão dos enums `TipoLembrete` / `StatusLembrete`.

**Unitários — Aplicação** (`tests/Vitalis.Tests.Unit/Aplicacao`)
Os quatro controllers com repositórios mockados via Moq (`MockBehavior.Strict`): caminhos de sucesso, `404`, `400` por `ModelState` inválido, `409` de CPF duplicado, `401` de login e de `X-Service-Token`, e verificação de que o repositório **não** é chamado nos caminhos de erro.

**Unitários — Repositórios** (`tests/Vitalis.Tests.Unit/Repositorios`)
Hash BCrypt da senha no cadastro, preservação da senha no update, filtro de responsável inativo por CPF, ordenação e filtros dos lembretes, e a regra de "principal único" de endereços e contatos (incluindo a promoção automática ao remover o principal).

**Unitários — Observabilidade** (`tests/Vitalis.Tests.Unit/Observabilidade`)
Cálculo de taxa de erros, média e percentil 95 no `InMemoryMetricsRegistry`; health checks da API, do banco e de serviços externos (com `HttpMessageHandler` mockado para simular sucesso, erro e timeout); reaproveitamento e geração do `X-Correlation-ID`; e a serialização JSON do relatório de saúde.

**Integração — Endpoints** (`tests/Vitalis.Tests.Integration/Endpoints`)
Fluxo HTTP completo: cadastro e login, garantia de que a senha nunca aparece na resposta, `400` para JSON malformado e para payload incompleto, `409` de CPF duplicado, autenticação por `X-Service-Token` (ausente, inválido e válido), CRUD de lembretes, isolamento entre responsáveis nos recursos aninhados e a regra de endereço/contato principal.

**Integração — Monitoramento** (`tests/Vitalis.Tests.Integration/Monitoramento`)
Os três endpoints de health check e o particionamento por tags, a semântica `Degraded` de um serviço externo não crítico fora do ar, o formato Prometheus de `/metrics`, os contadores de `/metrics/summary` (incluindo o agrupamento por template de rota) e o eco do header de correlação.

---

## Variáveis de configuração

Em `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "OracleConnection": "User Id=...;Password=...;Data Source=oracle.fiap.com.br:1521/orcl"
  },
  "ServiceToken": "pethub-internal-secret-2025"
}
```

`ServiceToken` é o token compartilhado com o backend Java para proteger os endpoints de integração entre sistemas.

---

## Documentação das rotas

### Monitoramento

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/health` | Saúde completa da API (API + banco + serviços externos) |
| `GET` | `/health/live` | *Liveness probe* — apenas o processo da API |
| `GET` | `/health/ready` | *Readiness probe* — banco e serviços externos |
| `GET` | `/metrics` | Métricas no formato Prometheus |
| `GET` | `/metrics/summary` | Resumo JSON de tempo de resposta e taxa de erros |

### Responsáveis — `/api/responsavel`

| Método | Rota | Descrição | Auth |
|---|---|---|---|
| `GET` | `/api/responsavel` | Lista todos os responsáveis | — |
| `GET` | `/api/responsavel/{id}` | Busca responsável por ID com endereços e contatos | — |
| `GET` | `/api/responsavel/buscar?cpf={cpf}` | Busca responsável por CPF (chamado pelo Java) | `X-Service-Token` |
| `POST` | `/api/responsavel/cadastro` | Cadastra novo responsável | — |
| `POST` | `/api/responsavel/login` | Autentica responsável por e-mail e senha | — |
| `PUT` | `/api/responsavel/{id}` | Atualiza dados do responsável | — |
| `DELETE` | `/api/responsavel/{id}` | Remove responsável | — |

### Endereços — `/api/responsavel/{responsavelId}/enderecos`

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/responsavel/{responsavelId}/enderecos` | Lista endereços do responsável |
| `GET` | `/api/responsavel/{responsavelId}/enderecos/{id}` | Busca endereço por ID |
| `POST` | `/api/responsavel/{responsavelId}/enderecos` | Adiciona endereço ao responsável |
| `PUT` | `/api/responsavel/{responsavelId}/enderecos/{id}` | Atualiza endereço |
| `DELETE` | `/api/responsavel/{responsavelId}/enderecos/{id}` | Remove endereço |
| `PATCH` | `/api/responsavel/{responsavelId}/enderecos/{id}/principal` | Define como endereço principal |

> Ao marcar um endereço como principal, os demais são automaticamente desmarcados.

### Contatos — `/api/responsavel/{responsavelId}/contatos`

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/responsavel/{responsavelId}/contatos` | Lista contatos do responsável |
| `GET` | `/api/responsavel/{responsavelId}/contatos/{id}` | Busca contato por ID |
| `POST` | `/api/responsavel/{responsavelId}/contatos` | Adiciona contato ao responsável |
| `PUT` | `/api/responsavel/{responsavelId}/contatos/{id}` | Atualiza contato |
| `DELETE` | `/api/responsavel/{responsavelId}/contatos/{id}` | Remove contato |
| `PATCH` | `/api/responsavel/{responsavelId}/contatos/{id}/principal` | Define como contato principal |

### Lembretes — `/api/lembretes`

| Método | Rota | Descrição | Auth |
|---|---|---|---|
| `GET` | `/api/lembretes` | Lista todos os lembretes | — |
| `GET` | `/api/lembretes/{id}` | Busca lembrete por ID | — |
| `GET` | `/api/lembretes/responsavel/{responsavelId}` | Lista lembretes de um responsável | — |
| `GET` | `/api/lembretes/responsavel/{responsavelId}/tipo/{tipo}` | Filtra por tipo (VACINA, CONSULTA, EXAME, MEDICAMENTO, HIDRATACAO) | — |
| `POST` | `/api/lembretes` | Cria lembrete (chamado pelo Java) | `X-Service-Token` |
| `PATCH` | `/api/lembretes/{id}/status` | Atualiza status do lembrete | — |
| `DELETE` | `/api/lembretes/{id}` | Remove lembrete | — |

> No **corpo** das requisições e respostas, `tipo` e `status` trafegam como o valor numérico do enum
> (`TipoLembrete`: `0` VACINA, `1` CONSULTA, `2` EXAME, `3` MEDICAMENTO, `4` HIDRATACAO;
> `StatusLembrete`: `0` PENDENTE, `1` ENVIADO, `2` FALHOU). Na **rota** de filtro por tipo, o nome
> do enum é aceito diretamente (`.../tipo/VACINA`).

---

## Integração com o backend Java

O backend Java (`pethub-java`) chama dois endpoints deste serviço:

**Buscar responsável por CPF** — ao cadastrar um Pet na clínica:
```
GET /api/responsavel/buscar?cpf=00000000000
Header: X-Service-Token: {valor configurado}
```

**Criar lembrete** — ao registrar vacinas, consultas ou pedidos médicos:
```
POST /api/lembretes
Header: X-Service-Token: {valor configurado}
Body: { responsavelId, petId, tipo, dataAgendada, mensagem, referenciaId, referenciaTipo }
```

---

## Senhas e segurança

- Senhas dos responsáveis são armazenadas com hash **BCrypt** — nunca em texto puro
- A senha **nunca é retornada** em nenhum response da API (verificado por teste de integração)
- Endpoints de integração com o Java são protegidos por `X-Service-Token` no header
- Exceções não tratadas retornam um JSON padronizado com o `correlationId`, sem vazar *stack trace* ao cliente

---

## Integrantes

- Pedro Chasci Puga — RM565154
- Ana Flavia Camelo — RM562745
- Gustavo kenji Terada — RM562745
- João Guilherme Carvalho Novaes — RM566234
- Lucas Figueiredo Vieira — RM561342
