using ApiControlePerifericos.Caching;
using ApiControlePerifericos.Extensions;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Repositories;
using ApiControlePerifericos.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        // Chave do assistente (issue #40): nenhuma chamada é feita, mas com ela presente o
        // AddInfrastructure registra o cliente real em vez do assistente desligado.
        private const string ApiKeyFake = "sk-ant-chave-de-teste";

        private static ServiceProvider ConstruirProvider()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                [
                    new KeyValuePair<string, string?>(
                        $"ConnectionStrings:{PersistenceExtensions.NomeConnectionString}",
                        ConnectionStringFake),
                    new KeyValuePair<string, string?>(
                        InfrastructureExtensions.ChaveApiKeyAnthropic, ApiKeyFake)
                ])
                .Build();

            var services = new ServiceCollection();
            // O TokenService recebe IConfiguration e o AnthropicAssistenteIA recebe ILogger —
            // na app quem registra os dois é o host.
            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging();
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

        [Fact]
        public void AddInfrastructure_ComChaveDaAnthropic_DeveRegistrarOClienteReal()
        {
            using var provider = ConstruirProvider();

            using var escopo = provider.CreateScope();

            Assert.IsType<AnthropicAssistenteIA>(escopo.ServiceProvider.GetRequiredService<IAssistenteIA>());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task AddInfrastructure_SemChaveDaAnthropic_DeveDesligarSoOAssistente(string? apiKey)
        {
            // Arrange — a chave serve a um endpoint só; derrubar o startup por causa dela
            // tiraria do ar produtos, movimentações e login junto.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                [
                    new KeyValuePair<string, string?>(
                        $"ConnectionStrings:{PersistenceExtensions.NomeConnectionString}",
                        ConnectionStringFake),
                    new KeyValuePair<string, string?>(
                        InfrastructureExtensions.ChaveApiKeyAnthropic, apiKey)
                ])
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddInfrastructure(configuration);

            using var provider = services.BuildServiceProvider(validateScopes: true);
            using var escopo = provider.CreateScope();

            // Assert — o resto da infraestrutura continua de pé...
            Assert.NotNull(escopo.ServiceProvider.GetRequiredService<IUnitOfWork>());

            // ...e a pergunta vira falha tratada, com a mensagem citando a chave a configurar.
            var assistente = escopo.ServiceProvider.GetRequiredService<IAssistenteIA>();
            var excecao = await Assert.ThrowsAsync<AssistenteIAException>(
                () => assistente.ResponderAsync("instrucoes", "manual", "pergunta", []));

            Assert.Contains(InfrastructureExtensions.ChaveApiKeyAnthropic, excecao.Message);
        }
    }
}
