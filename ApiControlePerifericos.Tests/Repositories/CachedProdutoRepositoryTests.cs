using ApiControlePerifericos.Caching;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace ApiControlePerifericos.Tests.Repositories
{
    // Testa o decorator de cache: leituras servidas do IMemoryCache e invalidação
    // por grupo nas escritas (e via ProdutoCacheInvalidator, como na movimentação).
    public class CachedProdutoRepositoryTests
    {
        private readonly Mock<IProdutoRepository> _inner = new();
        private readonly MemoryCache _cache = new(new MemoryCacheOptions());
        private readonly CacheTokens _tokens = new();
        private readonly CachedProdutoRepository _sut;

        public CachedProdutoRepositoryTests()
        {
            _inner.Setup(r => r.GetAllAsync())
                  .ReturnsAsync(new List<Produto> { new() { ProdutoId = 1, Descricao = "Mouse" } });
            _inner.Setup(r => r.Create(It.IsAny<Produto>())).Returns<Produto>(p => p);

            _sut = new CachedProdutoRepository(_inner.Object, _cache, _tokens);
        }

        [Fact]
        public async Task GetAllAsync_DeveServirDoCache_NaSegundaChamada()
        {
            // Act
            await _sut.GetAllAsync();
            await _sut.GetAllAsync();

            // Assert: a segunda leitura veio do cache; o repositório real foi chamado uma vez.
            _inner.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task Create_DeveInvalidarCache_ForcandoNovaConsulta()
        {
            // Arrange: popula o cache.
            await _sut.GetAllAsync();

            // Act: uma escrita invalida o grupo "produtos".
            _sut.Create(new Produto());
            await _sut.GetAllAsync();

            // Assert: a leitura após a escrita reconsultou o repositório real.
            _inner.Verify(r => r.GetAllAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task InvalidarProdutos_ViaInvalidator_DeveExpirarCache()
        {
            // Arrange: popula o cache e cria o invalidador que o EstoqueService usa,
            // compartilhando o MESMO CacheTokens (singleton em produção).
            await _sut.GetAllAsync();
            var invalidator = new ProdutoCacheInvalidator(_tokens);

            // Act: simula o que a movimentação de estoque faz após alterar o saldo.
            invalidator.InvalidarProdutos();
            await _sut.GetAllAsync();

            // Assert: o cache expirou e a leitura reconsultou o repositório real.
            _inner.Verify(r => r.GetAllAsync(), Times.Exactly(2));
        }
    }
}
