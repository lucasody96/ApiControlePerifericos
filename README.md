# ApiControlePerifericos

API REST para controle de periféricos e hardwares em coworkings. Permite gerenciar o estoque de equipamentos, rastrear quais colaboradores retiraram cada item e manter o saldo de cada produto sempre consistente através de movimentações de entrada, saída e ajuste.

O sistema está **em produção**, hospedado gratuitamente:

- **Frontend** (React/Vite) na Vercel — código em repositório dedicado (privado)
- **API** (esta solução) no Google Cloud Run
- **Banco** no TiDB Cloud (MySQL-compatível)

## Stack Tecnológico

| Tecnologia | Versão | Finalidade |
|---|---|---|
| .NET / ASP.NET Core | 10.0 | Framework principal |
| Entity Framework Core | 9.0.14 | ORM |
| Pomelo.EntityFrameworkCore.MySql | 9.0.0 | Driver MySQL / TiDB |
| ASP.NET Core Identity | 9.0.16 | Gestão de usuários e roles |
| JWT Bearer + System.IdentityModel.Tokens.Jwt | 10.0.8 / 8.16.0 | Autenticação por token |
| AutoMapper | 16.1.1 | Mapeamento Model ↔ DTO |
| X.PagedList | 10.5.9 | Paginação server-side |
| Microsoft.Extensions.Caching.Memory | 10.0.0 | Cache em memória (decorator) |
| Scalar.AspNetCore | 2.14.14 | UI de documentação da API |
| Newtonsoft.Json | 13.0.4 | Serialização JSON |
| xUnit + Moq | — | Testes unitários |

**Frontend:** SPA em React + Vite, mantida em **repositório separado** e consumida por esta API via CORS.

## Domínios

O sistema possui três entidades principais de negócio:

- **Produto** — hardware ou periférico disponível para retirada (notebook, mouse, teclado, etc.), com `SaldoAtual` e `EstoqueMinimo`.
- **Colaborador** — pessoa cadastrada que pode retirar equipamentos.
- **Movimentacao** — registro que **altera o saldo do produto**: entrada (`E`), saída (`S`) ou ajuste (`A`). A regra de saldo vive na camada de serviço (`EstoqueService`), não no controller.

Além disso, a API gerencia **usuários e roles** (ASP.NET Identity) para autenticação e autorização.

## Arquitetura — Clean Architecture (4 camadas)

A solução (`ApiControlePerifericos.slnx`, formato `.slnx`) é dividida em quatro projetos, com as dependências apontando sempre **para dentro**:

```
ApiControlePerifericos            (WebApi / Presentation)  → Application, Infrastructure, Domain
ApiControlePerifericos.Infrastructure                      → Application, Domain
ApiControlePerifericos.Application                         → Domain
ApiControlePerifericos.Domain                              → (nenhuma)
```

- **Domain** — entidades (`Models/`), interfaces de repositório (`IRepository<T>`, `IUnitOfWork`, repos especializados) e a `Pagination/`.
- **Application** — casos de uso e contratos: `EstoqueService` + `IEstoqueService`, todos os DTOs e o `MappingProfile` (AutoMapper).
- **Infrastructure** — detalhes técnicos: `AppDbContext`, repositórios (inclusive os decorators de cache), migrations, `TokenService` e `ApplicationUser` (Identity).
- **WebApi** (projeto `ApiControlePerifericos`, mantém o nome por causa do Dockerfile/deploy) — `Controllers/`, `Filters/`, `Auth/`, `Logging/` e `Program.cs` (composition root).

> **Convenção de namespaces:** os namespaces foram **mantidos** como `ApiControlePerifericos.*` e não refletem a camada. O que materializa as camadas é a separação física em assemblies + a direção das referências de projeto.

### Padrões centrais

- **Repository + Unit of Work** — toda persistência passa por `IUnitOfWork`, que expõe os três repositórios especializados e o `CommitAsync()`. Nunca se chama `SaveChangesAsync()` direto no `DbContext`.
- **Generic Repository** — `Repository<T>` implementa o CRUD básico; todas as leituras usam `AsNoTracking()`. Para updates rastreados (ex.: alterar `SaldoAtual`), os repos especializados saem do caminho genérico (`GetByIdTrackedAsync`).
- **AutoMapper** — toda conversão Model ↔ DTO passa pelo mapper injetado nos controllers.
- **Cache (decorator)** — as leituras de lista/paginação de **Produto** e **Colaborador** são cacheadas em `IMemoryCache` via `CachedProdutoRepository`/`CachedColaboradorRepository` (TTL 5 min). Cada escrita invalida o grupo inteiro via `CancellationChangeToken`. Como a `Movimentacao` altera o saldo sem passar pelo `Update`, o `EstoqueService` invalida o cache de produtos após o commit.

