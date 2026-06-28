using ApiControlePerifericos.Context;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using ApiControlePerifericos.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ApiControlePerifericos.Tests.Repositories
{
    public class MovimentacaoRepositoryTests
    {
        // Cada teste usa um banco InMemory isolado (nome via Guid). Recebe o nome para que
        // contextos diferentes do mesmo teste compartilhem o banco (escreve num, lê em outro).
        private static AppDbContext CriarContexto(string nomeBanco) =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(nomeBanco)
                .Options);

        private static readonly DateTime BaseData = new(2026, 1, 1);

        [Fact]
        public async Task GetMovimentacoesAsync_DeveAplicarPaginacaoEOrdenarPorDataDesc()
        {
            // Arrange — 5 movimentações em datas crescentes (dia 1..5)
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                for (var i = 1; i <= 5; i++)
                    contexto.Movimentacoes.Add(new Movimentacao
                    {
                        Tipo = 'E',
                        Quantidade = i,
                        ProdutoId = 1,
                        DataMovimentacao = BaseData.AddDays(i)
                    });
                await contexto.SaveChangesAsync();
            }

            // Act — página 1 com 2 itens por página
            using var contexto2 = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto2);
            var pagina = await repo.GetMovimentacoesAsync(new MovimentacoesParameters { PageNumber = 1, PageSize = 2 });

            // Assert — metadados de paginação
            Assert.Equal(2, pagina.Count);
            Assert.Equal(5, pagina.TotalItemCount);
            Assert.Equal(3, pagina.PageCount);
            Assert.False(pagina.HasPreviousPage);
            Assert.True(pagina.HasNextPage);
            // Ordenação decrescente por data: o item mais recente (dia 5) vem primeiro.
            Assert.Equal(BaseData.AddDays(5), pagina.First().DataMovimentacao);
        }

        [Fact]
        public async Task GetByProdutoIdAsync_DeveRetornarSomenteDoProduto()
        {
            // Arrange — 2 movimentações do produto 1 e 1 do produto 2
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                contexto.Movimentacoes.AddRange(
                    new Movimentacao { Tipo = 'E', Quantidade = 5, ProdutoId = 1, DataMovimentacao = BaseData.AddDays(1) },
                    new Movimentacao { Tipo = 'A', Quantidade = 2, ProdutoId = 1, DataMovimentacao = BaseData.AddDays(2) },
                    new Movimentacao { Tipo = 'E', Quantidade = 9, ProdutoId = 2, DataMovimentacao = BaseData.AddDays(3) });
                await contexto.SaveChangesAsync();
            }

            // Act
            using var contexto2 = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto2);
            var movimentacoes = (await repo.GetByProdutoIdAsync(1)).ToList();

            // Assert
            Assert.Equal(2, movimentacoes.Count);
            Assert.All(movimentacoes, m => Assert.Equal(1, m.ProdutoId));
        }

        [Fact]
        public async Task GetByProdutoIdAsync_SemMovimentacoes_DeveRetornarVazio()
        {
            // Arrange — só há movimentação de outro produto
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                contexto.Movimentacoes.Add(
                    new Movimentacao { Tipo = 'E', Quantidade = 5, ProdutoId = 1, DataMovimentacao = BaseData });
                await contexto.SaveChangesAsync();
            }

            // Act
            using var contexto2 = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto2);
            var movimentacoes = await repo.GetByProdutoIdAsync(9999);

            // Assert
            Assert.Empty(movimentacoes);
        }

        [Fact]
        public async Task GetByColaboradorIdAsync_DeveRetornarSomenteDoColaborador()
        {
            // Arrange — 2 saídas do colaborador 7 e 1 do colaborador 8
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                contexto.Movimentacoes.AddRange(
                    new Movimentacao { Tipo = 'S', Quantidade = 1, ProdutoId = 1, ColaboradorId = 7, DataMovimentacao = BaseData.AddDays(1) },
                    new Movimentacao { Tipo = 'S', Quantidade = 1, ProdutoId = 1, ColaboradorId = 7, DataMovimentacao = BaseData.AddDays(2) },
                    new Movimentacao { Tipo = 'S', Quantidade = 1, ProdutoId = 2, ColaboradorId = 8, DataMovimentacao = BaseData.AddDays(3) });
                await contexto.SaveChangesAsync();
            }

            // Act
            using var contexto2 = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto2);
            var movimentacoes = (await repo.GetByColaboradorIdAsync(7)).ToList();

            // Assert
            Assert.Equal(2, movimentacoes.Count);
            Assert.All(movimentacoes, m => Assert.Equal(7, m.ColaboradorId));
        }

        [Fact]
        public async Task GetByColaboradorIdAsync_SemMovimentacoes_DeveRetornarVazio()
        {
            // Arrange — banco vazio
            var dbName = Guid.NewGuid().ToString();
            using var contexto = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto);

            // Act
            var movimentacoes = await repo.GetByColaboradorIdAsync(9999);

            // Assert
            Assert.Empty(movimentacoes);
        }

        [Fact]
        public async Task GetRelatorioAsync_ComFiltroDescricaoProduto_DeveFiltrarECarregarNavegacoes()
        {
            // Arrange — produtos e colaboradores reais para os Includes do relatório
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                var mouse = new Produto { Descricao = "Mouse", SaldoAtual = 10 };
                var teclado = new Produto { Descricao = "Teclado", SaldoAtual = 10 };
                var colaborador = new Colaborador { Nome = "Ana" };
                contexto.AddRange(mouse, teclado, colaborador);
                await contexto.SaveChangesAsync();

                contexto.Movimentacoes.AddRange(
                    new Movimentacao { Tipo = 'S', Quantidade = 1, ProdutoId = mouse.ProdutoId, ColaboradorId = colaborador.ColaboradorId, DataMovimentacao = BaseData.AddDays(1) },
                    new Movimentacao { Tipo = 'E', Quantidade = 5, ProdutoId = teclado.ProdutoId, DataMovimentacao = BaseData.AddDays(2) });
                await contexto.SaveChangesAsync();
            }

            // Act
            using var contexto2 = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto2);
            var pagina = await repo.GetRelatorioAsync(new MovimentacoesParameters { DescricaoProduto = "Mouse" });

            // Assert — só a movimentação do Mouse, com Produto e Colaborador carregados (Include)
            Assert.Single(pagina);
            var movimentacao = pagina.First();
            Assert.Equal("Mouse", movimentacao.Produto!.Descricao);
            Assert.Equal("Ana", movimentacao.Colaborador!.Nome);
        }

        [Fact]
        public async Task GetRelatorioAsync_ComFiltroDeData_DeveRetornarSomenteDoIntervalo()
        {
            // Arrange — movimentações em 10/01, 15/01 e 20/01.
            // O relatório faz Include(Produto); no provider InMemory uma movimentação com
            // ProdutoId órfão (sem o Produto correspondente) é descartada no Include, então
            // criamos um Produto real e usamos o id dele.
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                var produto = new Produto { Descricao = "Mouse", SaldoAtual = 10 };
                contexto.Produtos.Add(produto);
                await contexto.SaveChangesAsync();

                contexto.Movimentacoes.AddRange(
                    new Movimentacao { Tipo = 'E', Quantidade = 1, ProdutoId = produto.ProdutoId, DataMovimentacao = new DateTime(2026, 1, 10) },
                    new Movimentacao { Tipo = 'E', Quantidade = 1, ProdutoId = produto.ProdutoId, DataMovimentacao = new DateTime(2026, 1, 15) },
                    new Movimentacao { Tipo = 'E', Quantidade = 1, ProdutoId = produto.ProdutoId, DataMovimentacao = new DateTime(2026, 1, 20) });
                await contexto.SaveChangesAsync();
            }

            // Act — intervalo 12/01 a 18/01 (DataFim é inclusiva do dia inteiro)
            using var contexto2 = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto2);
            var pagina = await repo.GetRelatorioAsync(new MovimentacoesParameters
            {
                DataInicio = new DateTime(2026, 1, 12),
                DataFim = new DateTime(2026, 1, 18)
            });

            // Assert — só a movimentação de 15/01 cai no intervalo
            Assert.Single(pagina);
            Assert.Equal(new DateTime(2026, 1, 15), pagina.First().DataMovimentacao);
        }
    }
}
