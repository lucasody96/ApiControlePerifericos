# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Rodar a API (abre em http://localhost:5045, UI em /scalar/v1)
dotnet run --project ApiControlePerifericos

# Build
dotnet build

# Migrations (EF Core)
dotnet ef migrations add <NomeDaMigration> --project ApiControlePerifericos
dotnet ef database update --project ApiControlePerifericos
```

**String de conexão:** configurada via User Secrets (não em appsettings). Para configurar:
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=...;User=...;Password=..."  --project ApiControlePerifericos
```

Não há testes automatizados neste projeto.

## Arquitetura

API REST em ASP.NET Core (.NET 10) com MySQL (Pomelo EF Core). Três domínios: **Produto** (hardware/periférico), **Colaborador** (pessoa que retira o item) e **Movimentacao** (registro de entrada/saída de produto por colaborador).

### Padrões centrais

**Repository + Unit of Work:** Toda persistência passa pela interface `IUnitOfWork`, que expõe os três repositórios especializados e o método `CommitAsync()`. Nunca chame `SaveChangesAsync()` diretamente no `DbContext` — sempre use `_uow.CommitAsync()`.

**Generic Repository:** `Repository<T>` implementa operações CRUD básicas via `DbContext.Set<T>()`. Repositórios especializados (`ProdutoRepository`, `ColaboradorRepository`, `MovimentacaoRepository`) herdam dele e adicionam queries com paginação (`X.PagedList`).

**AutoMapper:** Mapeamento bidirecional entre Models e DTOs definido em `DTOs/Mappings/MappingProfile.cs`. Toda conversão model↔DTO deve passar pelo mapper injetado nos controllers.

### Fluxo de uma requisição

```
Controller → IUnitOfWork → IRepositorioEspecializado → Repository<T> → AppDbContext → MySQL
```

Controllers injetam `IUnitOfWork`, `ILogger<T>` e `IMapper`. Usam AutoMapper para converter entidade→DTO antes de retornar ao cliente.

### Paginação

Todos os endpoints `GET /pagination` aceitam `pageNumber` e `pageSize` (máx. 50) via query string. A resposta inclui header `X-Pagination` com metadados (total, páginas, has-next, has-previous). A classe base `QueryStringParameters` centraliza as regras de clamp.

### Convenções dos controllers

- `POST` retorna `CreatedAtRoute` com o nome de rota `"ObterProduto"` / `"ObterColaborador"` / `"ObterMovimentacao"`.
- `PUT` valida que o `id` da rota bate com o id do DTO antes de processar.
- Retorna `404 NotFound` quando a entidade não existe.
- `ApiExceptionFilter` (registrado globalmente) captura exceções não tratadas e retorna 500.

### Logging

`CustomLoggerProvider` + `CustomerLogger` gravam em `Log.txt` na raiz da aplicação. Já registrado em `Program.cs` com `LogLevel.Information`.