### Camada de serviço: EstoqueService

`EstoqueService` concentra a regra de movimentação. Toda operação grava a `Movimentacao` **e** recalcula o `SaldoAtual` do produto na **mesma transação**:

- `RegistrarEntradaAsync` — soma ao saldo, `Tipo = 'E'`, sem colaborador.
- `RegistrarSaidaAsync` — subtrai do saldo, exige colaborador, `Tipo = 'S'`; valida saldo suficiente.
- `RegistrarAjusteAsync` — subtrai do saldo (perda/quebra), `Tipo = 'A'`, sem colaborador; valida saldo.

Em vez de exceções, retorna um `EstoqueResult` com um `EstoqueResultStatus` (`Sucesso`, `ProdutoNaoEncontrado`, `ColaboradorNaoEncontrado`, `SaldoInsuficiente`), que o controller mapeia para o HTTP adequado (404 / 400 / 201).

### Fluxo de uma requisição

```
Controller → IUnitOfWork → IRepositorioEspecializado → Repository<T> → AppDbContext → MySQL/TiDB
                                  (Produto/Colaborador passam por um decorator de cache)
```

## Estrutura de Pastas

```
ApiControlePerifericos.Domain/
├── Models/                        # Produto, Colaborador, Movimentacao
├── Interfaces/                    # IRepository<T>, IUnitOfWork, I{Produto,Colaborador,Movimentacao}Repository
└── Pagination/                    # QueryStringParameters (base) + filtros por recurso

ApiControlePerifericos.Application/
├── Services/                      # EstoqueService, EstoqueResult
├── Interfaces/                    # IEstoqueService, IProdutoCacheInvalidator
└── DTOs/
    ├── Estoque/                   # EntradaEstoqueRequest, SaidaEstoqueRequest, AjusteEstoqueRequest
    ├── Identity/                  # LoginRequest, RegisterRequest, ChangePasswordRequest,
    │                              #   AdminResetPasswordRequest, AtualizarRolesRequest,
    │                              #   UsuarioResponse, Response
    ├── ProdutoDTO.cs, ColaboradorDTO.cs, MovimentacaoDTO.cs, MovimentacaoRelatorioDTO.cs
    └── Mappings/MappingProfile.cs

ApiControlePerifericos.Infrastructure/
├── Context/AppDbContext.cs        # IdentityDbContext<ApplicationUser>
├── Repositories/                  # Repository<T>, UnitOfWork, repos + decorators de cache
├── Caching/                       # CacheGrupos, CacheTokens, ProdutoCacheInvalidator
├── Migrations/
├── Interfaces/ITokenService.cs
├── Services/TokenService.cs
└── Models/Identity/ApplicationUser.cs

ApiControlePerifericos/            # WebApi (startup)
├── Controllers/                   # Produtos, Colaboradores, Movimentacoes, Auth, Usuarios
├── Filters/                       # ApiExceptionFilter
├── Auth/                          # AuthCookies, CsrfValidationMiddleware
├── Logging/                       # CustomLoggerProvider → Log.txt
├── Program.cs                     # Composition root (DI + pipeline)
└── appsettings.json

ApiControlePerifericos.Tests/      # xUnit + Moq
├── Controllers/
├── Repositories/
└── Services/
```

