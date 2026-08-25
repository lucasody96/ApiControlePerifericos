using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace ApiControlePerifericos.Tests.Services
{
    public class AssistenteServiceTests
    {
        private const string ManualFake = "# Manual\nConteudo do manual de teste.";

        // Ferramenta de mentira: o serviço não a executa, apenas repassa o catálogo para a
        // IA. O que estes testes cobrem é o repasse, não a consulta — essa é a
        // FerramentasAssistenteTests.
        private static readonly FerramentaAssistente FerramentaFake =
            new("consultar_produto", "Consulta um produto.", [], (_, _) => Task.FromResult("{}"));

        private readonly Mock<IAssistenteIA> _ia = new();
        private readonly Mock<IManualProvider> _manual = new();
        private readonly Mock<IFerramentasAssistente> _ferramentas = new();
        private readonly Mock<ILogger<AssistenteService>> _logger = new();
        private readonly AssistenteService _service;

        public AssistenteServiceTests()
        {
            _manual.Setup(m => m.ObterConteudo()).Returns(ManualFake);
            _ferramentas.Setup(f => f.Obter()).Returns([FerramentaFake]);

            _service = new AssistenteService(_ia.Object, _manual.Object, _ferramentas.Object, _logger.Object);
        }

        // Helpers de Arrange
        private void ConfigurarResposta(string resposta) =>
            _ia.Setup(i => i.ResponderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                                            It.IsAny<IReadOnlyList<FerramentaAssistente>>(),
                                            It.IsAny<CancellationToken>()))
               .ReturnsAsync(resposta);

        private void ConfigurarFalha() =>
            _ia.Setup(i => i.ResponderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                                            It.IsAny<IReadOnlyList<FerramentaAssistente>>(),
                                            It.IsAny<CancellationToken>()))
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
            // Nem monta o catalogo de ferramentas, que consulta o banco.
            _ferramentas.VerifyNoOtherCalls();
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
                It.IsAny<IReadOnlyList<FerramentaAssistente>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ResponderAsync_DeveEnviarOCatalogoDeFerramentas()
        {
            ConfigurarResposta("ok");

            await _service.ResponderAsync("quantos mouses tem em estoque?");

            _ia.Verify(i => i.ResponderAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<FerramentaAssistente>>(catalogo => catalogo.Single() == FerramentaFake),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Instrucoes_DevemSepararManualDeConsultaAoEstoque()
        {
            // O guardrail é regra de negócio: se as instruções deixarem de mandar consultar
            // as ferramentas para dado de estoque, o assistente volta a responder saldo pelo
            // manual — que é justamente o que a issue #48 veio corrigir.
            ConfigurarResposta("ok");

            await _service.ResponderAsync("qual o saldo do mouse?");

            _ia.Verify(i => i.ResponderAsync(
                It.Is<string>(instrucoes => instrucoes.Contains("manual")
                                         && instrucoes.Contains("ferramentas")),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<FerramentaAssistente>>(),
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
