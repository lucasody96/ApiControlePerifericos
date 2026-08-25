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

        [Fact]
        public async Task GetMovimentacoesAsync_ComFiltroDeData_DeveRetornarSomenteDoIntervalo()
        {
            // Arrange — 10/01, 15/01 e 18/01 às 23h (para provar que DataFim é inclusiva do dia)
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                contexto.Movimentacoes.AddRange(
                    new Movimentacao { Tipo = 'E', Quantidade = 1, ProdutoId = 1, DataMovimentacao = new DateTime(2026, 1, 10) },
                    new Movimentacao { Tipo = 'E', Quantidade = 1, ProdutoId = 1, DataMovimentacao = new DateTime(2026, 1, 15) },
                    new Movimentacao { Tipo = 'E', Quantidade = 1, ProdutoId = 1, DataMovimentacao = new DateTime(2026, 1, 18, 23, 0, 0) });
                await contexto.SaveChangesAsync();
            }

            // Act — intervalo 12/01 a 18/01
            using var contexto2 = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto2);
            var pagina = await repo.GetMovimentacoesAsync(new MovimentacoesParameters
            {
                DataInicio = new DateTime(2026, 1, 12),
                DataFim = new DateTime(2026, 1, 18)
            });

            // Assert — 15/01 e 18/01 23h entram; 10/01 fica de fora
            Assert.Equal(2, pagina.TotalItemCount);
            Assert.DoesNotContain(pagina, m => m.DataMovimentacao == new DateTime(2026, 1, 10));
        }

        [Fact]
        public async Task GetMovimentacoesAsync_ComFiltroDescricaoProduto_DeveFiltrar()
        {
            // Arrange — produtos reais porque o filtro navega para Produto.Descricao
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                var mouse = new Produto { Descricao = "Mouse", SaldoAtual = 10 };
                var teclado = new Produto { Descricao = "Teclado", SaldoAtual = 10 };
                contexto.AddRange(mouse, teclado);
                await contexto.SaveChangesAsync();

                contexto.Movimentacoes.AddRange(
                    new Movimentacao { Tipo = 'E', Quantidade = 1, ProdutoId = mouse.ProdutoId, DataMovimentacao = BaseData.AddDays(1) },
                    new Movimentacao { Tipo = 'E', Quantidade = 5, ProdutoId = teclado.ProdutoId, DataMovimentacao = BaseData.AddDays(2) });
                await contexto.SaveChangesAsync();
            }

            // Act
            using var contexto2 = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto2);
            var pagina = await repo.GetMovimentacoesAsync(new MovimentacoesParameters { DescricaoProduto = "Mouse" });

            // Assert
            Assert.Equal(1, pagina.TotalItemCount);
            Assert.Equal(1, pagina.First().Quantidade);
        }

        [Fact]
        public async Task GetMovimentacoesAsync_ComFiltroNomeColaborador_DeveFiltrar()
        {
            // Arrange — uma saída da Ana, uma do Bruno e uma entrada sem colaborador
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                var ana = new Colaborador { Nome = "Ana" };
                var bruno = new Colaborador { Nome = "Bruno" };
                contexto.AddRange(ana, bruno);
                await contexto.SaveChangesAsync();

                contexto.Movimentacoes.AddRange(
                    new Movimentacao { Tipo = 'S', Quantidade = 1, ProdutoId = 1, ColaboradorId = ana.ColaboradorId, DataMovimentacao = BaseData.AddDays(1) },
                    new Movimentacao { Tipo = 'S', Quantidade = 1, ProdutoId = 1, ColaboradorId = bruno.ColaboradorId, DataMovimentacao = BaseData.AddDays(2) },
                    new Movimentacao { Tipo = 'E', Quantidade = 9, ProdutoId = 1, DataMovimentacao = BaseData.AddDays(3) });
                await contexto.SaveChangesAsync();
            }

            // Act
            using var contexto2 = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto2);
            var pagina = await repo.GetMovimentacoesAsync(new MovimentacoesParameters { NomeColaborador = "Ana" });

            // Assert — só a saída da Ana; a entrada sem colaborador não passa no filtro
            Assert.Equal(1, pagina.TotalItemCount);
            Assert.Equal('S', pagina.First().Tipo);
        }

        [Fact]
        public async Task GetMovimentacoesAsync_ComFiltrosVazios_DeveRetornarTudo()
        {
            // Arrange — filtro em branco não pode restringir nada
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                contexto.Movimentacoes.AddRange(
                    new Movimentacao { Tipo = 'E', Quantidade = 1, ProdutoId = 1, DataMovimentacao = BaseData.AddDays(1) },
                    new Movimentacao { Tipo = 'E', Quantidade = 2, ProdutoId = 2, DataMovimentacao = BaseData.AddDays(2) });
                await contexto.SaveChangesAsync();
            }

            // Act
            using var contexto2 = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto2);
            var pagina = await repo.GetMovimentacoesAsync(new MovimentacoesParameters
            {
                DescricaoProduto = "",
                NomeColaborador = "   "
            });

            // Assert
            Assert.Equal(2, pagina.TotalItemCount);
        }

        [Fact]
        public async Task GetRelatorioAsync_ComFiltroColaboradorId_DeveFiltrarPorIdExato()
        {
            // Arrange — dois colaboradores de nomes parecidos: o filtro por texto pegaria
            // os dois ("Ana" está contido em "Ana Paula"), o filtro por id só um.
            var dbName = Guid.NewGuid().ToString();
            int anaId;
            using (var contexto = CriarContexto(dbName))
            {
                var produto = new Produto { Descricao = "Mouse", SaldoAtual = 10 };
                var ana = new Colaborador { Nome = "Ana" };
                var anaPaula = new Colaborador { Nome = "Ana Paula" };
                contexto.AddRange(produto, ana, anaPaula);
                await contexto.SaveChangesAsync();
                anaId = ana.ColaboradorId;

                contexto.Movimentacoes.AddRange(
                    new Movimentacao { Tipo = 'S', Quantidade = 1, ProdutoId = produto.ProdutoId, ColaboradorId = ana.ColaboradorId, DataMovimentacao = BaseData.AddDays(1) },
                    new Movimentacao { Tipo = 'S', Quantidade = 2, ProdutoId = produto.ProdutoId, ColaboradorId = anaPaula.ColaboradorId, DataMovimentacao = BaseData.AddDays(2) });
                await contexto.SaveChangesAsync();
            }

            // Act
            using var contexto2 = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto2);
            var pagina = await repo.GetRelatorioAsync(new MovimentacoesParameters { ColaboradorId = anaId });

            // Assert — só a movimentação da Ana
            Assert.Single(pagina);
            Assert.Equal("Ana", pagina.First().Colaborador!.Nome);
        }

        [Fact]
        public async Task GetRelatorioAsync_ComColaboradorIdEPeriodo_DeveCombinarOsFiltros()
        {
            // Arrange — três saídas do mesmo colaborador, uma delas dentro do período
            var dbName = Guid.NewGuid().ToString();
            int colaboradorId;
            using (var contexto = CriarContexto(dbName))
            {
                var produto = new Produto { Descricao = "Mouse", SaldoAtual = 10 };
                var joao = new Colaborador { Nome = "João" };
                contexto.AddRange(produto, joao);
                await contexto.SaveChangesAsync();
                colaboradorId = joao.ColaboradorId;

                contexto.Movimentacoes.AddRange(
                    new Movimentacao { Tipo = 'S', Quantidade = 1, ProdutoId = produto.ProdutoId, ColaboradorId = joao.ColaboradorId, DataMovimentacao = new DateTime(2026, 6, 30) },
                    new Movimentacao { Tipo = 'S', Quantidade = 1, ProdutoId = produto.ProdutoId, ColaboradorId = joao.ColaboradorId, DataMovimentacao = new DateTime(2026, 7, 15) },
                    new Movimentacao { Tipo = 'S', Quantidade = 1, ProdutoId = produto.ProdutoId, ColaboradorId = joao.ColaboradorId, DataMovimentacao = new DateTime(2026, 8, 1) });
                await contexto.SaveChangesAsync();
            }

            // Act — "o que o João pegou em julho"
            using var contexto2 = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto2);
            var pagina = await repo.GetRelatorioAsync(new MovimentacoesParameters
            {
                ColaboradorId = colaboradorId,
                DataInicio = new DateTime(2026, 7, 1),
                DataFim = new DateTime(2026, 7, 31)
            });

            // Assert — o período recorta o histórico do colaborador
            Assert.Single(pagina);
            Assert.Equal(new DateTime(2026, 7, 15), pagina.First().DataMovimentacao);
        }

        [Fact]
        public async Task GetMovimentacoesAsync_ComFiltroProdutoId_DeveFiltrar()
        {
            // Arrange — movimentações de dois produtos distintos
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                contexto.Movimentacoes.AddRange(
                    new Movimentacao { Tipo = 'E', Quantidade = 1, ProdutoId = 1, DataMovimentacao = BaseData.AddDays(1) },
                    new Movimentacao { Tipo = 'E', Quantidade = 2, ProdutoId = 1, DataMovimentacao = BaseData.AddDays(2) },
                    new Movimentacao { Tipo = 'E', Quantidade = 9, ProdutoId = 2, DataMovimentacao = BaseData.AddDays(3) });
                await contexto.SaveChangesAsync();
            }

            // Act
            using var contexto2 = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto2);
            var pagina = await repo.GetMovimentacoesAsync(new MovimentacoesParameters { ProdutoId = 1 });

            // Assert
            Assert.Equal(2, pagina.TotalItemCount);
            Assert.All(pagina, m => Assert.Equal(1, m.ProdutoId));
        }

        [Fact]
        public async Task GetMovimentacoesAsync_ComFiltroTipo_DeveRetornarSomenteDoTipo()
        {
            // Arrange — um de cada tipo, para o filtro ter o que descartar dos dois lados
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                contexto.Movimentacoes.AddRange(
                    new Movimentacao { Tipo = 'E', Quantidade = 10, ProdutoId = 1, DataMovimentacao = BaseData.AddDays(1) },
                    new Movimentacao { Tipo = 'S', Quantidade = 2, ProdutoId = 1, DataMovimentacao = BaseData.AddDays(2) },
                    new Movimentacao { Tipo = 'S', Quantidade = 3, ProdutoId = 1, DataMovimentacao = BaseData.AddDays(3) },
                    new Movimentacao { Tipo = 'A', Quantidade = 1, ProdutoId = 1, DataMovimentacao = BaseData.AddDays(4) });
                await contexto.SaveChangesAsync();
            }

            // Act — "quais saídas aconteceram"
            using var contexto2 = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto2);
            var pagina = await repo.GetMovimentacoesAsync(new MovimentacoesParameters { Tipo = 'S' });

            // Assert — é o TotalItemCount que responde "quantas saídas", sem contar linha
            Assert.Equal(2, pagina.TotalItemCount);
            Assert.All(pagina, m => Assert.Equal('S', m.Tipo));
        }

        [Fact]
        public async Task GetMovimentacoesAsync_ComTipoEmMinusculo_DeveCasarComOGravado()
        {
            // Arrange — o tipo é gravado sempre em maiúscula
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                contexto.Movimentacoes.AddRange(
                    new Movimentacao { Tipo = 'S', Quantidade = 2, ProdutoId = 1, DataMovimentacao = BaseData.AddDays(1) },
                    new Movimentacao { Tipo = 'E', Quantidade = 5, ProdutoId = 1, DataMovimentacao = BaseData.AddDays(2) });
                await contexto.SaveChangesAsync();
            }

            // Act — o assistente pode mandar 's' minúsculo; quem normaliza é o parameters
            using var contexto2 = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto2);
            var pagina = await repo.GetMovimentacoesAsync(new MovimentacoesParameters { Tipo = 's' });

            // Assert — sem a normalização isto voltaria vazio, em silêncio
            Assert.Equal(1, pagina.TotalItemCount);
            Assert.Equal('S', pagina.First().Tipo);
        }

        [Fact]
        public async Task GetRelatorioAsync_ComTipoProdutoEPeriodo_DeveCombinarOsFiltros()
        {
            // Arrange — só uma movimentação satisfaz os três filtros ao mesmo tempo
            var dbName = Guid.NewGuid().ToString();
            int mouseId;
            using (var contexto = CriarContexto(dbName))
            {
                var mouse = new Produto { Descricao = "Mouse", SaldoAtual = 10 };
                var teclado = new Produto { Descricao = "Teclado", SaldoAtual = 10 };
                var joao = new Colaborador { Nome = "João" };
                contexto.AddRange(mouse, teclado, joao);
                await contexto.SaveChangesAsync();
                mouseId = mouse.ProdutoId;

                contexto.Movimentacoes.AddRange(
                    // a que passa: saída, do mouse, dentro de julho
                    new Movimentacao { Tipo = 'S', Quantidade = 1, ProdutoId = mouse.ProdutoId, ColaboradorId = joao.ColaboradorId, DataMovimentacao = new DateTime(2026, 7, 15) },
                    // mesma data e produto, mas entrada
                    new Movimentacao { Tipo = 'E', Quantidade = 4, ProdutoId = mouse.ProdutoId, DataMovimentacao = new DateTime(2026, 7, 16) },
                    // saída no período, mas de outro produto
                    new Movimentacao { Tipo = 'S', Quantidade = 1, ProdutoId = teclado.ProdutoId, ColaboradorId = joao.ColaboradorId, DataMovimentacao = new DateTime(2026, 7, 17) },
                    // saída do mouse, mas fora do período
                    new Movimentacao { Tipo = 'S', Quantidade = 1, ProdutoId = mouse.ProdutoId, ColaboradorId = joao.ColaboradorId, DataMovimentacao = new DateTime(2026, 8, 1) });
                await contexto.SaveChangesAsync();
            }

            // Act
            using var contexto2 = CriarContexto(dbName);
            var repo = new MovimentacaoRepository(contexto2);
            var pagina = await repo.GetRelatorioAsync(new MovimentacoesParameters
            {
                Tipo = 'S',
                ProdutoId = mouseId,
                DataInicio = new DateTime(2026, 7, 1),
                DataFim = new DateTime(2026, 7, 31)
            });

            // Assert — interseção, e não união: um filtro a mais só restringe
            Assert.Single(pagina);
            Assert.Equal(new DateTime(2026, 7, 15), pagina.First().DataMovimentacao);
        }
    }
}
