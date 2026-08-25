using ApiControlePerifericos.Controllers;
using ApiControlePerifericos.DTOs;
using ApiControlePerifericos.DTOs.Estoque;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using ApiControlePerifericos.Services;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using System.Security.Claims;
using X.PagedList.Extensions;

namespace ApiControlePerifericos.Tests.Controllers
{
    public class MovimentacoesControllerTests
    {
        private readonly Mock<IMovimentacaoRepository> _movimentacaoRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<MovimentacoesController>> _logger = new();
        private readonly Mock<IEstoqueService> _estoqueService = new();
        private readonly MovimentacoesController _controller;

        public MovimentacoesControllerTests()
        {
            _uow.Setup(u => u.MovimentacaoRepository).Returns(_movimentacaoRepo.Object);

            _controller = new MovimentacoesController(
                _uow.Object, _logger.Object, _mapper.Object, _estoqueService.Object);

            // Os endpoints de POST leem User.Identity?.Name; sem um HttpContext com
            // usuario autenticado, o acesso a User estoura NullReferenceException.
            var usuario = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "lucas.ody") }, "TestAuth"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = usuario }
            };
        }

        // Helper: configura o GetAsync (o mesmo metodo que o controller usa para buscar por id)
        private void ConfigurarMovimentacao(Movimentacao? movimentacao) =>
            _movimentacaoRepo
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Movimentacao, bool>>>()))
                .ReturnsAsync(movimentacao);

        // Helpers: PUT e DELETE delegam ao EstoqueService, que devolve o EstoqueResult.
        private void ConfigurarAtualizacao(EstoqueResult resultado) =>
            _estoqueService
                .Setup(s => s.AtualizarMovimentacaoAsync(It.IsAny<int>(), It.IsAny<MovimentacaoDTO>()))
                .ReturnsAsync(resultado);

        private void ConfigurarExclusao(EstoqueResult resultado) =>
            _estoqueService
                .Setup(s => s.ExcluirMovimentacaoAsync(It.IsAny<int>()))
                .ReturnsAsync(resultado);

        // ---------------------------- GET lista ------------------------------

        /// <summary>
        /// Testa se o Get (lista) retorna 200 OK com os DTOs mapeados quando existem movimentacoes.
        /// </summary>
        [Fact]
        public async Task Get_QuandoExistemMovimentacoes_DeveRetornar200ComDTOs()
        {
            // Arrange
            var movimentacoes = new List<Movimentacao>
            {
                new() { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 },
                new() { MovimentacaoId = 2, Tipo = 'S', Quantidade = 2, ProdutoId = 1, ColaboradorId = 7 }
            };
            var movimentacoesDTO = new List<MovimentacaoDTO>
            {
                new() { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 },
                new() { MovimentacaoId = 2, Tipo = 'S', Quantidade = 2, ProdutoId = 1, ColaboradorId = 7 }
            };

            _movimentacaoRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(movimentacoes);
            _mapper.Setup(m => m.Map<IEnumerable<MovimentacaoDTO>>(movimentacoes)).Returns(movimentacoesDTO);

            // Act
            var result = await _controller.Get();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<MovimentacaoDTO>>(ok.Value);
            Assert.Equal(2, dtos.Count());
        }

        // ---------------------------- GET por id ------------------------------

        /// <summary>
        /// Testa se o Get por id retorna 200 OK com o DTO quando a movimentacao existe.
        /// </summary>
        [Fact]
        public async Task Get_QuandoMovimentacaoExiste_DeveRetornar200ComDTO()
        {
            // Arrange
            var movimentacao = new Movimentacao { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };
            var movimentacaoDTO = new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };
            ConfigurarMovimentacao(movimentacao);
            _mapper.Setup(m => m.Map<MovimentacaoDTO>(movimentacao)).Returns(movimentacaoDTO);

            // Act
            var result = await _controller.Get(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<MovimentacaoDTO>(ok.Value);
            Assert.Equal(1, dto.MovimentacaoId);
        }

        /// <summary>
        /// Testa se o Get por id retorna 404 NotFound quando a movimentacao nao existe.
        /// </summary>
        [Fact]
        public async Task Get_QuandoMovimentacaoNaoExiste_DeveRetornar404()
        {
            // Arrange
            ConfigurarMovimentacao(null);

            // Act
            var result = await _controller.Get(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        // ---------------------------- GET por produto / colaborador ------------------------------

        /// <summary>
        /// Testa se o GetByProduto retorna 200 OK com os DTOs quando há movimentações do produto.
        /// </summary>
        [Fact]
        public async Task GetByProduto_QuandoExistem_DeveRetornar200ComDTOs()
        {
            // Arrange
            var movimentacoes = new List<Movimentacao>
            {
                new() { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 },
                new() { MovimentacaoId = 2, Tipo = 'S', Quantidade = 2, ProdutoId = 1, ColaboradorId = 7 },
            };
            var dtos = new List<MovimentacaoDTO>
            {
                new() { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 },
                new() { MovimentacaoId = 2, Tipo = 'S', Quantidade = 2, ProdutoId = 1, ColaboradorId = 7 },
            };
            _movimentacaoRepo.Setup(r => r.GetByProdutoIdAsync(1)).ReturnsAsync(movimentacoes);
            _mapper.Setup(m => m.Map<IEnumerable<MovimentacaoDTO>>(movimentacoes)).Returns(dtos);

            // Act
            var result = await _controller.GetByProduto(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var retorno = Assert.IsAssignableFrom<IEnumerable<MovimentacaoDTO>>(ok.Value);
            Assert.Equal(2, retorno.Count());
        }

        /// <summary>
        /// Testa se o GetByProduto retorna 404 NotFound quando não há movimentações do produto.
        /// </summary>
        [Fact]
        public async Task GetByProduto_QuandoVazio_DeveRetornar404()
        {
            // Arrange
            _movimentacaoRepo.Setup(r => r.GetByProdutoIdAsync(It.IsAny<int>()))
                             .ReturnsAsync(new List<Movimentacao>());

            // Act
            var result = await _controller.GetByProduto(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /// <summary>
        /// Testa se o GetByColaborador retorna 200 OK com os DTOs quando há movimentações do colaborador.
        /// </summary>
        [Fact]
        public async Task GetByColaborador_QuandoExistem_DeveRetornar200ComDTOs()
        {
            // Arrange
            var movimentacoes = new List<Movimentacao>
            {
                new() { MovimentacaoId = 3, Tipo = 'S', Quantidade = 1, ProdutoId = 2, ColaboradorId = 7 },
            };
            var dtos = new List<MovimentacaoDTO>
            {
                new() { MovimentacaoId = 3, Tipo = 'S', Quantidade = 1, ProdutoId = 2, ColaboradorId = 7 },
            };
            _movimentacaoRepo.Setup(r => r.GetByColaboradorIdAsync(7)).ReturnsAsync(movimentacoes);
            _mapper.Setup(m => m.Map<IEnumerable<MovimentacaoDTO>>(movimentacoes)).Returns(dtos);

            // Act
            var result = await _controller.GetByColaborador(7);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var retorno = Assert.IsAssignableFrom<IEnumerable<MovimentacaoDTO>>(ok.Value);
            Assert.Single(retorno);
        }

        /// <summary>
        /// Testa se o GetByColaborador retorna 404 NotFound quando não há movimentações do colaborador.
        /// </summary>
        [Fact]
        public async Task GetByColaborador_QuandoVazio_DeveRetornar404()
        {
            // Arrange
            _movimentacaoRepo.Setup(r => r.GetByColaboradorIdAsync(It.IsAny<int>()))
                             .ReturnsAsync(new List<Movimentacao>());

            // Act
            var result = await _controller.GetByColaborador(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        // ---------------------------- POST (entrada/saida/ajuste) ------------------------------

        /// <summary>
        /// Testa se a Entrada retorna 201 CreatedAtRoute ("ObterMovimentacao") quando o EstoqueService retorna Sucesso.
        /// </summary>
        [Fact]
        public async Task Entrada_QuandoSucesso_DeveRetornar201CreatedAtRoute()
        {
            // Arrange
            var request = new EntradaEstoqueRequest { ProdutoId = 1, Quantidade = 5 };
            var movimentacao = new Movimentacao { MovimentacaoId = 10, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };
            var movimentacaoDTO = new MovimentacaoDTO { MovimentacaoId = 10, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };

            _estoqueService
                .Setup(s => s.RegistrarEntradaAsync(1, 5, It.IsAny<string?>()))
                .ReturnsAsync(EstoqueResult.Ok(movimentacao));
            _mapper.Setup(m => m.Map<MovimentacaoDTO>(movimentacao)).Returns(movimentacaoDTO);

            // Act
            var result = await _controller.Entrada(request);

            // Assert
            var created = Assert.IsType<CreatedAtRouteResult>(result.Result);
            Assert.Equal("ObterMovimentacao", created.RouteName);
            var dto = Assert.IsType<MovimentacaoDTO>(created.Value);
            Assert.Equal(10, dto.MovimentacaoId);
        }

        /// <summary>
        /// Testa se a Entrada retorna 404 NotFound quando o EstoqueService retorna ProdutoNaoEncontrado.
        /// </summary>
        [Fact]
        public async Task Entrada_QuandoProdutoNaoEncontrado_DeveRetornar404()
        {
            // Arrange
            var request = new EntradaEstoqueRequest { ProdutoId = 99, Quantidade = 5 };
            _estoqueService
                .Setup(s => s.RegistrarEntradaAsync(99, 5, It.IsAny<string?>()))
                .ReturnsAsync(EstoqueResult.Falha(EstoqueResultStatus.ProdutoNaoEncontrado, "Produto nao encontrado."));

            // Act
            var result = await _controller.Entrada(request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /// <summary>
        /// Testa se a Saida retorna 404 NotFound quando o EstoqueService retorna ColaboradorNaoEncontrado.
        /// </summary>
        [Fact]
        public async Task Saida_QuandoColaboradorNaoEncontrado_DeveRetornar404()
        {
            // Arrange
            var request = new SaidaEstoqueRequest { ProdutoId = 1, Quantidade = 5, ColaboradorId = 99 };
            _estoqueService
                .Setup(s => s.RegistrarSaidaAsync(1, 5, 99, It.IsAny<string?>()))
                .ReturnsAsync(EstoqueResult.Falha(EstoqueResultStatus.ColaboradorNaoEncontrado, "Colaborador nao encontrado."));

            // Act
            var result = await _controller.Saida(request);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /// <summary>
        /// Testa se a Saida retorna 400 BadRequest quando o EstoqueService retorna SaldoInsuficiente.
        /// </summary>
        [Fact]
        public async Task Saida_QuandoSaldoInsuficiente_DeveRetornar400()
        {
            // Arrange
            var request = new SaidaEstoqueRequest { ProdutoId = 1, Quantidade = 50, ColaboradorId = 7 };
            _estoqueService
                .Setup(s => s.RegistrarSaidaAsync(1, 50, 7, It.IsAny<string?>()))
                .ReturnsAsync(EstoqueResult.Falha(EstoqueResultStatus.SaldoInsuficiente, "Saldo insuficiente."));

            // Act
            var result = await _controller.Saida(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        /// <summary>
        /// Testa se o Ajuste retorna 201 CreatedAtRoute quando o EstoqueService retorna Sucesso.
        /// </summary>
        [Fact]
        public async Task Ajuste_QuandoSucesso_DeveRetornar201CreatedAtRoute()
        {
            // Arrange
            var request = new AjusteEstoqueRequest { ProdutoId = 1, Quantidade = 3 };
            var movimentacao = new Movimentacao { MovimentacaoId = 20, Tipo = 'A', Quantidade = 3, ProdutoId = 1 };
            var movimentacaoDTO = new MovimentacaoDTO { MovimentacaoId = 20, Tipo = 'A', Quantidade = 3, ProdutoId = 1 };

            _estoqueService
                .Setup(s => s.RegistrarAjusteAsync(1, 3, It.IsAny<string?>()))
                .ReturnsAsync(EstoqueResult.Ok(movimentacao));
            _mapper.Setup(m => m.Map<MovimentacaoDTO>(movimentacao)).Returns(movimentacaoDTO);

            // Act
            var result = await _controller.Ajuste(request);

            // Assert
            var created = Assert.IsType<CreatedAtRouteResult>(result.Result);
            Assert.Equal("ObterMovimentacao", created.RouteName);
        }

        // ---------------------------- PUT ------------------------------

        /// <summary>
        /// Testa se o Put retorna 400 BadRequest quando o id da rota diverge do id do DTO,
        /// sem sequer acionar o EstoqueService.
        /// </summary>
        [Fact]
        public async Task Put_QuandoIdDivergeDoDTO_DeveRetornar400()
        {
            // Arrange
            var movimentacaoDTO = new MovimentacaoDTO { MovimentacaoId = 2, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };

            // Act (id da rota = 1, id do DTO = 2)
            var result = await _controller.Put(1, movimentacaoDTO);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
            _estoqueService.Verify(
                s => s.AtualizarMovimentacaoAsync(It.IsAny<int>(), It.IsAny<MovimentacaoDTO>()), Times.Never);
        }

        /// <summary>
        /// Testa se o Put delega ao EstoqueService (e nao escreve pelo repositorio),
        /// repassando o id da rota e o DTO recebido.
        /// </summary>
        [Fact]
        public async Task Put_DeveDelegarAoEstoqueService()
        {
            // Arrange
            var movimentacaoDTO = new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };
            var movimentacao = new Movimentacao { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };
            ConfigurarAtualizacao(EstoqueResult.Ok(movimentacao));
            _mapper.Setup(m => m.Map<MovimentacaoDTO>(movimentacao)).Returns(movimentacaoDTO);

            // Act
            await _controller.Put(1, movimentacaoDTO);

            // Assert: a regra de saldo vive no service; o controller nao toca no repositorio.
            _estoqueService.Verify(s => s.AtualizarMovimentacaoAsync(1, movimentacaoDTO), Times.Once);
            _movimentacaoRepo.Verify(r => r.Update(It.IsAny<Movimentacao>()), Times.Never);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        /// <summary>
        /// Testa se o Put retorna 404 NotFound quando o service retorna MovimentacaoNaoEncontrada.
        /// </summary>
        [Fact]
        public async Task Put_QuandoMovimentacaoNaoExiste_DeveRetornar404()
        {
            // Arrange
            var movimentacaoDTO = new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };
            ConfigurarAtualizacao(EstoqueResult.Falha(
                EstoqueResultStatus.MovimentacaoNaoEncontrada, "Movimentacao nao encontrada."));

            // Act
            var result = await _controller.Put(1, movimentacaoDTO);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /// <summary>
        /// Testa se o Put retorna 400 BadRequest quando o estorno deixaria o saldo negativo.
        /// </summary>
        [Fact]
        public async Task Put_QuandoSaldoNegativoAposEstorno_DeveRetornar400()
        {
            // Arrange
            var movimentacaoDTO = new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };
            ConfigurarAtualizacao(EstoqueResult.Falha(
                EstoqueResultStatus.SaldoNegativoAposEstorno, "Saldo ficaria negativo."));

            // Act
            var result = await _controller.Put(1, movimentacaoDTO);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        /// <summary>
        /// Testa se o Put retorna 400 BadRequest quando o service retorna TipoInvalido.
        /// </summary>
        [Fact]
        public async Task Put_QuandoTipoInvalido_DeveRetornar400()
        {
            // Arrange
            var movimentacaoDTO = new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'X', Quantidade = 5, ProdutoId = 1 };
            ConfigurarAtualizacao(EstoqueResult.Falha(EstoqueResultStatus.TipoInvalido, "Tipo invalido."));

            // Act
            var result = await _controller.Put(1, movimentacaoDTO);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        /// <summary>
        /// Testa se o Put retorna 200 OK (e nao 201) com o DTO da movimentacao alterada.
        /// </summary>
        [Fact]
        public async Task Put_MovimentacaoValida_DeveRetornar200ComDTO()
        {
            // Arrange
            var movimentacaoDTO = new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'S', Quantidade = 2, ProdutoId = 1, ColaboradorId = 7 };
            var movimentacao = new Movimentacao { MovimentacaoId = 1, Tipo = 'S', Quantidade = 2, ProdutoId = 1, ColaboradorId = 7 };

            ConfigurarAtualizacao(EstoqueResult.Ok(movimentacao));
            _mapper.Setup(m => m.Map<MovimentacaoDTO>(movimentacao)).Returns(movimentacaoDTO);

            // Act
            var result = await _controller.Put(1, movimentacaoDTO);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<MovimentacaoDTO>(ok.Value);
            Assert.Equal(1, dto.MovimentacaoId);
        }

        // ---------------------------- DELETE ------------------------------

        /// <summary>
        /// Testa se o Delete retorna 404 NotFound quando o service retorna MovimentacaoNaoEncontrada.
        /// </summary>
        [Fact]
        public async Task Delete_QuandoMovimentacaoNaoExiste_DeveRetornar404()
        {
            // Arrange
            ConfigurarExclusao(EstoqueResult.Falha(
                EstoqueResultStatus.MovimentacaoNaoEncontrada, "Movimentacao nao encontrada."));

            // Act
            var result = await _controller.Delete(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /// <summary>
        /// Testa se o Delete retorna 400 BadRequest quando o estorno deixaria o saldo negativo.
        /// </summary>
        [Fact]
        public async Task Delete_QuandoSaldoNegativoAposEstorno_DeveRetornar400()
        {
            // Arrange
            ConfigurarExclusao(EstoqueResult.Falha(
                EstoqueResultStatus.SaldoNegativoAposEstorno, "Saldo ficaria negativo."));

            // Act
            var result = await _controller.Delete(1);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        /// <summary>
        /// Testa se o Delete delega ao EstoqueService e retorna 200 OK com o DTO excluido,
        /// sem escrever pelo repositorio.
        /// </summary>
        [Fact]
        public async Task Delete_MovimentacaoValida_DeveRetornar200ComDTO()
        {
            // Arrange
            var movimentacao = new Movimentacao { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };
            var movimentacaoDTO = new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };

            ConfigurarExclusao(EstoqueResult.Ok(movimentacao));
            _mapper.Setup(m => m.Map<MovimentacaoDTO>(movimentacao)).Returns(movimentacaoDTO);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<MovimentacaoDTO>(ok.Value);
            Assert.Equal(1, dto.MovimentacaoId);

            _estoqueService.Verify(s => s.ExcluirMovimentacaoAsync(1), Times.Once);
            _movimentacaoRepo.Verify(r => r.Delete(It.IsAny<Movimentacao>()), Times.Never);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        // ---------------------------- GET pagination / relatorio ------------------------------

        /// <summary>
        /// Testa se o Get paginado retorna 400 BadRequest quando DataInicio e maior que DataFim,
        /// sem sequer consultar o repositorio.
        /// </summary>
        [Fact]
        public async Task GetPagination_ComIntervaloInvertido_DeveRetornar400ENaoConsultarRepositorio()
        {
            // Arrange
            var parameters = new MovimentacoesParameters
            {
                DataInicio = new DateTime(2026, 1, 20),
                DataFim = new DateTime(2026, 1, 10)
            };

            // Act
            var result = await _controller.Get(parameters);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
            _movimentacaoRepo.Verify(
                r => r.GetMovimentacoesAsync(It.IsAny<MovimentacoesParameters>()), Times.Never);
        }

        /// <summary>
        /// Testa se o GetRelatorio mantem a mesma validacao de intervalo apos a extracao do helper.
        /// </summary>
        [Fact]
        public async Task GetRelatorio_ComIntervaloInvertido_DeveRetornar400ENaoConsultarRepositorio()
        {
            // Arrange
            var parameters = new MovimentacoesParameters
            {
                DataInicio = new DateTime(2026, 1, 20),
                DataFim = new DateTime(2026, 1, 10)
            };

            // Act
            var result = await _controller.GetRelatorio(parameters);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
            _movimentacaoRepo.Verify(
                r => r.GetRelatorioAsync(It.IsAny<MovimentacoesParameters>()), Times.Never);
        }

        /// <summary>
        /// Testa se o Get paginado retorna 400 BadRequest quando o tipo esta fora de E/S/A,
        /// em vez de consultar o banco e devolver lista vazia com cara de "nao houve movimentacao".
        /// </summary>
        [Fact]
        public async Task GetPagination_ComTipoInvalido_DeveRetornar400ENaoConsultarRepositorio()
        {
            // Arrange
            var parameters = new MovimentacoesParameters { Tipo = 'X' };

            // Act
            var result = await _controller.Get(parameters);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
            _movimentacaoRepo.Verify(
                r => r.GetMovimentacoesAsync(It.IsAny<MovimentacoesParameters>()), Times.Never);
        }

        /// <summary>
        /// Testa se o GetRelatorio aplica a mesma validacao de tipo do Get paginado: os dois
        /// expoem o mesmo MovimentacoesParameters.
        /// </summary>
        [Fact]
        public async Task GetRelatorio_ComTipoInvalido_DeveRetornar400ENaoConsultarRepositorio()
        {
            // Arrange
            var parameters = new MovimentacoesParameters { Tipo = 'X' };

            // Act
            var result = await _controller.GetRelatorio(parameters);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
            _movimentacaoRepo.Verify(
                r => r.GetRelatorioAsync(It.IsAny<MovimentacoesParameters>()), Times.Never);
        }

        /// <summary>
        /// Testa se o tipo em minuscula passa pela validacao: o parameters normaliza a caixa,
        /// entao 's' e um filtro valido e nao pode virar 400.
        /// </summary>
        [Fact]
        public async Task GetPagination_ComTipoEmMinusculo_DeveConsultarRepositorioComTipoNormalizado()
        {
            // Arrange
            MovimentacoesParameters? enviado = null;
            _movimentacaoRepo
                .Setup(r => r.GetMovimentacoesAsync(It.IsAny<MovimentacoesParameters>()))
                .Callback<MovimentacoesParameters>(p => enviado = p)
                .ReturnsAsync(new List<Movimentacao>().ToPagedList(1, 10));

            var parameters = new MovimentacoesParameters { Tipo = 's' };

            // Act
            var result = await _controller.Get(parameters);

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal('S', enviado!.Tipo);
        }

        /// <summary>
        /// Testa se a validacao de tipo nao atrapalha quem nao filtra por tipo: sem o filtro,
        /// a consulta segue normalmente.
        /// </summary>
        [Fact]
        public async Task GetPagination_SemTipo_NaoDeveCairNaValidacaoDeTipo()
        {
            // Arrange
            _movimentacaoRepo
                .Setup(r => r.GetMovimentacoesAsync(It.IsAny<MovimentacoesParameters>()))
                .ReturnsAsync(new List<Movimentacao>().ToPagedList(1, 10));

            // Act
            var result = await _controller.Get(new MovimentacoesParameters());

            // Assert
            Assert.IsType<OkObjectResult>(result.Result);
            _movimentacaoRepo.Verify(
                r => r.GetMovimentacoesAsync(It.IsAny<MovimentacoesParameters>()), Times.Once);
        }
    }
}
