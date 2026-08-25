using ApiControlePerifericos.Controllers;
using ApiControlePerifericos.DTOs.Assistente;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace ApiControlePerifericos.Tests.Controllers
{
    public class AssistenteControllerTests
    {
        private readonly Mock<IAssistenteService> _assistenteService = new();
        private readonly AssistenteController _controller;

        public AssistenteControllerTests()
        {
            _controller = new AssistenteController(_assistenteService.Object);
            Autenticar(ehAdmin: false);
        }

        // O controller le User.IsInRole para decidir quais ferramentas o assistente recebe;
        // sem um HttpContext com usuario autenticado, o acesso a User estoura.
        private void Autenticar(bool ehAdmin)
        {
            var claims = new List<Claim> { new(ClaimTypes.Name, "lucas.ody") };
            if (ehAdmin)
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };
        }

        private void ConfigurarResultado(AssistenteResult resultado) =>
            _assistenteService
                .Setup(s => s.ResponderAsync(It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Perguntar_DeveInformarAoServicoSeQuemPerguntouEAdmin(bool ehAdmin)
        {
            Autenticar(ehAdmin);
            ConfigurarResultado(AssistenteResult.Ok("resposta"));

            await _controller.Perguntar(new PerguntaDTO { Pergunta = "o que saiu em julho?" },
                                        CancellationToken.None);

            // A role vem do JWT e so o controller a conhece. Se ela nao descer, o admin
            // perde as ferramentas de movimentacao em silencio -- e, no sentido contrario,
            // um nao-admin ganharia acesso a dado que o MovimentacoesController protege.
            _assistenteService.Verify(
                s => s.ResponderAsync(It.IsAny<string?>(), ehAdmin, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Perguntar_SemARoleDeAdmin_NaoDevePedirAsFerramentasDeAdmin()
        {
            // Usuario comum autenticado: o endpoint continua aberto a ele (o caso de uso e
            // duvida de manual), mas sem as ferramentas de movimentacao.
            ConfigurarResultado(AssistenteResult.Ok("resposta"));

            await _controller.Perguntar(new PerguntaDTO { Pergunta = "como faco login?" },
                                        CancellationToken.None);

            _assistenteService.Verify(
                s => s.ResponderAsync(It.IsAny<string?>(), true, It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
