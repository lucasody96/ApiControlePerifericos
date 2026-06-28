using ApiControlePerifericos.Caching;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using Microsoft.Extensions.Caching.Memory;
using System.Linq.Expressions;
using X.PagedList;

namespace ApiControlePerifericos.Repositories
{
    // Decorator de cache sobre IProdutoRepository: serve as leituras do IMemoryCache
    // e invalida o grupo "produtos" em qualquer escrita. Leitura rastreada
    // (GetByIdTrackedAsync, usada para alterar saldo) e checagens por predicate
    // (GetAsync/ExistsAsync) não são cacheadas.
    public class CachedProdutoRepository : IProdutoRepository
    {
        private static readonly TimeSpan Expiracao = TimeSpan.FromMinutes(5);

        private readonly IProdutoRepository _inner;
        private readonly IMemoryCache _cache;
        private readonly CacheTokens _tokens;

        public CachedProdutoRepository(IProdutoRepository inner, IMemoryCache cache, CacheTokens tokens)
        {
            _inner = inner;
            _cache = cache;
            _tokens = tokens;
        }

        public Task<IEnumerable<Produto>> GetAllAsync() =>
            ObterOuCachear("produtos:all", () => _inner.GetAllAsync());

        public Task<IPagedList<Produto>> GetProdutosAsync(ProdutosParameters parameters) =>
            ObterOuCachear(
                $"produtos:paged:{parameters.PageNumber}:{parameters.PageSize}:{parameters.Descricao}",
                () => _inner.GetProdutosAsync(parameters));

        public Task<IEnumerable<Produto>> GetAbaixoEstoqueMinimoAsync() =>
            ObterOuCachear("produtos:abaixo-minimo", () => _inner.GetAbaixoEstoqueMinimoAsync());

        // Não cacheado: precisa da entidade rastreada para o EstoqueService alterar o saldo.
        public Task<Produto?> GetByIdTrackedAsync(int produtoId) => _inner.GetByIdTrackedAsync(produtoId);

        // Não cacheado: predicate arbitrário (chave instável) / mera checagem de existência.
        public Task<Produto?> GetAsync(Expression<Func<Produto, bool>> predicate) => _inner.GetAsync(predicate);
        public Task<bool> ExistsAsync(Expression<Func<Produto, bool>> predicate) => _inner.ExistsAsync(predicate);

        public Produto Create(Produto entity) => ComInvalidacao(() => _inner.Create(entity));
        public Produto Update(Produto entity) => ComInvalidacao(() => _inner.Update(entity));
        public Produto Delete(Produto entity) => ComInvalidacao(() => _inner.Delete(entity));

        private async Task<T> ObterOuCachear<T>(string chave, Func<Task<T>> consulta)
        {
            if (_cache.TryGetValue(chave, out T? cacheado) && cacheado is not null)
                return cacheado;

            var resultado = await consulta();

            _cache.Set(chave, resultado, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(Expiracao)
                .AddExpirationToken(_tokens.ObterToken(CacheGrupos.Produtos)));

            return resultado;
        }

        private T ComInvalidacao<T>(Func<T> acao)
        {
            var resultado = acao();
            _tokens.Invalidar(CacheGrupos.Produtos);
            return resultado;
        }
    }
}
