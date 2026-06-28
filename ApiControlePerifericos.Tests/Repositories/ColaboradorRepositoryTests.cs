using ApiControlePerifericos.Context;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using ApiControlePerifericos.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ApiControlePerifericos.Tests.Repositories
{
    public class ColaboradorRepositoryTests
    {
        // Cada teste usa um banco InMemory isolado (nome via Guid). Recebe o nome para que
        // contextos diferentes do mesmo teste compartilhem o banco (escreve num, lê em outro).
        private static AppDbContext CriarContexto(string nomeBanco) =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(nomeBanco)
                .Options);

        // Semeia 'quantidade' colaboradores com nomes "Colaborador 1..N".
        private static async Task SemearColaboradoresAsync(string nomeBanco, int quantidade)
        {
            using var contexto = CriarContexto(nomeBanco);
            for (var i = 1; i <= quantidade; i++)
                contexto.Colaboradores.Add(new Colaborador { Nome = $"Colaborador {i}" });
            await contexto.SaveChangesAsync();
        }

        [Fact]
        public async Task GetColaboradoresAsync_PrimeiraPagina_DeveRetornarMetadadosCorretos()
        {
            // Arrange — 5 colaboradores, página 1 com 2 itens por página
            var dbName = Guid.NewGuid().ToString();
            await SemearColaboradoresAsync(dbName, 5);

            // Act
            using var contexto = CriarContexto(dbName);
            var repo = new ColaboradorRepository(contexto);
            var pagina = await repo.GetColaboradoresAsync(new ColaboradoresParameters { PageNumber = 1, PageSize = 2 });

            // Assert
            Assert.Equal(2, pagina.Count);              // itens nesta página
            Assert.Equal(5, pagina.TotalItemCount);     // total geral
            Assert.Equal(3, pagina.PageCount);          // 5 itens / 2 por página = 3 páginas
            Assert.False(pagina.HasPreviousPage);
            Assert.True(pagina.HasNextPage);
        }

        [Fact]
        public async Task GetColaboradoresAsync_UltimaPagina_DeveTerHasPreviousSemHasNext()
        {
            // Arrange — 5 colaboradores, última página (3) com 2 itens por página
            var dbName = Guid.NewGuid().ToString();
            await SemearColaboradoresAsync(dbName, 5);

            // Act
            using var contexto = CriarContexto(dbName);
            var repo = new ColaboradorRepository(contexto);
            var pagina = await repo.GetColaboradoresAsync(new ColaboradoresParameters { PageNumber = 3, PageSize = 2 });

            // Assert
            Assert.Single(pagina);                      // sobra 1 item na última página
            Assert.True(pagina.HasPreviousPage);
            Assert.False(pagina.HasNextPage);
        }

        [Fact]
        public async Task GetColaboradoresAsync_ComFiltroNome_DeveRetornarSomenteCorrespondentes()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                contexto.Colaboradores.AddRange(
                    new Colaborador { Nome = "Ana Silva" },
                    new Colaborador { Nome = "Bruno Souza" },
                    new Colaborador { Nome = "Ana Paula" });
                await contexto.SaveChangesAsync();
            }

            // Act
            using var contextoLeitura = CriarContexto(dbName);
            var repo = new ColaboradorRepository(contextoLeitura);
            var pagina = await repo.GetColaboradoresAsync(new ColaboradoresParameters { Nome = "Ana" });

            // Assert — "Ana Silva" e "Ana Paula" casam o Contains("Ana")
            Assert.Equal(2, pagina.Count);
            Assert.All(pagina, c => Assert.Contains("Ana", c.Nome!));
        }
    }
}
