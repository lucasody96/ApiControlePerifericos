using ApiControlePerifericos.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace ApiControlePerifericos.Tests.Services
{
    public class ManualProviderTests
    {
        private readonly Mock<IConfiguration> _configuration = new();

        private void ConfigurarCaminho(string? caminho) =>
            _configuration.Setup(c => c[ManualProvider.ChaveCaminho]).Returns(caminho);

        [Fact]
        public void ObterConteudo_DeveLerOArquivoConfigurado()
        {
            var caminho = Path.GetTempFileName();
            File.WriteAllText(caminho, "# Manual\nTexto de teste.");
            ConfigurarCaminho(caminho);

            try
            {
                var provider = new ManualProvider(_configuration.Object);

                Assert.Contains("Texto de teste.", provider.ObterConteudo());
            }
            finally
            {
                File.Delete(caminho);
            }
        }

        [Fact]
        public void Construtor_QuandoArquivoNaoExiste_DeveFalharCitandoAChaveDeConfiguracao()
        {
            ConfigurarCaminho(Path.Combine(Path.GetTempPath(), $"manual-inexistente-{Guid.NewGuid()}.md"));

            var excecao = Assert.Throws<InvalidOperationException>(
                () => new ManualProvider(_configuration.Object));

            // A mensagem precisa dizer ao operador exatamente o que configurar.
            Assert.Contains(ManualProvider.ChaveCaminho, excecao.Message);
        }

        [Fact]
        public void ObterConteudo_DeveServirDaMemoriaSemRelerODisco()
        {
            var caminho = Path.GetTempFileName();
            File.WriteAllText(caminho, "conteudo original");
            ConfigurarCaminho(caminho);

            var provider = new ManualProvider(_configuration.Object);
            File.Delete(caminho);

            // Se lesse o disco a cada chamada, isto estouraria FileNotFoundException.
            Assert.Equal("conteudo original", provider.ObterConteudo());
        }
    }
}
