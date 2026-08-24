using ApiControlePerifericos.Controllers;
using ApiControlePerifericos.DTOs.Assistente;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ApiControlePerifericos.Tests.Controllers
{
    public class AssistenteControllerTests
    {
        private readonly Mock<IAssistenteService> _assistenteService = new();
        private readonly AssistenteController _controller;

        public AssistenteControllerTests()
        {
            _controller = new AssistenteController(_assistenteService.Object);
        }

        private void ConfigurarResultado(AssistenteResult resultado) =>
            _assistenteService
                .Setup(s => s.ResponderAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(resultado);

        [Fact]
        public async Task Perguntar_DeveRetornarOkComARespostaDoServico()
        {
            ConfigurarResultado(AssistenteResult.Ok("Va em Movimentacoes."));

            var acao = await _controller.Perguntar(
                new PerguntaDTO { Pergunta = "como registro uma saida?" }, CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(acao.Result);
            var dto = Assert.IsType<RespostaDTO>(ok.Value);
            Assert.Equal("Va em Movimentacoes.", dto.Resposta);
        }

        [Theory]
        [InlineData(AssistenteResultStatus.PerguntaVazia)]
        [InlineData(AssistenteResultStatus.PerguntaMuitoLonga)]
        public async Task Perguntar_QuandoPerguntaInvalida_DeveRetornarBadRequest(AssistenteResultStatus status)
        {
            ConfigurarResultado(AssistenteResult.Falha(status, "mensagem de erro"));

            var acao = await _controller.Perguntar(new PerguntaDTO(), CancellationToken.None);

            var badRequest = Assert.IsType<BadRequestObjectResult>(acao.Result);
            Assert.Equal("mensagem de erro", badRequest.Value);
        }

        [Fact]
        public async Task Perguntar_QuandoIAFalha_DeveRetornar503()
        {
            ConfigurarResultado(AssistenteResult.Falha(AssistenteResultStatus.FalhaNaIA, "indisponivel"));

            var acao = await _controller.Perguntar(
                new PerguntaDTO { Pergunta = "qualquer" }, CancellationToken.None);

            // Falha da API externa e indisponibilidade temporaria, nao erro do cliente.
            var resultado = Assert.IsType<ObjectResult>(acao.Result);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, resultado.StatusCode);
        }
    }
}
