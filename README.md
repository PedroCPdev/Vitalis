# Vitalis API

API RESTful desenvolvida em **ASP.NET Core (.NET 10)** responsável pelo domínio do **Tutor** no sistema PetHub.

O PetHub é um sistema veterinário composto por dois backends que compartilham o mesmo banco Oracle:

| Backend | Tecnologia | Responsabilidade |
|---|---|---|
| **Vitalis (este)** | C# .NET 10 | Cadastro e autenticação de Tutores, Endereços, Contatos e Lembretes |
| **pethub-java** | Java 21 + Spring Boot 3 | Veterinários, Pets, Consultas, Diagnósticos, Vacinas e Wearable IoT |

O app mobile consome ambos os backends. O Java chama a API do Vitalis para buscar tutores por CPF e para criar lembretes de eventos veterinários.

---

## Tecnologias

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- Oracle.EntityFrameworkCore
- BCrypt.Net-Next (hash de senhas)
- Swashbuckle (Swagger / OpenAPI)

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
git clone https://github.com/seu-usuario/seu-repositorio.git
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

### Tutores — `/api/tutores`

| Método | Rota | Descrição | Auth |
|---|---|---|---|
| `GET` | `/api/tutores` | Lista todos os tutores | — |
| `GET` | `/api/tutores/{id}` | Busca tutor por ID com endereços e contatos | — |
| `GET` | `/api/tutores/buscar?cpf={cpf}` | Busca tutor por CPF (chamado pelo Java) | `X-Service-Token` |
| `POST` | `/api/tutores/cadastro` | Cadastra novo tutor | — |
| `POST` | `/api/tutores/login` | Autentica tutor por e-mail e senha | — |
| `PUT` | `/api/tutores/{id}` | Atualiza dados do tutor | — |
| `DELETE` | `/api/tutores/{id}` | Remove tutor | — |

---

### Endereços — `/api/tutores/{tutorId}/enderecos`

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/tutores/{tutorId}/enderecos` | Lista endereços do tutor |
| `GET` | `/api/tutores/{tutorId}/enderecos/{id}` | Busca endereço por ID |
| `POST` | `/api/tutores/{tutorId}/enderecos` | Adiciona endereço ao tutor |
| `PUT` | `/api/tutores/{tutorId}/enderecos/{id}` | Atualiza endereço |
| `DELETE` | `/api/tutores/{tutorId}/enderecos/{id}` | Remove endereço |
| `PATCH` | `/api/tutores/{tutorId}/enderecos/{id}/principal` | Define como endereço principal |

> Ao marcar um endereço como principal, os demais são automaticamente desmarcados.

---

### Contatos — `/api/tutores/{tutorId}/contatos`

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/tutores/{tutorId}/contatos` | Lista contatos do tutor |
| `GET` | `/api/tutores/{tutorId}/contatos/{id}` | Busca contato por ID |
| `POST` | `/api/tutores/{tutorId}/contatos` | Adiciona contato ao tutor |
| `PUT` | `/api/tutores/{tutorId}/contatos/{id}` | Atualiza contato |
| `DELETE` | `/api/tutores/{tutorId}/contatos/{id}` | Remove contato |
| `PATCH` | `/api/tutores/{tutorId}/contatos/{id}/principal` | Define como contato principal |

---

### Lembretes — `/api/lembretes`

| Método | Rota | Descrição | Auth |
|---|---|---|---|
| `GET` | `/api/lembretes` | Lista todos os lembretes | — |
| `GET` | `/api/lembretes/{id}` | Busca lembrete por ID | — |
| `GET` | `/api/lembretes/tutor/{tutorId}` | Lista lembretes de um tutor | — |
| `GET` | `/api/lembretes/tutor/{tutorId}/tipo/{tipo}` | Filtra por tipo (VACINA, CONSULTA, EXAME, MEDICAMENTO) | — |
| `POST` | `/api/lembretes` | Cria lembrete (chamado pelo Java) | `X-Service-Token` |
| `PATCH` | `/api/lembretes/{id}/status` | Atualiza status do lembrete | — |
| `DELETE` | `/api/lembretes/{id}` | Remove lembrete | — |

---

## Integração com o backend Java

O backend Java (`pethub-java`) chama dois endpoints deste serviço:

**Buscar tutor por CPF** — ao cadastrar um Pet na clínica:
```
GET /api/tutores/buscar?cpf=00000000000
Header: X-Service-Token: {valor configurado}
```

**Criar lembrete** — ao registrar vacinas, consultas ou pedidos médicos:
```
POST /api/lembretes
Header: X-Service-Token: {valor configurado}
Body: { tutorId, petId, tipo, dataAgendada, mensagem, referenciaId, referenciaTipo }
```

---

## Estrutura do projeto

```
Vitalis/
├── Controllers/
│   ├── TutoresApiController.cs
│   ├── TutorEnderecoController.cs
│   ├── TutorContatoController.cs
│   └── LembretesApiController.cs
├── Dados/
│   └── AppDbContext.cs
├── Dto/
│   ├── CadastrarTutorDto.cs
│   ├── LoginDto.cs
│   ├── CriarLembreteDto.cs
│   └── AtualizarStatusDto.cs
├── Models/
│   ├── Tutor.cs
│   ├── TutorEndereco.cs
│   ├── TutorContato.cs
│   ├── Lembrete.cs
│   └── Enums.cs
├── Repositories/
│   ├── ITutorRepository.cs / TutorRepository.cs
│   ├── ITutorEnderecoRepository.cs / TutorEnderecoRepository.cs
│   ├── ITutorContatoRepository.cs / TutorContatoRepository.cs
│   └── ILembreteRepository.cs / LembreteRepository.cs
├── Migrations/
├── appsettings.json
└── Program.cs
```

---

## Senhas e segurança

- Senhas de tutores são armazenadas com hash **BCrypt** — nunca em texto puro
- A senha **nunca é retornada** em nenhum response da API
- Endpoints de integração com o Java são protegidos por `X-Service-Token` no header

---

## Integrantes

- Pedro — RM565154
