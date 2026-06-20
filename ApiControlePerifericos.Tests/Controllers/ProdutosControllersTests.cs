using ApiControlePerifericos.Controllers;
using ApiControlePerifericos.DTOs;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace ApiControlePerifericos.Tests.Controllers
{
    public class ProdutosControllersTests
    {
        private readonly Mock<IProdutoRepository> _produtoRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<ProdutosController>> _logger = new();
        private readonly ProdutosController _controller;

        public ProdutosControllersTests()
        {
            _uow.Setup(u => u.ProdutoRepository).Returns(_produtoRepo.Object);
            _controller = new ProdutosController(_uow.Object, _logger.Object, _mapper.Object);
        }

        //Helpers de Arrange
        private void ConfigurarProduto(Produto? produto) =>
            _produtoRepo
                .Setup(r => r.GetAsync(It.IsAny<Expression<Func<Produto, bool>>>()))
                .ReturnsAsync(produto);

        //------------------------------------- GET Lista ---------------------------------

        /// <summary>
        /// Testa o método Get do controller ProdutosController para verificar se ele retorna uma lista de produtos corretamente.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Get_QuandoExistemProdutos_DeveRetornar200ComDTOS()
        {
            // Arrange
            var produtos = new List<Produto>
            {
                new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 10 },
                new Produto { ProdutoId = 2, Descricao = "Teclado", SaldoAtual = 5 }
            };
           
            var produtosDTO = new List<ProdutoDTO>
            {
                new ProdutoDTO { ProdutoId = 1, Descricao = "Mouse" },
                new ProdutoDTO { ProdutoId = 2, Descricao = "Teclado" }
            };

            _produtoRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(produtos);
            _mapper.Setup(m => m.Map<IEnumerable<ProdutoDTO>>(produtos)).Returns(produtosDTO);

            //Act
            var result = await _controller.Get();

            //Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dtos = Assert.IsAssignableFrom<IEnumerable<ProdutoDTO>>(ok.Value);
            Assert.Equal(2, dtos.Count());
        }

        /// <summary>
        /// Testa o método Get do controller ProdutosController para verificar se ele retorna um produto corretamente.
        /// </summary>
        /// <returns></returns>

        [Fact]
        public async Task Get_QuandoProdutoExiste_DeveRetornar200ComDTO()
        {
            // Arrange
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 10 };
            var produtoDTO = new ProdutoDTO { ProdutoId = 1, Descricao = "Mouse"};
            ConfigurarProduto(produto);
            _mapper.Setup(m => m.Map<ProdutoDTO>(produto)).Returns(produtoDTO);

            // Act
            var result = await _controller.Get(1);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<ProdutoDTO>(ok.Value);
            Assert.Equal(1, dto.ProdutoId);
            Assert.Equal("Mouse", dto.Descricao);
        }

        /// <summary>
        /// Testa o método Get do controller ProdutosController para verificar se ele retorna NotFound quando o produto não existe.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Get_QuandoProdutoNaoExiste_DeveRetornar404()
        {
            // Arrange
            ConfigurarProduto(null);
            // Act
            var result = await _controller.Get(999);
            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Produto com ID 999 não encontrado.", notFound.Value);
        }

        //--------------------- POST ------------------------------

        /// <summary>
        /// Testa o método Post do controller ProdutosController para verificar se ele retorna CreatedAtRoute quando um produto válido é enviado.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Post_ProdutoValido_DeveRetornar201CreatedAtRoute()
        {
            //Arrange
            var produtoDTO = new ProdutoDTO { ProdutoId = 1, Descricao = "Mouse" };
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse" };

            _mapper.Setup(m => m.Map<Produto>(produtoDTO)).Returns(produto);
            _produtoRepo.Setup(r => r.Create(produto)).Returns(produto);
            _mapper.Setup(m => m.Map<ProdutoDTO>(produto)).Returns(produtoDTO);

            //Act
            var result = await _controller.Post(produtoDTO);

            //Assert
            var created = Assert.IsType<CreatedAtRouteResult>(result.Result);

            Assert.Equal("ObterProduto", created.RouteName);
            var dto = Assert.IsType<ProdutoDTO>(created.Value);
            Assert.Equal(1, dto.ProdutoId);
            _uow.Verify(u => u.CommitAsync(), Times.Once);

        }

        //------------------------- PUT -----------------------------

        /// <summary>
        /// Testa o método Put do controller ProdutosController para verificar se ele retorna BadRequest quando o ID do produto no DTO diverge do ID passado na URL.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Put_QuandoIdDivergeDoDTO_DeveRetornar400()
        {
            //Arrange
            var produtoDTO = new ProdutoDTO { ProdutoId = 2, Descricao = "Mouse" };

            //Act
            var result = await _controller.Put(1, produtoDTO);

            //Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
            _uow.Verify(u => u.CommitAsync(), Times.Never);

        }

        /// <summary>
        /// Testa o método Put do controller ProdutosController para verificar se ele retorna NotFound quando o produto não existe.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Put_QuandoProdutoNaoExiste_DeveRetornar404()
        {
            //Arrange
            var produtoDTO = new ProdutoDTO { ProdutoId = 1, Descricao = "Mouse" };
            ConfigurarProduto(null);

            //Act
            var result = await _controller.Put(1, produtoDTO);

            //Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }


        /// <summary>
        /// Testa o método Put do controller ProdutosController para verificar se ele retorna Ok com o DTO atualizado quando um produto válido é enviado.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Put_ProdutoValido_DeveRetornar200ComDTO()
        {
            //Arrange
            var produtoDTO = new ProdutoDTO { ProdutoId = 1, Descricao = "Mouse" };
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse" };

            ConfigurarProduto(produto);
            _mapper.Setup(m => m.Map<Produto>(produtoDTO)).Returns(produto);
            _produtoRepo.Setup(r => r.Update(produto)).Returns(produto);
            _mapper.Setup(m => m.Map<ProdutoDTO>(produto)).Returns(produtoDTO);

            //act
            var result = await _controller.Put(1, produtoDTO);

            //Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<ProdutoDTO>(ok.Value);
            Assert.Equal(1, dto.ProdutoId);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        //---------------------------- Delete ------------------------------

        /// <summary>
        /// Testa o método Delete do controller ProdutosController para verificar se ele retorna NotFound quando o produto não existe.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Delete_QuandoProdutoNaoExiste_DeveRetornar404()
        {
            //arrange
            ConfigurarProduto(null);

            //Act
            var result = await _controller.Delete(999);

            //Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        /// <summary>
        /// Testa o método Delete do controller ProdutosController para verificar se ele retorna Ok com o DTO do produto deletado quando um produto válido é deletado.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Delete_ProdutoValido_DeveRetornar200ComDTO()
        {
            //Arrange
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 10 };
            var produtoDTO = new ProdutoDTO { ProdutoId = 1, Descricao = "Mouse" };

            ConfigurarProduto(produto);
            _produtoRepo.Setup(r => r.Delete(produto)).Returns(produto);
            _mapper.Setup(m => m.Map<ProdutoDTO>(produto)).Returns(produtoDTO);

            //Act
            var result = await _controller.Delete(1);

            //Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<ProdutoDTO>(ok.Value);
            Assert.Equal(1, dto.ProdutoId);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }


    }   

}
