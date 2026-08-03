using ApiControlePerifericos.Context;
using ApiControlePerifericos.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiControlePerifericos.Tests.Extensions
{
    // A connection string chega por variável de ambiente no Cloud Run (issue #31). Quando
    // ela falta, o erro precisa aparecer no startup e citar a chave — não estourar depois,
    // na primeira query, com uma mensagem do driver MySQL que não menciona configuração.
    public class PersistenceExtensionsTests
    {
        private static IConfiguration ConfigurarConnectionString(string? valor) =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                [
                    new KeyValuePair<string, string?>(
                        $"ConnectionStrings:{PersistenceExtensions.NomeConnectionString}", valor)
                ])
                .Build();

        [Fact]
        public void AddPersistence_ConfiguracaoAusente_LancaCitandoONomeDaChave()
        {
            // Arrange — nenhuma seção ConnectionStrings, como num deploy sem a env definida
            var configuration = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();

            // Act
            var excecao = Assert.Throws<InvalidOperationException>(
                () => services.AddPersistence(configuration));

            // Assert — a mensagem precisa dizer qual chave configurar
            Assert.Contains(PersistenceExtensions.NomeConnectionString, excecao.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AddPersistence_ConnectionStringVaziaOuEmBranco_Lanca(string? valor)
        {
            // Arrange — env presente porém sem conteúdo útil
            var services = new ServiceCollection();

            // Act + Assert
            Assert.Throws<InvalidOperationException>(
                () => services.AddPersistence(ConfigurarConnectionString(valor)));
        }

        [Fact]
        public void AddPersistence_ConnectionStringPresente_RegistraOContexto()
        {
            // Arrange — string com formato válido; nenhum acesso ao banco acontece aqui
            var configuration = ConfigurarConnectionString(
                "Server=localhost;Database=perifericos_teste;User=root;Password=senha;");
            var services = new ServiceCollection();

            // Act
            services.AddPersistence(configuration);

            // Assert — o comportamento anterior continua valendo: o contexto é resolvível
            using var provider = services.BuildServiceProvider();
            using var escopo = provider.CreateScope();
            Assert.NotNull(escopo.ServiceProvider.GetRequiredService<AppDbContext>());
        }
    }
}
