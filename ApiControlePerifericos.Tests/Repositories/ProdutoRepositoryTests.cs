using ApiControlePerifericos.Context;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using ApiControlePerifericos.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ApiControlePerifericos.Tests.Repositories
{
    public class ProdutoRepositoryTests
    {
        // Cada teste usa um banco InMemory isolado (nome via Guid). Recebe o nome para que
        // contextos diferentes do mesmo teste compartilhem o banco (escreve num, lê em outro).
        private static AppDbContext CriarContexto(string nomeBanco) =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(nomeBanco)
                .Options);

        // Semeia 'quantidade' produtos com descrições "Produto 1..N" e saldo crescente.
        private static async Task SemearProdutosAsync(string nomeBanco, int quantidade)
        {
            using var contexto = CriarContexto(nomeBanco);
            for (var i = 1; i <= quantidade; i++)
                contexto.Produtos.Add(new Produto { Descricao = $"Produto {i}", SaldoAtual = i });
            await contexto.SaveChangesAsync();
        }

        [Fact]
        public async Task GetProdutosAsync_PrimeiraPagina_DeveRetornarMetadadosCorretos()
        {
            // Arrange — 5 produtos, página 1 com 2 itens por página
            var dbName = Guid.NewGuid().ToString();
            await SemearProdutosAsync(dbName, 5);

            // Act
            using var contexto = CriarContexto(dbName);
            var repo = new ProdutoRepository(contexto);
            var pagina = await repo.GetProdutosAsync(new ProdutosParameters { PageNumber = 1, PageSize = 2 });

            // Assert
            Assert.Equal(2, pagina.Count);              // itens nesta página
            Assert.Equal(5, pagina.TotalItemCount);     // total geral
            Assert.Equal(3, pagina.PageCount);          // 5 itens / 2 por página = 3 páginas
            Assert.False(pagina.HasPreviousPage);
            Assert.True(pagina.HasNextPage);
        }

        [Fact]
        public async Task GetProdutosAsync_UltimaPagina_DeveTerHasPreviousSemHasNext()
        {
            // Arrange — 5 produtos, última página (3) com 2 itens por página
            var dbName = Guid.NewGuid().ToString();
            await SemearProdutosAsync(dbName, 5);

            // Act
            using var contexto = CriarContexto(dbName);
            var repo = new ProdutoRepository(contexto);
            var pagina = await repo.GetProdutosAsync(new ProdutosParameters { PageNumber = 3, PageSize = 2 });

            // Assert
            Assert.Single(pagina);                      // sobra 1 item na última página
            Assert.True(pagina.HasPreviousPage);
            Assert.False(pagina.HasNextPage);
        }

        [Fact]
        public async Task GetProdutosAsync_ComFiltroDescricao_DeveRetornarSomenteCorrespondentes()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                contexto.Produtos.AddRange(
                    new Produto { Descricao = "Mouse Logitech", SaldoAtual = 10 },
                    new Produto { Descricao = "Teclado Mecânico", SaldoAtual = 5 },
                    new Produto { Descricao = "Mousepad", SaldoAtual = 8 });
                await contexto.SaveChangesAsync();
            }

            // Act
            using var contextoLeitura = CriarContexto(dbName);
            var repo = new ProdutoRepository(contextoLeitura);
            var pagina = await repo.GetProdutosAsync(new ProdutosParameters { Descricao = "Mouse" });

            // Assert — "Mouse Logitech" e "Mousepad" casam o Contains("Mouse")
            Assert.Equal(2, pagina.Count);
            Assert.All(pagina, p => Assert.Contains("Mouse", p.Descricao!));
        }

        [Fact]
        public async Task GetByIdTrackedAsync_QuandoExiste_DeveRetornarProdutoRastreado()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            int produtoId;
            using (var contexto = CriarContexto(dbName))
            {
                var produto = new Produto { Descricao = "Monitor", SaldoAtual = 3 };
                contexto.Produtos.Add(produto);
                await contexto.SaveChangesAsync();
                produtoId = produto.ProdutoId;
            }

            // Act
            using var contextoLeitura = CriarContexto(dbName);
            var repo = new ProdutoRepository(contextoLeitura);
            var encontrado = await repo.GetByIdTrackedAsync(produtoId);

            // Assert — ao contrário das leituras genéricas (AsNoTracking), este vem RASTREADO,
            // para permitir o update do SaldoAtual na mesma transação.
            Assert.NotNull(encontrado);
            Assert.Equal(EntityState.Unchanged, contextoLeitura.Entry(encontrado!).State);
        }

        [Fact]
        public async Task GetByIdTrackedAsync_QuandoNaoExiste_DeveRetornarNull()
        {
            // Arrange — banco vazio
            var dbName = Guid.NewGuid().ToString();
            using var contexto = CriarContexto(dbName);
            var repo = new ProdutoRepository(contexto);

            // Act
            var encontrado = await repo.GetByIdTrackedAsync(9999);

            // Assert
            Assert.Null(encontrado);
        }

        [Fact]
        public async Task GetAbaixoEstoqueMinimoAsync_DeveRetornarSomenteProdutosNoLimiteOuAbaixo()
        {
            // Arrange — o repositório usa SaldoAtual <= EstoqueMinimo (inclui o igual).
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                contexto.Produtos.AddRange(
                    new Produto { Descricao = "Abaixo", SaldoAtual = 1, EstoqueMinimo = 5 },   // entra (<)
                    new Produto { Descricao = "NoLimite", SaldoAtual = 5, EstoqueMinimo = 5 },  // entra (=)
                    new Produto { Descricao = "Acima", SaldoAtual = 10, EstoqueMinimo = 5 });   // fora (>)
                await contexto.SaveChangesAsync();
            }

            // Act
            using var contextoLeitura = CriarContexto(dbName);
            var repo = new ProdutoRepository(contextoLeitura);
            var abaixo = (await repo.GetAbaixoEstoqueMinimoAsync()).ToList();

            // Assert
            Assert.Equal(2, abaixo.Count);
            Assert.DoesNotContain(abaixo, p => p.Descricao == "Acima");
        }
    }
}
