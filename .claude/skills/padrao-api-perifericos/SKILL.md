---
name: padrao-api-perifericos
description: Valores concretos do padrão de API deste repositório — nomes, limites, bibliotecas e caminhos reais que a skill genérica `padrao-api-dotnet` deixa em aberto. Use junto com ela ao escrever ou revisar código do ApiControlePerifericos.
---

# Valores concretos — ApiControlePerifericos

Esta skill **não repete padrão**. O padrão, o motivo de cada peça e as armadilhas estão em
`padrao-api-dotnet`. Aqui ficam só os valores que aquela skill declara como "decisão do projeto",
para não haver duas versões da mesma explicação envelhecendo em paralelo.

Se algo abaixo divergir do código, **o código vence** — e atualize esta tabela na mesma tarefa.

## Solução e camadas

| Item | Valor |
| --- | --- |
| Arquivo de solução | `ApiControlePerifericos.slnx` (formato novo) |
| Projeto de apresentação / startup | `ApiControlePerifericos` (mantém o nome por causa do Dockerfile) |
| Demais projetos | `.Application`, `.Infrastructure`, `.Domain` |
| Projeto de testes | `ApiControlePerifericos.Tests` |
| Namespaces | todos sob `ApiControlePerifericos.*` — **não** refletem a camada, e isso é intencional |

Decisões de fronteira que fogem do óbvio: `ApplicationUser` e `ITokenService` ficam na
**Infrastructure** (acoplados a Identity/JWT), e `Pagination/` fica no **Domain** (as interfaces de
repositório dependem dela).

## Nomes que a skill genérica deixa em aberto

| Conceito | Nome neste repo |
| --- | --- |
| Campo de Unit of Work — produção | `_uof` |
| Campo de Unit of Work — testes | `_uow` (diverge da produção; siga o lado em que estiver escrevendo) |
| Leitura rastreada | `IProdutoRepository.GetByIdTrackedAsync(int)` e `IMovimentacaoRepository.GetByIdTrackedAsync(int)` |
| Filtro de exceção global | `ApiExceptionFilter` — `Filters/ApiExceptionFilter.cs` |
| Objeto de resultado | `EstoqueResult` + `EstoqueResultStatus` — `Application/Services/EstoqueResult.cs` |
| Tradutor status → HTTP | `MovimentacoesController.MapearFalha` (só falhas; o sucesso é 201 em `ProcessarResultado` e 200 em `ProcessarEscritaDeHistorico`) |
| Rotas nomeadas do `POST` | `ObterProduto`, `ObterColaborador`, `ObterMovimentacao` |

## Paginação

- Biblioteca: **`X.PagedList`**.
- Base: `QueryStringParameters` — `Domain/Pagination/`.
- `MaxPageSize = 50`, e o **default é o próprio máximo**: `/pagination` sem `pageSize` traz 50.
- Header de metadados: `X-Pagination`, serializado com **`JsonConvert`** (Newtonsoft).
- Montagem e escrita do header: extensão `Response.AdicionarHeaderDePaginacao(pagina)` em
  `Extensions/PaginacaoResponseExtensions.cs` (WebApi). É o ponto único — não recrie o objeto
  anônimo de metadata no controller.
- Exposto ao frontend por `WithExposedHeaders("X-Pagination")` na política `FrontendCors`.
- Parâmetros concretos com filtro próprio: `MovimentacoesParameters` (`DataInicio`, `DataFim`,
  `DescricaoProduto`, `NomeColaborador`, `ProdutoId`, `ColaboradorId`, `Tipo`) e
  `UsuariosParameters` (`Busca`). Os filtros se combinam por interseção; os por id e o `Tipo`
  usam comparação exata, os de texto usam `Contains`.

## Autorização

Policies em `Program.cs`: `AdminOnly` e `SuperAdminOnly` — só essas duas. A `UserOnly` foi removida
na issue #22 (declarada, nunca aplicada); a role `"User"` em si continua existindo.

