using ApiControlePerifericos.Caching;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Repositories;
using ApiControlePerifericos.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiControlePerifericos.Extensions
{
    /// <summary>
    /// Registro da infraestrutura: acesso a dados, cache e repositórios. Como a
    /// composição dos decorators de cache sobre os repositórios concretos é detalhe
    /// desta camada, a apresentação declara só que usa a infraestrutura — não como
    /// cada peça é construída.
    /// </summary>
    public static class InfrastructureExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Provider e connection string ficam em PersistenceExtensions.
            services.AddPersistence(configuration);

            // Cache em memória (issue #117): decorators sobre os repositórios de Produto e
            // Colaborador servem leituras do IMemoryCache e invalidam por grupo na escrita.
            // CacheTokens e o invalidador são singletons (o estado de invalidação é global).
            services.AddMemoryCache();
            services.AddSingleton<CacheTokens>();
            services.AddSingleton<IProdutoCacheInvalidator, ProdutoCacheInvalidator>();

            // Repositório concreto + decorator de cache. O UnitOfWork recebe as interfaces
            // (já decoradas) via DI, então toda leitura passa pelo cache.
            services.AddScoped<ProdutoRepository>();
            services.AddScoped<IProdutoRepository>(sp => new CachedProdutoRepository(
                sp.GetRequiredService<ProdutoRepository>(),
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<CacheTokens>()));

            services.AddScoped<ColaboradorRepository>();
            services.AddScoped<IColaboradorRepository>(sp => new CachedColaboradorRepository(
                sp.GetRequiredService<ColaboradorRepository>(),
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<CacheTokens>()));

            services.AddScoped<IMovimentacaoRepository, MovimentacaoRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<ITokenService, TokenService>();

            return services;
        }
    }
}
