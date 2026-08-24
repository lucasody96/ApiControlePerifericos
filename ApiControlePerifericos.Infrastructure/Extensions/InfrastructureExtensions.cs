using Anthropic;
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
        /// <summary>
        /// Chave da API da Anthropic esperada na configuracao. Publica porque os testes
        /// verificam que a mensagem de erro cita a chave que o operador precisa corrigir.
        /// </summary>
        public const string ChaveApiKeyAnthropic = "Anthropic:ApiKey";

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

            // Assistente de dúvidas (issue #40). A chave nunca vai para o appsettings: user-secrets
            // em desenvolvimento, variável de ambiente Anthropic__ApiKey no Cloud Run.
            var anthropicApiKey = configuration[ChaveApiKeyAnthropic];

            services.AddSingleton<IManualProvider, ManualProvider>();

            if (string.IsNullOrWhiteSpace(anthropicApiKey))
            {
                // Diferente da connection string, esta chave serve a um endpoint só: derrubar o
                // startup por causa dela tiraria do ar produtos, movimentações e login junto.
                // A API sobe e o assistente devolve 503 até a chave ser configurada.
                services.AddSingleton<IAssistenteIA>(new AssistenteIAIndisponivel(ChaveApiKeyAnthropic));
            }
            else
            {
                // O cliente é thread-safe e caro de construir (mantém o HttpClient): singleton.
                services.AddSingleton(new AnthropicClient { ApiKey = anthropicApiKey });
                services.AddScoped<IAssistenteIA, AnthropicAssistenteIA>();
            }

            return services;
        }
    }
}