`SuperAdminOnly` = role `Admin` **mais** claim `id` numa allowlist que vem da config `SuperAdmins`
(User Secrets/appsettings), com fallback `["lucas.ody", "admin"]` quando a seção está ausente ou
vazia. A resolução tem um ponto único — `Auth/SuperAdminAllowlist` (`Resolver` / `Contem`) —
consumido pela policy e por `AuthController.EhSuperAdmin`; ao ler a allowlist em algum lugar novo,
chame esse tipo em vez de reler a seção. Para promover alguém, edite a config — não há lista
hardcoded.

Escalonamento em vigor nos controllers de domínio:

| Controller | GET | POST/PUT | DELETE |
| --- | --- | --- | --- |
| `Produtos`, `Colaboradores` | `[Authorize]` | `AdminOnly` | `SuperAdminOnly` |
| `Movimentacoes` | `AdminOnly` | `AdminOnly` | `SuperAdminOnly` (PUT também) |
| `Usuarios` | controller inteiro `AdminOnly` | — | `PUT /{userName}/roles` é `SuperAdminOnly` |

## Cache — decorator

Leituras de **Produto** e **Colaborador** passam por `CachedProdutoRepository` /
`CachedColaboradorRepository` (`Infrastructure/Repositories/`), injetados no lugar da interface. TTL
de 5 min; invalidação em massa por `CancellationChangeToken` em `CacheGrupos` (`Infrastructure/
Caching/`), com `CacheTokens` **singleton**.

**Não** são cacheados: `GetByIdTrackedAsync`, `GetAsync` e `ExistsAsync`.

Como a movimentação altera `SaldoAtual` sem passar pelo `Update`, o `EstoqueService` chama
`IProdutoCacheInvalidator.InvalidarProdutos()` **depois do commit** — a abstração fica na Application
para que ela não conheça `IMemoryCache`.

Ao tornar outro recurso cacheável, repita o desenho: decorator + grupo em `CacheGrupos` +
invalidação em toda escrita.

## Banco e migrations

- MySQL local, **TiDB Cloud** em produção (compatível com MySQL).
- `ServerVersion` **fixado** em `MySqlServerVersion(8,0,11)` — nada de `AutoDetect`, que abriria
  conexão no startup e tropeçaria na string de versão própria do TiDB.
- Collation `utf8mb4_general_ci` forçada em `Colaborador.Nome` e `Produto.Descricao`: o TiDB cria
  texto como `utf8mb4_bin` (case-sensitive) e a busca ficaria diferente do MySQL local. No Identity o
  contorno é outro — comparar contra as colunas `Normalized*`.
- Migrations aplicadas **no startup** via `db.Database.MigrateAsync()`.

```bash
dotnet ef migrations add <Nome> --project ApiControlePerifericos.Infrastructure --startup-project ApiControlePerifericos
```

## Autenticação — o que difere do JWT comum

O token **não** trafega no corpo nem no `localStorage`: vai em cookie httpOnly (`accessToken`,
`refreshToken`) mais `XSRF-TOKEN` legível pelo JS, com proteção anti-CSRF double-submit em
`Auth/CsrfValidationMiddleware.cs`. Nomes centralizados em `Auth/AuthCookies.cs`.

Consequências ao mexer em endpoint de escrita:

- Todo método que não seja `GET`/`HEAD`/`OPTIONS`/`TRACE` exige o header `X-CSRF-Token`. Único
  isento: `POST /api/Auth/login`.
- CORS **precisa** de `AllowCredentials()` e de origens explícitas — `AllowAnyOrigin()` é
  incompatível. Origem nova do frontend entra em `Cors:AllowedOrigins`, senão o login "funciona" mas
  o cookie não gruda.

## Logging

`CustomerLogger.IsEnabled` compara por **igualdade** (`logLevel == _config.LogLevel`), não `>=`. Só o
nível exato configurado em `Program.cs` é gravado em `Log.txt` — mudar o nível troca qual severidade
aparece, não estabelece um piso.
