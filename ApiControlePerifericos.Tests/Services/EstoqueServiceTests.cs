using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;


namespace ApiControlePerifericos.Tests.Services
{
    public class EstoqueServiceTests
    {
        private readonly Mock<IProdutoRepository> _produtoRepo = new();
        private readonly Mock<IColaboradorRepository> _colaboradorRepo = new();
        private readonly Mock<IMovimentacaoRepository> _movimentacaoRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<ILogger<EstoqueService>> _logger = new();
        private readonly Mock<IProdutoCacheInvalidator> _produtoCache = new();
        private readonly EstoqueService _service;

        public EstoqueServiceTests()
        {
            // Liga o UnitOfWork mockado aos reposit�rios mockados.
            _uow.Setup(u => u.ProdutoRepository).Returns(_produtoRepo.Object);
            _uow.Setup(u => u.ColaboradorRepository).Returns(_colaboradorRepo.Object);
            _uow.Setup(u => u.MovimentacaoRepository).Returns(_movimentacaoRepo.Object);

            _service = new EstoqueService(_uow.Object, _logger.Object, _produtoCache.Object);
        }

        //Helpers de Arrange
        private void ConfigurarProduto(Produto? produto, int produtoId = 1) =>
            _produtoRepo.Setup(r => r.GetByIdTrackedAsync(produtoId)).ReturnsAsync(produto);

