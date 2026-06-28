using ApiControlePerifericos.Caching;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using Microsoft.Extensions.Caching.Memory;
using System.Linq.Expressions;
using X.PagedList;

namespace ApiControlePerifericos.Repositories
{
    // Decorator de cache sobre IColaboradorRepository: serve as leituras do
    // IMemoryCache e invalida o grupo "colaboradores" em qualquer escrita.
    public class CachedColaboradorRepository : IColaboradorRepository
    {
        private static readonly TimeSpan Expiracao = TimeSpan.FromMinutes(5);

        private readonly IColaboradorRepository _inner;
        private readonly IMemoryCache _cache;
        private readonly CacheTokens _tokens;

        public CachedColaboradorRepository(IColaboradorRepository inner, IMemoryCache cache, CacheTokens tokens)
        {
            _inner = inner;
            _cache = cache;
            _tokens = tokens;
        }

        public Task<IEnumerable<Colaborador>> GetAllAsync() =>
            ObterOuCachear("colaboradores:all", () => _inner.GetAllAsync());

        public Task<IPagedList<Colaborador>> GetColaboradoresAsync(ColaboradoresParameters parameters) =>
            ObterOuCachear(
                $"colaboradores:paged:{parameters.PageNumber}:{parameters.PageSize}:{parameters.Nome}",
                () => _inner.GetColaboradoresAsync(parameters));

        // Não cacheado: predicate arbitrário / mera checagem de existência.
        public Task<Colaborador?> GetAsync(Expression<Func<Colaborador, bool>> predicate) => _inner.GetAsync(predicate);
        public Task<bool> ExistsAsync(Expression<Func<Colaborador, bool>> predicate) => _inner.ExistsAsync(predicate);

        public Colaborador Create(Colaborador entity) => ComInvalidacao(() => _inner.Create(entity));
        public Colaborador Update(Colaborador entity) => ComInvalidacao(() => _inner.Update(entity));
        public Colaborador Delete(Colaborador entity) => ComInvalidacao(() => _inner.Delete(entity));

        private async Task<T> ObterOuCachear<T>(string chave, Func<Task<T>> consulta)
        {
            if (_cache.TryGetValue(chave, out T? cacheado) && cacheado is not null)
                return cacheado;

            var resultado = await consulta();

            _cache.Set(chave, resultado, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(Expiracao)
                .AddExpirationToken(_tokens.ObterToken(CacheGrupos.Colaboradores)));

            return resultado;
        }

        private T ComInvalidacao<T>(Func<T> acao)
        {
            var resultado = acao();
            _tokens.Invalidar(CacheGrupos.Colaboradores);
            return resultado;
        }
    }
}
