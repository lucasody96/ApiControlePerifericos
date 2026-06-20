using ApiControlePerifericos.Controllers;
using ApiControlePerifericos.DTOs;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace ApiControlePerifericos.Tests.Controllers
{
    public class ColaboradoresControllerTests
    {
        private readonly Mock<IColaboradorRepository> _colaboradorRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<ColaboradoresController>> _logger = new();
        private readonly ColaboradoresController _controller;

        public ColaboradoresControllerTests()
        {
            _uow.Setup(u => u.ColaboradorRepository).Returns(_colaboradorRepo.Object);
            _controller = new ColaboradoresController(_uow.Object, _logger.Object, _mapper.Object);
        }

        // Helper: configura o GetAsync (o mesmo metodo que o controller usa para buscar por id)
        private void ConfigurarColaborador(Colaborador? colaborador) =>
            _colaboradorRepo
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Colaborador, bool>>>()))
                .ReturnsAsync(colaborador);

        // ---------------------------- GET lista ------------------------------

        /// <summary>
        /// Testa se o Get (lista) retorna 200 OK com os DTOs mapeados quando existem colaboradores.
        /// </summary>
        [Fact]
        public async Task Get_QuandoExistemColaboradores_DeveRetornar200ComDTOs()
        {
            // Arrange
            var colaboradores = new List<Colaborador>
            {
                new() { ColaboradorId = 1, Nome = "Fulano" },
                new() { ColaboradorId = 2, Nome = "Beltrano" }
            };
            var colaboradoresDTO = new List<ColaboradorDTO>
            {
                new() { ColaboradorId = 1, Nome = "Fulano" },
                new() { ColaboradorId = 2, Nome = "Beltrano" }
            };

            _colaboradorRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(colaboradores);
            _mapper.Setup(m => m.Map<IEnumerable<ColaboradorDTO>>(colaboradores)).Returns(colaboradoresDTO);

            // Act
            var result = await _controller.Get();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<ColaboradorDTO>>(ok.Value);
            Assert.Equal(2, dtos.Count());
        }

        // ---------------------------- GET por id ------------------------------

        /// <summary>
        /// Testa se o Get por id retorna 200 OK com o DTO quando o colaborador existe.
        /// </summary>
        [Fact]
        public async Task Get_QuandoColaboradorExiste_DeveRetornar200ComDTO()
        {
            // Arrange
            var colaborador = new Colaborador { ColaboradorId = 1, Nome = "Fulano" };
            var colaboradorDTO = new ColaboradorDTO { ColaboradorId = 1, Nome = "Fulano" };
            ConfigurarColaborador(colaborador);
            _mapper.Setup(m => m.Map<ColaboradorDTO>(colaborador)).Returns(colaboradorDTO);

            // Act
            var result = await _controller.Get(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<ColaboradorDTO>(ok.Value);
            Assert.Equal(1, dto.ColaboradorId);
            Assert.Equal("Fulano", dto.Nome);
        }

        /// <summary>
        /// Testa se o Get por id retorna 404 NotFound quando o colaborador nao existe.
        /// </summary>
        [Fact]
        public async Task Get_QuandoColaboradorNaoExiste_DeveRetornar404()
        {
            // Arrange
            ConfigurarColaborador(null);

            // Act
            var result = await _controller.Get(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        // ---------------------------- POST ------------------------------

        /// <summary>
        /// Testa se o Post retorna 201 CreatedAtRoute com a rota "ObterColaborador" e persiste via CommitAsync.
        /// </summary>
        [Fact]
        public async Task Post_ColaboradorValido_DeveRetornar201CreatedAtRoute()
        {
            // Arrange
            var colaboradorDTO = new ColaboradorDTO { ColaboradorId = 1, Nome = "Fulano" };
            var colaborador = new Colaborador { ColaboradorId = 1, Nome = "Fulano" };

            _mapper.Setup(m => m.Map<Colaborador>(colaboradorDTO)).Returns(colaborador);
            _colaboradorRepo.Setup(r => r.Create(colaborador)).Returns(colaborador);
            _mapper.Setup(m => m.Map<ColaboradorDTO>(colaborador)).Returns(colaboradorDTO);

            // Act
            var result = await _controller.Post(colaboradorDTO);

            // Assert
            var created = Assert.IsType<CreatedAtRouteResult>(result.Result);
            Assert.Equal("ObterColaborador", created.RouteName);
            var dto = Assert.IsType<ColaboradorDTO>(created.Value);
            Assert.Equal(1, dto.ColaboradorId);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        // ---------------------------- PUT ------------------------------

        /// <summary>
        /// Testa se o Put retorna 400 BadRequest quando o id da rota diverge do id do DTO.
        /// </summary>
        [Fact]
        public async Task Put_QuandoIdDivergeDoDTO_DeveRetornar400()
        {
            // Arrange
            var colaboradorDTO = new ColaboradorDTO { ColaboradorId = 2, Nome = "Fulano" };

            // Act (id da rota = 1, id do DTO = 2)
            var result = await _controller.Put(1, colaboradorDTO);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        /// <summary>
        /// Testa se o Put retorna 404 NotFound quando o colaborador nao existe.
        /// </summary>
        [Fact]
        public async Task Put_QuandoColaboradorNaoExiste_DeveRetornar404()
        {
            // Arrange
            var colaboradorDTO = new ColaboradorDTO { ColaboradorId = 1, Nome = "Fulano" };
            ConfigurarColaborador(null);

            // Act
            var result = await _controller.Put(1, colaboradorDTO);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        /// <summary>
        /// Testa se o Put retorna 200 OK com o DTO atualizado quando o colaborador e valido.
        /// </summary>
        [Fact]
        public async Task Put_ColaboradorValido_DeveRetornar200ComDTO()
        {
            // Arrange
            var colaboradorDTO = new ColaboradorDTO { ColaboradorId = 1, Nome = "Fulano" };
            var colaborador = new Colaborador { ColaboradorId = 1, Nome = "Fulano" };

            ConfigurarColaborador(colaborador);
            _mapper.Setup(m => m.Map<Colaborador>(colaboradorDTO)).Returns(colaborador);
            _colaboradorRepo.Setup(r => r.Update(colaborador)).Returns(colaborador);
            _mapper.Setup(m => m.Map<ColaboradorDTO>(colaborador)).Returns(colaboradorDTO);

            // Act
            var result = await _controller.Put(1, colaboradorDTO);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<ColaboradorDTO>(ok.Value);
            Assert.Equal(1, dto.ColaboradorId);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        // ---------------------------- DELETE ------------------------------

        /// <summary>
        /// Testa se o Delete retorna 404 NotFound quando o colaborador nao existe.
        /// </summary>
        [Fact]
        public async Task Delete_QuandoColaboradorNaoExiste_DeveRetornar404()
        {
            // Arrange
            ConfigurarColaborador(null);

            // Act
            var result = await _controller.Delete(99);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        /// <summary>
        /// Testa se o Delete retorna 200 OK com o DTO quando o colaborador e excluido com sucesso.
        /// </summary>
        [Fact]
        public async Task Delete_ColaboradorValido_DeveRetornar200ComDTO()
        {
            // Arrange
            var colaborador = new Colaborador { ColaboradorId = 1, Nome = "Fulano" };
            var colaboradorDTO = new ColaboradorDTO { ColaboradorId = 1, Nome = "Fulano" };

            ConfigurarColaborador(colaborador);
            _colaboradorRepo.Setup(r => r.Delete(colaborador)).Returns(colaborador);
            _mapper.Setup(m => m.Map<ColaboradorDTO>(colaborador)).Returns(colaboradorDTO);

            // Act
            var result = await _controller.Delete(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<ColaboradorDTO>(ok.Value);
            Assert.Equal(1, dto.ColaboradorId);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }
    }
}
