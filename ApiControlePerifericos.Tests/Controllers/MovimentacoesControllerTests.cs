using ApiControlePerifericos.Controllers;
using ApiControlePerifericos.DTOs;
using ApiControlePerifericos.DTOs.Estoque;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Services;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using System.Security.Claims;

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
        /// Testa se o Put retorna 400 BadRequest quando o id da rota diverge do id do DTO.
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
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        /// <summary>
        /// Testa se o Put retorna 404 NotFound quando a movimentacao nao existe.
        /// </summary>
        [Fact]
        public async Task Put_QuandoMovimentacaoNaoExiste_DeveRetornar404()
        {
            // Arrange
            var movimentacaoDTO = new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };
            ConfigurarMovimentacao(null);

            // Act
            var result = await _controller.Put(1, movimentacaoDTO);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        /// <summary>
        /// Testa se o Put retorna 200 OK com o DTO atualizado quando a movimentacao e valida.
        /// </summary>
        [Fact]
        public async Task Put_MovimentacaoValida_DeveRetornar200ComDTO()
        {
            // Arrange
            var movimentacaoDTO = new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };
            var movimentacao = new Movimentacao { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };

            ConfigurarMovimentacao(movimentacao);
            _mapper.Setup(m => m.Map<Movimentacao>(movimentacaoDTO)).Returns(movimentacao);
            _movimentacaoRepo.Setup(r => r.Update(movimentacao)).Returns(movimentacao);
            _mapper.Setup(m => m.Map<MovimentacaoDTO>(movimentacao)).Returns(movimentacaoDTO);

            // Act
            var result = await _controller.Put(1, movimentacaoDTO);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<MovimentacaoDTO>(ok.Value);
            Assert.Equal(1, dto.MovimentacaoId);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        // ---------------------------- DELETE ------------------------------

        /// <summary>
        /// Testa se o Delete retorna 404 NotFound quando a movimentacao nao existe.
        /// </summary>
        [Fact]
        public async Task Delete_QuandoMovimentacaoNaoExiste_DeveRetornar404()
        {
            // Arrange
            ConfigurarMovimentacao(null);

            // Act
            var result = await _controller.Delete(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        /// <summary>
        /// Testa se o Delete retorna 200 OK com o DTO quando a movimentacao e excluida com sucesso.
        /// </summary>
        [Fact]
        public async Task Delete_MovimentacaoValida_DeveRetornar200ComDTO()
        {
            // Arrange
            var movimentacao = new Movimentacao { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };
            var movimentacaoDTO = new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };

            ConfigurarMovimentacao(movimentacao);
            _movimentacaoRepo.Setup(r => r.Delete(movimentacao)).Returns(movimentacao);
            _mapper.Setup(m => m.Map<MovimentacaoDTO>(movimentacao)).Returns(movimentacaoDTO);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<MovimentacaoDTO>(ok.Value);
            Assert.Equal(1, dto.MovimentacaoId);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}