> O **frontend** (React + Vite) fica em repositório próprio — não faz parte desta solução.

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MySQL 8+ local (ou MariaDB compatível); em produção é TiDB Cloud
- [EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

## Como Executar

### 1. Configurar segredos via User Secrets

Apenas os segredos ficam em User Secrets — `ConnectionStrings:DefaultConnection`, `JWT:SecretKey` e o seed de admin:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost;Database=ControlePerifericos;User=root;Password=suasenha;" \
  --project ApiControlePerifericos

dotnet user-secrets set "JWT:SecretKey" "uma-chave-secreta-bem-grande" --project ApiControlePerifericos
```

> Configurações JWT não-secretas (`ValidIssuer`, `ValidAudience`, `TokenValidityInMinutes`, `RefreshTokenValidityInMinutes`) e a allowlist `SuperAdmins` ficam em `appsettings.json`.

### 2. Rodar a API

As migrations são aplicadas automaticamente no startup (`db.Database.MigrateAsync()`), seguidas do seed de roles e admin. Basta rodar:

```bash
dotnet run --project ApiControlePerifericos
```

A API sobe em:
- **HTTP:** `http://localhost:5045`
- **HTTPS:** `https://localhost:7081`
- **Documentação interativa (Scalar):** `http://localhost:5045/scalar/v1`

> O **frontend** (React + Vite) vive em repositório próprio, com instruções de execução no README dele.

### Build

```bash
dotnet build ApiControlePerifericos.slnx
```

### Testes

```bash
dotnet test ApiControlePerifericos.Tests
# Um único filtro:
dotnet test ApiControlePerifericos.Tests --filter "FullyQualifiedName~EstoqueServiceTests"
```

### Migrations

O `DbContext` vive na Infrastructure e o host na WebApi — passe os dois projetos:

```bash
dotnet ef migrations add <NomeDaMigration> \
  --project ApiControlePerifericos.Infrastructure --startup-project ApiControlePerifericos

dotnet ef database update \
  --project ApiControlePerifericos.Infrastructure --startup-project ApiControlePerifericos
```

## Autenticação & Autorização

A API usa **JWT** com ASP.NET Identity, mas o token **não trafega no corpo nem no header** `Authorization`: após o `login`, o access token e o refresh token são gravados em **cookies `httpOnly`** (inacessíveis ao JavaScript, mitigando XSS) e enviados automaticamente pelo navegador. O corpo do `login` devolve apenas `{ username, roles }`.

- **Proteção CSRF (double-submit):** como a sessão vive em cookie, requisições que **alteram estado** (POST/PUT/DELETE) exigem um token anti-CSRF — um cookie legível pelo JS (`httpOnly: false`) que o frontend reenvia num header, validado pelo `CsrfValidationMiddleware`.
- **Rate limiting:** `/login` e `/refresh-token` têm limite de tentativas por IP (janela fixa); ao estourar, retornam `429 Too Many Requests` com `Retry-After`.

### Endpoints de Auth — `/api/Auth`

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `POST` | `/api/Auth/login` | Público | Valida credenciais, grava os tokens em cookies `httpOnly` e devolve `{ username, roles }` |
| `POST` | `/api/Auth/refresh-token` | Público | Lê o par de tokens dos cookies e o renova (novo access + refresh) |
| `GET` | `/api/Auth/me` | Autenticado | Retorna `username` + roles da sessão (o token é httpOnly; o front não o decodifica) |
| `POST` | `/api/Auth/logout` | Autenticado | Limpa os cookies de autenticação e revoga o refresh token |
| `POST` | `/api/Auth/Register` | AdminOnly | Cria usuário e o adiciona à role `User` |
| `POST` | `/api/Auth/change-password` | Autenticado | Troca a própria senha (exige a atual) |
| `POST` | `/api/Auth/reset-password` | AdminOnly | Reseta a senha de outro usuário (um Admin comum não reseta a de um super admin) |
| `POST` | `/api/Auth/CreateRole` | SuperAdminOnly | Cria uma role |
| `POST` | `/api/Auth/AddUserToRole` | SuperAdminOnly | Adiciona usuário a uma role |
| `POST` | `/api/Auth/revoke/{username}` | SuperAdminOnly | Revoga o refresh token do próprio usuário |

### Gestão de usuários — `/api/Usuarios` (controller inteiro `AdminOnly`)

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/Usuarios` | AdminOnly | Lista usuários + suas roles |
| `GET` | `/api/Usuarios/pagination` | AdminOnly | Lista paginada (busca case-insensitive) |
| `GET` | `/api/Usuarios/roles` | AdminOnly | Lista as roles |
| `PUT` | `/api/Usuarios/{userName}/roles` | SuperAdminOnly | Altera as roles de um usuário |

### Políticas

- `AdminOnly` — `RequireRole("Admin")`.
- `SuperAdminOnly` — `RequireRole("Admin")` **e** estar na allowlist `SuperAdmins` (config; default `["lucas.ody", "admin"]`).

## Endpoints de Domínio

### Produtos — `/api/produtos`

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/produtos` | Autenticado | Lista todos os produtos |
| `GET` | `/api/produtos/{id}` | Autenticado | Retorna produto por ID |
| `GET` | `/api/produtos/pagination` | Autenticado | Lista produtos paginados |
| `GET` | `/api/produtos/abaixo-do-minimo` | Autenticado | Produtos com saldo abaixo do estoque mínimo |
| `POST` | `/api/produtos` | AdminOnly | Cadastra novo produto |
| `PUT` | `/api/produtos/{id}` | AdminOnly | Atualiza produto existente |
| `DELETE` | `/api/produtos/{id}` | SuperAdminOnly | Remove produto |

### Colaboradores — `/api/colaboradores`

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/colaboradores` | Autenticado | Lista todos os colaboradores |
| `GET` | `/api/colaboradores/{id}` | Autenticado | Retorna colaborador por ID |
| `GET` | `/api/colaboradores/pagination` | Autenticado | Lista colaboradores paginados |
| `POST` | `/api/colaboradores` | AdminOnly | Cadastra novo colaborador |
| `PUT` | `/api/colaboradores/{id}` | AdminOnly | Atualiza colaborador existente |
| `DELETE` | `/api/colaboradores/{id}` | SuperAdminOnly | Remove colaborador |

### Movimentações — `/api/movimentacoes`

Não há `POST` genérico — a escrita é feita pelos três endpoints de estoque, que delegam ao `EstoqueService`.

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/api/movimentacoes` | AdminOnly | Lista todas as movimentações |
| `GET` | `/api/movimentacoes/{id}` | AdminOnly | Retorna movimentação por ID |
| `GET` | `/api/movimentacoes/pagination` | AdminOnly | Lista paginada (data DESC) |
| `GET` | `/api/movimentacoes/relatorio` | AdminOnly | Relatório paginado (`MovimentacaoRelatorioDTO`) |
| `GET` | `/api/movimentacoes/produto/{produtoId}` | AdminOnly | Movimentações de um produto |
| `GET` | `/api/movimentacoes/colaborador/{colaboradorId}` | AdminOnly | Movimentações de um colaborador |
| `POST` | `/api/movimentacoes/entrada` | AdminOnly | Registra entrada (soma ao saldo) |
| `POST` | `/api/movimentacoes/saida` | AdminOnly | Registra saída por colaborador (subtrai) |
| `POST` | `/api/movimentacoes/ajuste` | AdminOnly | Registra ajuste/perda (subtrai) |
| `PUT` | `/api/movimentacoes/{id}` | SuperAdminOnly | Atualiza movimentação |
| `DELETE` | `/api/movimentacoes/{id}` | SuperAdminOnly | Remove movimentação |

### Parâmetros de paginação (query string)

Todos os endpoints `/pagination` aceitam:

| Parâmetro | Padrão | Máximo | Descrição |
|---|---|---|---|
| `pageNumber` | `1` | — | Número da página |
| `pageSize` | `50` | `50` | Itens por página (clamp 1–50) |

> Atenção: `pageSize` tem default **e** máximo iguais a 50 — chamar `/pagination` sem `pageSize` traz 50 itens.

`MovimentacoesParameters` aceita ainda os filtros `DataInicio`, `DataFim`, `DescricaoProduto` e `NomeColaborador`; `UsuariosParameters` aceita `Busca`.

A resposta inclui o header `X-Pagination` (exposto ao frontend via CORS) com metadados:

```json
{
  "count": 10,
  "pageSize": 10,
  "pageCount": 3,
  "totalItemCount": 30,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

## Exemplos de Uso

> Não existe header `Authorization`: depois do `login`, os cookies de sessão (`httpOnly`) vão automaticamente em cada requisição. Os exemplos de escrita abaixo (POST/PUT/DELETE) exigem ainda o header anti-CSRF `X-CSRF-Token`, com o valor do cookie `XSRF-TOKEN` gravado no login. Requisições `GET` não precisam dele.

### Login

```http
POST /api/Auth/login
Content-Type: application/json

{
  "username": "lucas.ody",
  "password": "minhaSenha"
}
```

> A resposta grava os cookies `httpOnly` de autenticação (access + refresh) e o cookie anti-CSRF; o corpo retorna apenas `{ username, roles }`.

### Cadastrar um produto

```http
POST /api/produtos
Content-Type: application/json

{
  "descricao": "Notebook Dell Inspiron 15",
  "saldoAtual": 5,
  "estoqueMinimo": 1
}
```

### Registrar entrada de estoque

```http
POST /api/movimentacoes/entrada
Content-Type: application/json

{
  "produtoId": 1,
  "quantidade": 3
}
```

### Registrar saída (retirada por colaborador)

```http
POST /api/movimentacoes/saida
Content-Type: application/json

{
  "produtoId": 1,
  "quantidade": 1,
  "colaboradorId": 2
}
```

### Registrar ajuste (perda/quebra)

```http
POST /api/movimentacoes/ajuste
Content-Type: application/json

{
  "produtoId": 1,
  "quantidade": 1
}
```

### Listar produtos com paginação

```http
GET /api/produtos/pagination?pageNumber=1&pageSize=10
```

## Modelos de Dados

### Produto

| Campo | Tipo | Obrigatório | Restrições |
|---|---|---|---|
| `produtoId` | `int` | — | PK, gerado automaticamente |
| `descricao` | `string` | Sim | Máximo 300 caracteres (busca case-insensitive) |
| `saldoAtual` | `int` | Sim | Mínimo 0 (mantido pelo `EstoqueService`) |
| `estoqueMinimo` | `int` | Sim | Mínimo 0 |

### Colaborador

| Campo | Tipo | Obrigatório | Restrições |
|---|---|---|---|
| `colaboradorId` | `int` | — | PK, gerado automaticamente |
| `nome` | `string` | Sim | Máximo 80 caracteres (busca case-insensitive) |

### Movimentacao

| Campo | Tipo | Obrigatório | Restrições |
|---|---|---|---|
| `movimentacaoId` | `int` | — | PK, gerado automaticamente |
| `tipo` | `char` | Sim | `'E'` (entrada), `'S'` (saída) ou `'A'` (ajuste) |
| `quantidade` | `int` | Sim | Mínimo 1 |
| `dataMovimentacao` | `DateTime?` | Não | — |
| `produtoId` | `int` | Sim | FK → Produto |
| `colaboradorId` | `int?` | Não | FK → Colaborador (só saída tem colaborador) |
| `registradoPor` | `string` | — | Username do JWT de quem registrou |

## Banco, Deploy & Infraestrutura

- **TiDB Cloud (MySQL-compatível) em produção.** O `ServerVersion` é fixado em `MySqlServerVersion(8,0,11)` (não `AutoDetect`).
- **Collation case-insensitive.** O TiDB cria colunas de texto com `utf8mb4_bin`; por isso `Colaborador.Nome` e `Produto.Descricao` são forçadas a `utf8mb4_general_ci`. A busca de usuários compara contra as colunas `Normalized*` do Identity.
- **Migrations no startup.** Aplicadas automaticamente antes do seed — o banco de produção é provisionado vazio.
- **Cloud Run.** A porta vem da env `PORT`; o TLS é terminado na borda (a app honra `X-Forwarded-*` e desliga `UseHttpsRedirection` em produção).
- **CORS.** Política `FrontendCors` global; origens vêm de `Cors:AllowedOrigins` (default dev `http://localhost:5173`). Expõe o header `X-Pagination`.
- **Seed.** Roles `Admin`/`User` e usuários admin a partir de `Seed:AdminUsers` (User Secrets).

## Logging

`CustomLoggerProvider` grava em `Log.txt` na raiz da aplicação. **Atenção:** `IsEnabled` compara por igualdade (`==`), não `>=` — só entradas com o nível exato configurado (`Information`) são gravadas.

## Códigos de Resposta HTTP

| Código | Situação |
|---|---|
| `200 OK` | Operação bem-sucedida |
| `201 Created` | Recurso criado (POST) |
| `400 Bad Request` | Dados inválidos / saldo insuficiente |
| `401 Unauthorized` | Token ausente ou inválido |
| `403 Forbidden` | Autenticado, mas sem permissão para a ação (ou token CSRF ausente/inválido) |
| `404 Not Found` | Recurso não encontrado |
| `429 Too Many Requests` | Limite de tentativas de login/refresh excedido (rate limiting) |
| `500 Internal Server Error` | Erro não tratado (capturado pelo `ApiExceptionFilter`) |
