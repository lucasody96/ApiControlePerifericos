using ApiControlePerifericos.Caching;
using ApiControlePerifericos.Extensions;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiControlePerifericos.Tests.Extensions
{
    // O AddInfrastructure (issue #32) assumiu a composição que antes ficava na Program.cs.
    // O que estes testes seguram é justamente o que um refactor futuro quebra sem avisar:
    // as leituras de Produto e Colaborador passarem pelo decorator de cache e os tempos
    // de vida de cada registro — nenhum dos dois aparece como erro de compilação.
    public class InfrastructureExtensionsTests
    {
        // Formato válido, mas nenhuma conexão é aberta: o provider só resolve os serviços.
        private const string ConnectionStringFake =
            "Server=localhost;Database=perifericos_teste;User=root;Password=senha;";

        private static ServiceProvider ConstruirProvider()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                [
                    new KeyValuePair<string, string?>(
                        $"ConnectionStrings:{PersistenceExtensions.NomeConnectionString}",
                        ConnectionStringFake)
                ])
                .Build();

            var services = new ServiceCollection();
            // O TokenService recebe IConfiguration — na app quem registra é o host.
            services.AddSingleton<IConfiguration>(configuration);
            services.AddInfrastructure(configuration);

            // validateScopes: reprova scoped resolvido a partir do provider raiz.
            return services.BuildServiceProvider(validateScopes: true);
        }

        [Fact]
        public void AddInfrastructure_LeiturasDeProdutoEColaborador_PassamPelosDecoratorsDeCache()
        {
            // Arrange
            using var provider = ConstruirProvider();

            // Act
            using var escopo = provider.CreateScope();
            var produtoRepository = escopo.ServiceProvider.GetRequiredService<IProdutoRepository>();
            var colaboradorRepository = escopo.ServiceProvider.GetRequiredService<IColaboradorRepository>();

            // Assert — quem atende as interfaces é o decorator, não o repositório concreto
            Assert.IsType<CachedProdutoRepository>(produtoRepository);
            Assert.IsType<CachedColaboradorRepository>(colaboradorRepository);
        }

        [Fact]
        public void AddInfrastructure_CacheTokensEInvalidador_SaoSingletons()
        {
            // Arrange — o estado de invalidação é global; duas instâncias significariam
            // uma escrita expirando um conjunto de tokens que ninguém mais consulta
            using var provider = ConstruirProvider();

            // Act
            using var primeiroEscopo = provider.CreateScope();
            using var segundoEscopo = provider.CreateScope();

            // Assert
            Assert.Same(
                primeiroEscopo.ServiceProvider.GetRequiredService<CacheTokens>(),
                segundoEscopo.ServiceProvider.GetRequiredService<CacheTokens>());
            Assert.Same(
                primeiroEscopo.ServiceProvider.GetRequiredService<IProdutoCacheInvalidator>(),
                segundoEscopo.ServiceProvider.GetRequiredService<IProdutoCacheInvalidator>());
        }

        [Theory]
        [InlineData(typeof(IProdutoRepository))]
        [InlineData(typeof(IColaboradorRepository))]
        [InlineData(typeof(IMovimentacaoRepository))]
        [InlineData(typeof(IUnitOfWork))]
        [InlineData(typeof(ITokenService))]
        public void AddInfrastructure_RepositoriosUnitOfWorkETokenService_SaoScoped(Type servico)
        {
            // Arrange — scoped porque compartilham o AppDbContext da requisição
            using var provider = ConstruirProvider();

            // Act
            using var primeiroEscopo = provider.CreateScope();
            using var segundoEscopo = provider.CreateScope();

            var doPrimeiro = primeiroEscopo.ServiceProvider.GetRequiredService(servico);
            var doSegundo = segundoEscopo.ServiceProvider.GetRequiredService(servico);

            // Assert — a mesma instância dentro do escopo, outra em escopo diferente
            Assert.Same(doPrimeiro, primeiroEscopo.ServiceProvider.GetRequiredService(servico));
            Assert.NotSame(doPrimeiro, doSegundo);
        }

        [Fact]
        public void AddInfrastructure_UnitOfWork_ExpoeOsRepositoriosDecorados()
        {
            // Arrange — o UnitOfWork é o caminho que os controllers usam; se ele recebesse
            // os concretos, as leituras deixariam de passar pelo cache sem nada quebrar
            using var provider = ConstruirProvider();

            // Act
            using var escopo = provider.CreateScope();
            var unitOfWork = escopo.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Assert
            Assert.IsType<CachedProdutoRepository>(unitOfWork.ProdutoRepository);
            Assert.IsType<CachedColaboradorRepository>(unitOfWork.ColaboradorRepository);
        }
    }
}
