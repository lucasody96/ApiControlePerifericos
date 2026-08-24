using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApiControlePerifericos.Tests.Services
{
    public class AssistenteServiceTests
    {
        private const string ManualFake = "# Manual\nConteudo do manual de teste.";

        private readonly Mock<IAssistenteIA> _ia = new();
        private readonly Mock<IManualProvider> _manual = new();
        private readonly Mock<ILogger<AssistenteService>> _logger = new();
        private readonly AssistenteService _service;

        public AssistenteServiceTests()
        {
            _manual.Setup(m => m.ObterConteudo()).Returns(ManualFake);

            _service = new AssistenteService(_ia.Object, _manual.Object, _logger.Object);
        }

        // Helpers de Arrange
        private void ConfigurarResposta(string resposta) =>
            _ia.Setup(i => i.ResponderAsync(It.IsAny<string>(), It.IsAny<string>(),
                                            It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(resposta);

        private void ConfigurarFalha() =>
            _ia.Setup(i => i.ResponderAsync(It.IsAny<string>(), It.IsAny<string>(),
                                            It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ThrowsAsync(new AssistenteIAException("Falha simulada."));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ResponderAsync_DeveRecusarPerguntaVazia(string? pergunta)
        {
            var resultado = await _service.ResponderAsync(pergunta);

            Assert.False(resultado.Sucesso);
            Assert.Equal(AssistenteResultStatus.PerguntaVazia, resultado.Status);
            // Nao gasta chamada paga na API externa por uma pergunta que nem existe.
            _ia.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ResponderAsync_DeveRecusarPerguntaAcimaDoLimite()
        {
            var pergunta = new string('a', AssistenteService.TamanhoMaximoPergunta + 1);

            var resultado = await _service.ResponderAsync(pergunta);

            Assert.Equal(AssistenteResultStatus.PerguntaMuitoLonga, resultado.Status);
            Assert.Contains(AssistenteService.TamanhoMaximoPergunta.ToString(), resultado.Mensagem!);
            _ia.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ResponderAsync_NoLimiteExato_DeveAceitar()
        {
            ConfigurarResposta("ok");
            var pergunta = new string('a', AssistenteService.TamanhoMaximoPergunta);

            var resultado = await _service.ResponderAsync(pergunta);

            Assert.True(resultado.Sucesso);
        }

        [Fact]
        public async Task ResponderAsync_DeveDevolverARespostaDaIA()
        {
            ConfigurarResposta("Va em Movimentacoes e clique em Nova saida.");

            var resultado = await _service.ResponderAsync("como registro uma saida?");

            Assert.True(resultado.Sucesso);
            Assert.Equal("Va em Movimentacoes e clique em Nova saida.", resultado.Resposta);
        }

        [Fact]
        public async Task ResponderAsync_DeveEnviarOManualComoContextoEAPerguntaSemEspacos()
        {
            ConfigurarResposta("ok");

            await _service.ResponderAsync("  como faco login?  ");

            _ia.Verify(i => i.ResponderAsync(
                It.Is<string>(instrucoes => instrucoes.Contains("manual")),
                ManualFake,
                "como faco login?",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ResponderAsync_QuandoIAFalha_DeveDevolverFalhaTratada()
        {
            ConfigurarFalha();

            var resultado = await _service.ResponderAsync("como registro uma saida?");

            Assert.Equal(AssistenteResultStatus.FalhaNaIA, resultado.Status);
            Assert.Null(resultado.Resposta);
            // A mensagem que chega ao cliente nao entrega o detalhe da integracao.
            Assert.DoesNotContain("Anthropic", resultado.Mensagem!);
        }
    }
}
