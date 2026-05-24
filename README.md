# Vitalis API

API RESTful desenvolvida em **ASP.NET Core (.NET 10)** responsável pelo domínio do **Responsável** no sistema PetHub.

O PetHub é um sistema veterinário composto por dois backends que compartilham o mesmo banco Oracle:

| Backend | Tecnologia | Responsabilidade |
|---|---|---|
| **Vitalis (este)** | C# .NET 10 | Cadastro e autenticação de Responsáveis, Endereços, Contatos e Lembretes |
| **pethub-java** | Java 21 + Spring Boot 3 | Veterinários, Pets, Consultas, Diagnósticos, Vacinas e Wearable IoT |

O app mobile consome ambos os backends. O Java chama a API do Vitalis para buscar responsáveis por CPF e para criar lembretes de eventos veterinários.

---

## Tecnologias

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- Oracle.EntityFrameworkCore
- BCrypt.Net-Next (hash de senhas)
- Swashbuckle (Swagger / OpenAPI 3.0)

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

### Responsáveis — `/api/responsaveis`

| Método | Rota | Descrição | Auth |
|---|---|---|---|
| `GET` | `/api/responsaveis` | Lista todos os responsáveis | — |
| `GET` | `/api/responsaveis/{id}` | Busca responsável por ID com endereços e contatos | — |
| `GET` | `/api/responsaveis/buscar?cpf={cpf}` | Busca responsável por CPF (chamado pelo Java) | `X-Service-Token` |
| `POST` | `/api/responsaveis/cadastro` | Cadastra novo responsável | — |
| `POST` | `/api/responsaveis/login` | Autentica responsável por e-mail e senha | — |
| `PUT` | `/api/responsaveis/{id}` | Atualiza dados do responsável | — |
| `DELETE` | `/api/responsaveis/{id}` | Remove responsável | — |

---

### Endereços — `/api/responsaveis/{responsavelId}/enderecos`

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/responsaveis/{responsavelId}/enderecos` | Lista endereços do responsável |
| `GET` | `/api/responsaveis/{responsavelId}/enderecos/{id}` | Busca endereço por ID |
| `POST` | `/api/responsaveis/{responsavelId}/enderecos` | Adiciona endereço ao responsável |
| `PUT` | `/api/responsaveis/{responsavelId}/enderecos/{id}` | Atualiza endereço |
| `DELETE` | `/api/responsaveis/{responsavelId}/enderecos/{id}` | Remove endereço |
| `PATCH` | `/api/responsaveis/{responsavelId}/enderecos/{id}/principal` | Define como endereço principal |

> Ao marcar um endereço como principal, os demais são automaticamente desmarcados.

---

### Contatos — `/api/responsaveis/{responsavelId}/contatos`

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/responsaveis/{responsavelId}/contatos` | Lista contatos do responsável |
| `GET` | `/api/responsaveis/{responsavelId}/contatos/{id}` | Busca contato por ID |
| `POST` | `/api/responsaveis/{responsavelId}/contatos` | Adiciona contato ao responsável |
| `PUT` | `/api/responsaveis/{responsavelId}/contatos/{id}` | Atualiza contato |
| `DELETE` | `/api/responsaveis/{responsavelId}/contatos/{id}` | Remove contato |
| `PATCH` | `/api/responsaveis/{responsavelId}/contatos/{id}/principal` | Define como contato principal |

---

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

---

## Integração com o backend Java

O backend Java (`pethub-java`) chama dois endpoints deste serviço:

**Buscar responsável por CPF** — ao cadastrar um Pet na clínica:
```
GET /api/responsaveis/buscar?cpf=00000000000
Header: X-Service-Token: {valor configurado}
```

**Criar lembrete** — ao registrar vacinas, consultas ou pedidos médicos:
```
POST /api/lembretes
Header: X-Service-Token: {valor configurado}
Body: { responsavelId, petId, tipo, dataAgendada, mensagem, referenciaId, referenciaTipo }
```

---

## Estrutura do projeto

```
Vitalis/
├── Controllers/
│   ├── ResponsaveisApiController.cs
│   ├── ResponsavelEnderecoController.cs
│   ├── ResponsavelContatoController.cs
│   └── LembretesApiController.cs
├── Dados/
│   └── AppDbContext.cs
├── Dto/
│   ├── CadastrarResponsavelDto.cs
│   ├── LoginDto.cs
│   ├── CriarLembreteDto.cs
│   └── AtualizarStatusDto.cs
├── Models/
│   ├── Responsavel.cs
│   ├── ResponsavelEndereco.cs
│   ├── ResponsavelContato.cs
│   ├── Lembrete.cs
│   └── Enums.cs
├── Repositories/
│   ├── IResponsavelRepository.cs / ResponsavelRepository.cs
│   ├── IResponsavelEnderecoRepository.cs / ResponsavelEnderecoRepository.cs
│   ├── IResponsavelContatoRepository.cs / ResponsavelContatoRepository.cs
│   └── ILembreteRepository.cs / LembreteRepository.cs
├── Migrations/
├── appsettings.json
└── Program.cs
```

---

## Senhas e segurança

- Senhas dos responsáveis são armazenadas com hash **BCrypt** — nunca em texto puro
- A senha **nunca é retornada** em nenhum response da API
- Endpoints de integração com o Java são protegidos por `X-Service-Token` no header

---

## Integrantes

- Pedro Chasci Puga — RM565154
- Ana Flavia Camelo — RM562745
- Gustavo kenji Terada — RM562745
- João Guilherme Carvalho Novaes — RM566234
- Lucas Figueiredo Vieira — RM561342

