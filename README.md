# ApiControlePerifericos

API REST para controle de periféricos e hardwares em coworkings. Permite gerenciar o estoque de equipamentos e rastrear quais colaboradores retiraram cada item.

## Stack Tecnológico

| Tecnologia | Versão | Finalidade |
|---|---|---|
| .NET / ASP.NET Core | 10.0 | Framework principal |
| Entity Framework Core | 9.0.14 | ORM |
| Pomelo.EntityFrameworkCore.MySql | 9.0.0 | Driver MySQL |
| AutoMapper | 16.1.1 | Mapeamento Model ↔ DTO |
| X.PagedList | 10.5.9 | Paginação server-side |
| Scalar.AspNetCore | 2.13.22 | UI de documentação da API |
| Newtonsoft.Json | 13.0.4 | Serialização JSON |

## Domínios

O sistema possui três entidades principais:

- **Produto** — hardware ou periférico disponível para retirada (notebook, mouse, teclado, etc.)
- **Colaborador** — pessoa cadastrada que pode retirar equipamentos
- **Movimentacao** — registro de entrada (`E`) ou saída (`S`) de um produto por um colaborador

## Estrutura de Pastas

```
ApiControlePerifericos/
├── Context/
│   └── AppDbContext.cs            # DbContext do EF Core
├── Controllers/
│   ├── ColaboradoresController.cs
│   ├── MovimentacoesController.cs
│   └── ProdutosController.cs
├── DTOs/
│   ├── ColaboradorDTO.cs
│   ├── MovimentacaoDTO.cs
│   ├── ProdutoDTO.cs
│   └── Mappings/
│       └── MappingProfile.cs      # Configuração do AutoMapper
├── Filters/
│   ├── ApiExceptionFilter.cs      # Captura exceções → HTTP 500
│   └── ApiLoggingFilter.cs        # Log de entrada/saída das actions
├── Interfaces/
│   ├── IColaboradorRepository.cs
│   ├── IMovimentacaoRepository.cs
│   ├── IProdutoRepository.cs
│   ├── IRepository.cs             # Contrato genérico CRUD
│   └── IUnitOfWork.cs
├── Logging/
│   ├── CustomLoggerProvider.cs
│   ├── CustomLoggerProviderConfiguration.cs
│   └── CustomerLogger.cs          # Grava em Log.txt
├── Migrations/                    # Migrations do EF Core
├── Models/
│   ├── Colaborador.cs
│   ├── Movimentacao.cs
│   └── Produto.cs
├── Pagination/
│   ├── ColaboradoresParameters.cs
│   ├── MovimentacoesParameters.cs
│   ├── ProdutosParameters.cs
│   └── QueryStringParameters.cs   # Base com clamp de pageSize (máx. 50)
├── Repositories/
│   ├── ColaboradorRepository.cs
│   ├── MovimentacaoRepository.cs
│   ├── ProdutoRepository.cs
│   ├── Repository.cs              # Repositório genérico base
│   └── UnitOfWork.cs
├── Program.cs
├── appsettings.json
└── ApiControlePerifericos.csproj
```

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MySQL 8+ (ou MariaDB compatível)
- [EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

## Como Executar

### 1. Configurar a string de conexão via User Secrets

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost;Database=ControlePerifericos;User=root;Password=suasenha;" \
  --project ApiControlePerifericos
```

### 2. Aplicar as migrations

```bash
dotnet ef database update --project ApiControlePerifericos
```

### 3. Rodar a API

```bash
dotnet run --project ApiControlePerifericos
```

A API sobe em:
- **HTTP:** `http://localhost:5045`
- **HTTPS:** `https://localhost:7081`
- **Documentação interativa (Scalar):** `http://localhost:5045/scalar/v1`

### Build

```bash
dotnet build
```

### Adicionar nova migration

```bash
dotnet ef migrations add <NomeDaMigration> --project ApiControlePerifericos
```

## Endpoints

### Produtos — `/api/produtos`

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/produtos` | Lista todos os produtos |
| `GET` | `/api/produtos/{id}` | Retorna produto por ID |
| `GET` | `/api/produtos/pagination` | Lista produtos paginados |
| `POST` | `/api/produtos` | Cadastra novo produto |
| `PUT` | `/api/produtos/{id}` | Atualiza produto existente |
| `DELETE` | `/api/produtos/{id}` | Remove produto |

### Colaboradores — `/api/colaboradores`

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/colaboradores` | Lista todos os colaboradores |
| `GET` | `/api/colaboradores/{id}` | Retorna colaborador por ID |
| `GET` | `/api/colaboradores/pagination` | Lista colaboradores paginados |
| `POST` | `/api/colaboradores` | Cadastra novo colaborador |
| `PUT` | `/api/colaboradores/{id}` | Atualiza colaborador existente |
| `DELETE` | `/api/colaboradores/{id}` | Remove colaborador |

### Movimentações — `/api/movimentacoes`

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/movimentacoes` | Lista todas as movimentações |
| `GET` | `/api/movimentacoes/{id}` | Retorna movimentação por ID |
| `GET` | `/api/movimentacoes/pagination` | Lista movimentações paginadas (ordem: data DESC) |
| `POST` | `/api/movimentacoes` | Registra nova movimentação |
| `PUT` | `/api/movimentacoes/{id}` | Atualiza movimentação existente |
| `DELETE` | `/api/movimentacoes/{id}` | Remove movimentação |

### Parâmetros de paginação (query string)

Todos os endpoints `/pagination` aceitam:

| Parâmetro | Padrão | Máximo | Descrição |
|---|---|---|---|
| `pageNumber` | `1` | — | Número da página |
| `pageSize` | `50` | `50` | Itens por página |

A resposta inclui o header `X-Pagination` com metadados:

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

**Resposta:** `201 Created` com header `Location` apontando para o recurso criado.

### Cadastrar um colaborador

```http
POST /api/colaboradores
Content-Type: application/json

{
  "nome": "Ana Paula Silva"
}
```

### Registrar saída de produto

```http
POST /api/movimentacoes
Content-Type: application/json

{
  "tipo": "S",
  "quantidade": 1,
  "dataMovimentacao": "2026-05-13T09:00:00",
  "produtoId": 1,
  "colaboradorId": 2
}
```

> `tipo`: `"E"` para entrada (devolução), `"S"` para saída (retirada).

### Listar produtos com paginação

```http
GET /api/produtos/pagination?pageNumber=1&pageSize=10
```

### Buscar movimentações (segunda página)

```http
GET /api/movimentacoes/pagination?pageNumber=2&pageSize=20
```

## Modelos de Dados

### Produto

| Campo | Tipo | Obrigatório | Restrições |
|---|---|---|---|
| `produtoId` | `int` | — | PK, gerado automaticamente |
| `descricao` | `string` | Sim | Máximo 300 caracteres |
| `saldoAtual` | `int` | Sim | Mínimo 0 |
| `estoqueMinimo` | `int` | Sim | Mínimo 0 |

### Colaborador

| Campo | Tipo | Obrigatório | Restrições |
|---|---|---|---|
| `colaboradorId` | `int` | — | PK, gerado automaticamente |
| `nome` | `string` | Sim | Máximo 80 caracteres |

### Movimentacao

| Campo | Tipo | Obrigatório | Restrições |
|---|---|---|---|
| `movimentacaoId` | `int` | — | PK, gerado automaticamente |
| `tipo` | `char` | Sim | `'E'` (entrada) ou `'S'` (saída) |
| `quantidade` | `int` | Sim | Mínimo 1 |
| `dataMovimentacao` | `DateTime?` | Não | — |
| `produtoId` | `int` | Sim | FK → Produto |
| `colaboradorId` | `int` | Sim | FK → Colaborador |

## Arquitetura

O projeto segue o padrão **Repository + Unit of Work** com separação clara entre camadas:

```
Controller → IUnitOfWork → IRepositorioEspecializado → Repository<T> → AppDbContext → MySQL
```

- **Controllers** injetam `IUnitOfWork`, `IMapper` e `ILogger<T>`. Nunca acessam o `DbContext` diretamente.
- **Repository<T>** implementa CRUD genérico com `AsNoTracking()` em todas as leituras.
- **UnitOfWork** centraliza o `CommitAsync()` — único ponto de `SaveChangesAsync()`.
- **AutoMapper** converte entidades em DTOs antes de retornar ao cliente.
- **ApiExceptionFilter** captura qualquer exceção não tratada e retorna `500` com mensagem amigável.
- **CustomLoggerProvider** grava logs em `Log.txt` na raiz da aplicação com nível `Information`.

## Códigos de Resposta HTTP

| Código | Situação |
|---|---|
| `200 OK` | Operação bem-sucedida |
| `201 Created` | Recurso criado (POST) |
| `400 Bad Request` | Dados inválidos |
| `404 Not Found` | Recurso não encontrado |
| `500 Internal Server Error` | Erro não tratado (capturado pelo `ApiExceptionFilter`) |
