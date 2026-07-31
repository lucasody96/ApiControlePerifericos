using ApiControlePerifericos.Caching;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using ApiControlePerifericos.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System.Linq.Expressions;
using X.PagedList.Extensions;

namespace ApiControlePerifericos.Tests.Repositories
{
    // Espelha CachedProdutoRepositoryTests: o decorator de Colaborador tem o mesmo
    // desenho — leituras servidas do IMemoryCache e invalidação do grupo na escrita.
    public class CachedColaboradorRepositoryTests
    {
        private readonly Mock<IColaboradorRepository> _inner = new();
        private readonly MemoryCache _cache = new(new MemoryCacheOptions());
        private readonly CacheTokens _tokens = new();
        private readonly CachedColaboradorRepository _sut;

        public CachedColaboradorRepositoryTests()
        {
            _inner.Setup(r => r.GetAllAsync())
                  .ReturnsAsync(new List<Colaborador> { new() { ColaboradorId = 1, Nome = "Ana" } });

            _inner.Setup(r => r.GetColaboradoresAsync(It.IsAny<ColaboradoresParameters>()))
                  .ReturnsAsync(new List<Colaborador> { new() { ColaboradorId = 1, Nome = "Ana" } }
                      .ToPagedList(1, 10));

            _inner.Setup(r => r.Create(It.IsAny<Colaborador>())).Returns<Colaborador>(c => c);
            _inner.Setup(r => r.Update(It.IsAny<Colaborador>())).Returns<Colaborador>(c => c);
            _inner.Setup(r => r.Delete(It.IsAny<Colaborador>())).Returns<Colaborador>(c => c);

            _sut = new CachedColaboradorRepository(_inner.Object, _cache, _tokens);
        }

        // ─── Leituras cacheadas ──────────────────────────────────────────────────

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
        public async Task GetColaboradoresAsync_DeveServirDoCache_NaSegundaChamadaComMesmosParametros()
        {
            // Arrange
            var parameters = new ColaboradoresParameters { PageNumber = 1, PageSize = 10 };

            // Act
            await _sut.GetColaboradoresAsync(parameters);
            await _sut.GetColaboradoresAsync(parameters);

            // Assert
            _inner.Verify(r => r.GetColaboradoresAsync(It.IsAny<ColaboradoresParameters>()), Times.Once);
        }

        [Fact]
        public async Task GetColaboradoresAsync_ComParametrosDiferentes_DeveConsultarRepositorioNovamente()
        {
            // Arrange: a chave do cache inclui página, tamanho e filtro — variações
            // não podem se servir da entrada uma da outra.
            var primeiraPagina = new ColaboradoresParameters { PageNumber = 1, PageSize = 10 };
            var segundaPagina = new ColaboradoresParameters { PageNumber = 2, PageSize = 10 };

            // Act
            await _sut.GetColaboradoresAsync(primeiraPagina);
            await _sut.GetColaboradoresAsync(segundaPagina);

            // Assert
            _inner.Verify(r => r.GetColaboradoresAsync(It.IsAny<ColaboradoresParameters>()), Times.Exactly(2));
        }

        // ─── Leituras NÃO cacheadas ──────────────────────────────────────────────

        [Fact]
        public async Task GetAsync_NaoDeveSerCacheado_SempreConsultaRepositorio()
        {
            // Arrange: predicate arbitrário não é cacheável.
            _inner.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Colaborador, bool>>>()))
                  .ReturnsAsync(new Colaborador { ColaboradorId = 1, Nome = "Ana" });

            // Act
            await _sut.GetAsync(c => c.ColaboradorId == 1);
            await _sut.GetAsync(c => c.ColaboradorId == 1);

            // Assert
            _inner.Verify(r => r.GetAsync(It.IsAny<Expression<Func<Colaborador, bool>>>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ExistsAsync_NaoDeveSerCacheado_SempreConsultaRepositorio()
        {
            // Arrange
            _inner.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Colaborador, bool>>>()))
                  .ReturnsAsync(true);

            // Act
            await _sut.ExistsAsync(c => c.ColaboradorId == 1);
            await _sut.ExistsAsync(c => c.ColaboradorId == 1);

            // Assert
            _inner.Verify(r => r.ExistsAsync(It.IsAny<Expression<Func<Colaborador, bool>>>()), Times.Exactly(2));
        }

        // ─── Invalidação nas escritas ────────────────────────────────────────────

        [Fact]
        public async Task Create_DeveInvalidarCache_ForcandoNovaConsulta()
        {
            // Arrange: popula o cache.
            await _sut.GetAllAsync();

            // Act: uma escrita invalida o grupo "colaboradores".
            _sut.Create(new Colaborador());
            await _sut.GetAllAsync();

            // Assert
            _inner.Verify(r => r.GetAllAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task Update_DeveInvalidarCache_ForcandoNovaConsulta()
        {
            // Arrange
            await _sut.GetAllAsync();

            // Act
            _sut.Update(new Colaborador { ColaboradorId = 1 });
            await _sut.GetAllAsync();

            // Assert
            _inner.Verify(r => r.GetAllAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task Delete_DeveInvalidarCache_ForcandoNovaConsulta()
        {
            // Arrange
            await _sut.GetAllAsync();

            // Act
            _sut.Delete(new Colaborador { ColaboradorId = 1 });
            await _sut.GetAllAsync();

            // Assert
            _inner.Verify(r => r.GetAllAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task Create_DeveInvalidarTambemAsLeiturasPaginadas()
        {
            // Arrange: o IMemoryCache não remove por prefixo — a invalidação por
            // CancellationChangeToken é o que derruba as variações de paginação.
            var parameters = new ColaboradoresParameters { PageNumber = 1, PageSize = 10 };
            await _sut.GetColaboradoresAsync(parameters);

            // Act
            _sut.Create(new Colaborador());
            await _sut.GetColaboradoresAsync(parameters);

            // Assert
            _inner.Verify(r => r.GetColaboradoresAsync(It.IsAny<ColaboradoresParameters>()), Times.Exactly(2));
        }

        [Fact]
        public async Task Create_NaoDeveInvalidarOGrupoDeProdutos()
        {
            // Arrange: cada recurso tem o seu próprio token de invalidação; a escrita
            // de colaborador não pode derrubar o cache de produtos.
            var produtoRepo = new Mock<IProdutoRepository>();
            produtoRepo.Setup(r => r.GetAllAsync())
                       .ReturnsAsync(new List<Produto> { new() { ProdutoId = 1, Descricao = "Mouse" } });

            var cachedProdutos = new CachedProdutoRepository(produtoRepo.Object, _cache, _tokens);
            await cachedProdutos.GetAllAsync();

            // Act
            _sut.Create(new Colaborador());
            await cachedProdutos.GetAllAsync();

            // Assert: o cache de produtos continuou válido.
            produtoRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }
    }
}