        private void ConfigurarColaborador(Colaborador? colaborador) =>
            _colaboradorRepo.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Colaborador, bool>>>())).ReturnsAsync(colaborador);

        [Fact]
        public async Task RegistrarEntradaAsync_DeveRetornarProdutoNaoEncontrado()
        {   
            
            // Arrange
            ConfigurarProduto(null);
            
            // Act
            var result = await _service.RegistrarEntradaAsync(produtoId: 1, quantidade: 5, registradoPor: "lucas.ody");

            // Assert
            Assert.Equal(EstoqueResultStatus.ProdutoNaoEncontrado, result.Status);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task RegistrarEntradaAsync_DeveAumentarSaldoEPersistir()
        {
            //arrange
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 10 };
            ConfigurarProduto(produto);

            //Act
            var resultado = await _service.RegistrarEntradaAsync(produtoId: 1, quantidade: 5, registradoPor: "lucas.ody");

            //Assert
            Assert.Equal(EstoqueResultStatus.Sucesso, resultado.Status);
            Assert.Equal(15, produto.SaldoAtual);

            _movimentacaoRepo.Verify(r => r.Create(It.IsAny<Movimentacao>()), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task RegistrarEntradaAsync_DeveInvalidarCacheDeProdutos_QuandoSucesso()
        {
            // Arrange
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 10 };
            ConfigurarProduto(produto);

            // Act
            await _service.RegistrarEntradaAsync(produtoId: 1, quantidade: 5, registradoPor: "lucas.ody");

            // Assert: o saldo mudou, então o cache de produtos deve ser invalidado.
            _produtoCache.Verify(c => c.InvalidarProdutos(), Times.Once);
        }

        [Fact]
        public async Task RegistrarEntradaAsync_NaoDeveInvalidarCache_QuandoProdutoNaoEncontrado()
        {
            // Arrange
            ConfigurarProduto(null);

            // Act
            await _service.RegistrarEntradaAsync(produtoId: 1, quantidade: 5, registradoPor: "lucas.ody");

            // Assert: nada foi persistido, logo o cache não deve ser invalidado.
            _produtoCache.Verify(c => c.InvalidarProdutos(), Times.Never);
        }

        [Fact]
        public async Task RegistrarSaidaAsync_DeveRetornarProdutoNaoEncontrado()
        {
            //arrange
            ConfigurarProduto(null);

            //Act
            var resultado = await _service.RegistrarSaidaAsync(produtoId: 1, quantidade: 5, colaboradorId: 1, registradoPor: "lucas.ody");

            //Assert
            Assert.Equal(EstoqueResultStatus.ProdutoNaoEncontrado, resultado.Status);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task RegistrarSaidaAsync_DeveRetornarColaboradorNaoEncontrado()
        {
            //arrange
            ConfigurarProduto(new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 10 });

            ConfigurarColaborador(null);

            //Act
            var resultado = await _service.RegistrarSaidaAsync(produtoId: 1, quantidade: 5, colaboradorId: 9999, registradoPor: "lucas.ody");

            //Assert
            Assert.Equal(EstoqueResultStatus.ColaboradorNaoEncontrado, resultado.Status);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task RegistrarSaidaAsync_SaldoInsuficiente_NaoDeveAlterarNemPersistir()
        {
            //arrange
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 3 };
            ConfigurarProduto(produto);
            ConfigurarColaborador(new Colaborador { ColaboradorId = 1, Nome = "Fulano" });

            //Act
            var resultado = await _service.RegistrarSaidaAsync(produtoId: 1, quantidade: 5, colaboradorId: 1, registradoPor: "lucas.ody");

            //Assert
            Assert.Equal(EstoqueResultStatus.SaldoInsuficiente, resultado.Status);
            Assert.Equal(3, produto.SaldoAtual);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task RegistrarSaidaAsync_DeveDiminuirSaldoECriarMovimentacaoSaida()
        {
            var produto = new Produto{ ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 10 };
            ConfigurarProduto(produto);
            ConfigurarColaborador(new Colaborador { ColaboradorId = 7, Nome = "Fulano" });

            // Captura a Movimentacao passada ao Create para validar Tipo/ColaboradorId.
            Movimentacao? capturada = null;
            _movimentacaoRepo.Setup(r => r.Create(It.IsAny<Movimentacao>()))
                             .Callback<Movimentacao>(m => capturada = m)
                             .Returns((Movimentacao m) => m); // Retorna a mesma movimenta��o para evitar erros de null.

            //Act
            var resultado = await _service.RegistrarSaidaAsync(produtoId: 1, quantidade: 4, colaboradorId: 7, registradoPor: "lucas.ody");

            Assert.Equal(EstoqueResultStatus.Sucesso, resultado.Status);
            Assert.Equal(6,produto.SaldoAtual);
            Assert.NotNull(capturada);
            Assert.Equal('S', capturada!.Tipo);
            Assert.Equal(7, capturada.ColaboradorId);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task RegistrarAjusteAsync_DeveRetornarProdutoNaoEncontrado()
        {
            // Arrange
            ConfigurarProduto(null);

            // Act
            var resultado = await _service.RegistrarAjusteAsync(produtoId: 1, quantidade: 2, registradoPor: "lucas.ody");

            // Assert
            Assert.Equal(EstoqueResultStatus.ProdutoNaoEncontrado, resultado.Status);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task RegistrarAjusteAsync_SaldoInsuficiente_NaoDeveAlterarNemPersistir()
        {
            //arrange
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 2 };
            ConfigurarProduto(produto);

            //Act
            var resultado = await _service.RegistrarAjusteAsync(produtoId: 1, quantidade: 5, registradoPor: "lucas.ody");

            //Assert
            Assert.Equal(EstoqueResultStatus.SaldoInsuficiente, resultado.Status);
            Assert.Equal(2, produto.SaldoAtual);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task RegistrarAjusteAsync_DeveDiminuirSaldoECriarMovimentacaoSemColaborador()
        {
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 10 };
            ConfigurarProduto(produto);

            // Captura a Movimentacao passada ao Create para validar Tipo/ColaboradorId.
            Movimentacao? capturada = null;
            _movimentacaoRepo.Setup(r => r.Create(It.IsAny<Movimentacao>()))
                             .Callback<Movimentacao>(m => capturada = m)
                             .Returns((Movimentacao m) => m); // Retorna a mesma movimenta��o para evitar erros de null.

            //Act
            var resultado = await _service.RegistrarAjusteAsync(produtoId: 1, quantidade: 3, registradoPor: "lucas.ody");

            //Assert
            Assert.Equal(EstoqueResultStatus.Sucesso, resultado.Status);
            Assert.Equal(7, produto.SaldoAtual);
            Assert.NotNull(capturada);
            Assert.Equal('A', capturada!.Tipo);
            Assert.Null(capturada.ColaboradorId);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

    }
}
