using ApiControlePerifericos.Context;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ApiControlePerifericos.Tests.Repositories
{
    public class RepositoryTests
    {
        // Cada teste usa um banco InMemory com nome próprio (Guid) para isolamento.
        // Recebe o nome para que contextos diferentes do MESMO teste compartilhem o banco
        // (simula requests separadas: escreve num contexto, lê em outro).

        private static AppDbContext CriarContexto(string nomeBanco) =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(nomeBanco)
                .Options);

        [Fact]
        public async Task GetAllAsync_DeveRetornarTodosOsRegistros()
        {
            //arrange
            var dbName = Guid.NewGuid().ToString();
            using(var contexto = CriarContexto(dbName))
            {
                contexto.Produtos.AddRange(
                    new Produto { Descricao = "Produto 1", SaldoAtual = 10 },
                    new Produto { Descricao = "Produto 2", SaldoAtual = 5 },
                    new Produto { Descricao = "Produto 3", SaldoAtual = 2 }
                );

                await contexto.SaveChangesAsync();
            }

            //Act
            using var contextoLeitura = CriarContexto(dbName);
            var repo = new Repository<Produto>(contextoLeitura);
            var todos = await repo.GetAllAsync();

            //Assert
            Assert.Equal(3, todos.Count());

        }

        [Fact]
        public async Task GetAsync_QuandoExiste_DeveRetornarEntidade()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                contexto.Produtos.Add(
                    new Produto { Descricao = "Produto 1", SaldoAtual = 10 }
                );

                await contexto.SaveChangesAsync();
            }
            // Act
            using var contextoLeitura = CriarContexto(dbName);
            var repo = new Repository<Produto>(contextoLeitura);
            var produto = await repo.GetAsync(p => p.Descricao == "Produto 1");

            // Assert
            Assert.NotNull(produto);
            Assert.Equal(10, produto!.SaldoAtual);
        }

        [Fact]
        public async Task GetAsync_QuandoNaoExiste_DeveRetornarNull()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using var contexto = CriarContexto(dbName);
            var repo = new Repository<Produto>(contexto);

            // Act
            var produto = await repo.GetAsync(p => p.Descricao == "Produto Inexistente");

            // Assert
            Assert.Null(produto);
        }

        [Fact]
        public async Task Create_CommitAsync_DevePersistirEntidade()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var produto = new Produto { Descricao = "Produto 4", SaldoAtual = 15 };

            // Act
            using (var contexto = CriarContexto(dbName))
            {
                var repo = new Repository<Produto>(contexto);
                repo.Create(produto);
                await contexto.SaveChangesAsync();
            }

            // Assert
            using (var contexto = CriarContexto(dbName))
            {
                var persistido = await contexto.Produtos.FindAsync(produto.ProdutoId);
                Assert.NotNull(persistido);
                Assert.Equal("Produto 4", persistido!.Descricao);
            }
        }

        [Fact]
        public async Task Update_CommitAsync_DeveAlterarEntidade()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                contexto.Produtos.Add(new Produto { Descricao = "Produto 5", SaldoAtual = 20 });
                await contexto.SaveChangesAsync();
            }

            // Act
            using (var contexto = CriarContexto(dbName))
            {
                var repo = new Repository<Produto>(contexto);
                var produto = await repo.GetAsync(p => p.Descricao == "Produto 5");
                produto!.SaldoAtual = 25;
                repo.Update(produto);
                await contexto.SaveChangesAsync();
            }

            // Assert
            using (var contexto = CriarContexto(dbName))
            {
                var atualizado = await contexto.Produtos.FirstAsync(p => p.Descricao == "Produto 5");
                Assert.Equal(25, atualizado!.SaldoAtual);
            }
        }

        [Fact]
        public async Task Delete_CommitAsync_DeveRemoverEntidade()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                contexto.Produtos.Add(new Produto { Descricao = "Produto 6", SaldoAtual = 30 });
                await contexto.SaveChangesAsync();
            }

            // Act
            using (var contexto = CriarContexto(dbName))
            {
                var repo = new Repository<Produto>(contexto);
                var produto = await repo.GetAsync(p => p.Descricao == "Produto 6");
                repo.Delete(produto!);
                await contexto.SaveChangesAsync();
            }

            // Assert
            using (var contexto = CriarContexto(dbName))
            {
                var repo = new Repository<Produto>(contexto);
                var todos = await repo.GetAllAsync();
                Assert.Empty(todos);
            }
        }

        [Fact]
        public async Task GetAsync_DeveUsarAsNoTracking_EntidadeNaoFicaRastreada()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            using (var contexto = CriarContexto(dbName))
            {
                contexto.Produtos.Add(new Produto { Descricao = "Produto 7", SaldoAtual = 40 });
                await contexto.SaveChangesAsync();
            }

            // Act
            using var contextoLeitura = CriarContexto(dbName);
            var repo = new Repository<Produto>(contextoLeitura);
            var produto = await repo.GetAsync(p => p.Descricao == "Produto 7");


            // Assert
            Assert.NotNull(produto);
            Assert.Empty(contextoLeitura.ChangeTracker.Entries());
        }
    }
}
