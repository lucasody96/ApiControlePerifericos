using ApiControlePerifericos.DTOs;
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

        // A alteração/exclusão de histórico busca a movimentação rastreada pela PK.
        private void ConfigurarMovimentacao(Movimentacao? movimentacao, int movimentacaoId = 1) =>
            _movimentacaoRepo.Setup(r => r.GetByIdTrackedAsync(movimentacaoId)).ReturnsAsync(movimentacao);

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

        // ---------------------- AtualizarMovimentacaoAsync ----------------------

        [Fact]
        public async Task AtualizarMovimentacaoAsync_QuandoMovimentacaoNaoExiste_DeveRetornarNaoEncontrada()
        {
            // Arrange
            ConfigurarMovimentacao(null);

            // Act
            var resultado = await _service.AtualizarMovimentacaoAsync(1,
                new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 });

            // Assert
            Assert.Equal(EstoqueResultStatus.MovimentacaoNaoEncontrada, resultado.Status);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
            _produtoCache.Verify(c => c.InvalidarProdutos(), Times.Never);
        }

        [Fact]
        public async Task AtualizarMovimentacaoAsync_QuandoTipoInvalido_DeveRetornarTipoInvalido()
        {
            // Arrange
            ConfigurarMovimentacao(new Movimentacao { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 });

            // Act ('X' não é 'E'/'S'/'A' — sem tipo válido não dá para saber o efeito no saldo)
            var resultado = await _service.AtualizarMovimentacaoAsync(1,
                new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'X', Quantidade = 5, ProdutoId = 1 });

            // Assert
            Assert.Equal(EstoqueResultStatus.TipoInvalido, resultado.Status);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task AtualizarMovimentacaoAsync_SaidaSemColaborador_DeveRetornarColaboradorObrigatorio()
        {
            // Arrange
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 20 };
            ConfigurarMovimentacao(new Movimentacao { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 });
            ConfigurarProduto(produto);

            // Act
            var resultado = await _service.AtualizarMovimentacaoAsync(1,
                new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'S', Quantidade = 5, ProdutoId = 1, ColaboradorId = null });

            // Assert
            Assert.Equal(EstoqueResultStatus.ColaboradorObrigatorio, resultado.Status);
            Assert.Equal(20, produto.SaldoAtual);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task AtualizarMovimentacaoAsync_QuandoColaboradorNaoExiste_DeveRetornarColaboradorNaoEncontrado()
        {
            // Arrange
            ConfigurarMovimentacao(new Movimentacao { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 });
            ConfigurarProduto(new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 20 });
            ConfigurarColaborador(null);

            // Act
            var resultado = await _service.AtualizarMovimentacaoAsync(1,
                new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'S', Quantidade = 5, ProdutoId = 1, ColaboradorId = 9999 });

            // Assert
            Assert.Equal(EstoqueResultStatus.ColaboradorNaoEncontrado, resultado.Status);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task AtualizarMovimentacaoAsync_MesmoProduto_DeveEstornarAntigaEAplicarNova()
        {
            // Arrange: saldo 20 já contempla a entrada de 5.
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 20 };
            var movimentacao = new Movimentacao { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };
            ConfigurarMovimentacao(movimentacao);
            ConfigurarProduto(produto);

            // Act: a entrada de 5 vira entrada de 8.
            var resultado = await _service.AtualizarMovimentacaoAsync(1,
                new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'E', Quantidade = 8, ProdutoId = 1 });

            // Assert: 20 - 5 + 8 = 23.
            Assert.Equal(EstoqueResultStatus.Sucesso, resultado.Status);
            Assert.Equal(23, produto.SaldoAtual);
            Assert.Equal(8, movimentacao.Quantidade);

            // A movimentação está rastreada: alterar as propriedades basta, sem Update.
            _movimentacaoRepo.Verify(r => r.Update(It.IsAny<Movimentacao>()), Times.Never);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
            _produtoCache.Verify(c => c.InvalidarProdutos(), Times.Once);
        }

        [Fact]
        public async Task AtualizarMovimentacaoAsync_TrocaDeTipo_DeveRecalcularSaldo()
        {
            // Arrange: saldo 20 contempla a entrada de 5.
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 20 };
            var movimentacao = new Movimentacao { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };
            ConfigurarMovimentacao(movimentacao);
            ConfigurarProduto(produto);

            // Act: era entrada de 5, na verdade era ajuste (perda) de 5.
            var resultado = await _service.AtualizarMovimentacaoAsync(1,
                new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'A', Quantidade = 5, ProdutoId = 1 });

            // Assert: 20 - 5 (estorno) - 5 (ajuste) = 10.
            Assert.Equal(EstoqueResultStatus.Sucesso, resultado.Status);
            Assert.Equal(10, produto.SaldoAtual);
            Assert.Equal('A', movimentacao.Tipo);
        }

        [Fact]
        public async Task AtualizarMovimentacaoAsync_EstornoIntermediarioNegativo_MasFinalValido_DeveTerSucesso()
        {
            // Arrange: saldo 5, movimentação antiga era entrada de 10.
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 5 };
            ConfigurarMovimentacao(new Movimentacao { MovimentacaoId = 1, Tipo = 'E', Quantidade = 10, ProdutoId = 1 });
            ConfigurarProduto(produto);

            // Act: vira entrada de 20. O estorno sozinho levaria a -5, mas o final é positivo.
            var resultado = await _service.AtualizarMovimentacaoAsync(1,
                new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'E', Quantidade = 20, ProdutoId = 1 });

            // Assert: 5 - 10 + 20 = 15. Só o saldo FINAL é validado.
            Assert.Equal(EstoqueResultStatus.Sucesso, resultado.Status);
            Assert.Equal(15, produto.SaldoAtual);
        }

        [Fact]
        public async Task AtualizarMovimentacaoAsync_SaldoFinalNegativo_NaoDeveAlterarNemPersistir()
        {
            // Arrange: saldo 2, movimentação antiga era entrada de 10.
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 2 };
            var movimentacao = new Movimentacao { MovimentacaoId = 1, Tipo = 'E', Quantidade = 10, ProdutoId = 1 };
            ConfigurarMovimentacao(movimentacao);
            ConfigurarProduto(produto);

            // Act: 2 - 10 + 3 = -5.
            var resultado = await _service.AtualizarMovimentacaoAsync(1,
                new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'E', Quantidade = 3, ProdutoId = 1 });

            // Assert: nada foi tocado — nem o saldo, nem a movimentação.
            Assert.Equal(EstoqueResultStatus.SaldoNegativoAposEstorno, resultado.Status);
            Assert.Equal(2, produto.SaldoAtual);
            Assert.Equal(10, movimentacao.Quantidade);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
            _produtoCache.Verify(c => c.InvalidarProdutos(), Times.Never);
        }

        [Fact]
        public async Task AtualizarMovimentacaoAsync_TrocaDeProduto_DeveEstornarNoAntigoEAplicarNoNovo()
        {
            // Arrange: a entrada de 5 foi lançada no produto errado.
            var produtoAntigo = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 20 };
            var produtoNovo = new Produto { ProdutoId = 2, Descricao = "Teclado", SaldoAtual = 3 };
            ConfigurarMovimentacao(new Movimentacao { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 });
            ConfigurarProduto(produtoAntigo, produtoId: 1);
            ConfigurarProduto(produtoNovo, produtoId: 2);

            // Act
            var resultado = await _service.AtualizarMovimentacaoAsync(1,
                new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 2 });

            // Assert: estorno no antigo (20 - 5) e lançamento no novo (3 + 5).
            Assert.Equal(EstoqueResultStatus.Sucesso, resultado.Status);
            Assert.Equal(15, produtoAntigo.SaldoAtual);
            Assert.Equal(8, produtoNovo.SaldoAtual);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Fact]
        public async Task AtualizarMovimentacaoAsync_QuandoNaoEhSaida_DeveDescartarColaborador()
        {
            // Arrange: saída de 5 para o colaborador 7 (saldo 15 já reflete a saída).
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 15 };
            var movimentacao = new Movimentacao { MovimentacaoId = 1, Tipo = 'S', Quantidade = 5, ProdutoId = 1, ColaboradorId = 7 };
            ConfigurarMovimentacao(movimentacao);
            ConfigurarProduto(produto);

            // Act: na verdade era um ajuste (perda), que não tem colaborador.
            var resultado = await _service.AtualizarMovimentacaoAsync(1,
                new MovimentacaoDTO { MovimentacaoId = 1, Tipo = 'A', Quantidade = 5, ProdutoId = 1, ColaboradorId = 7 });

            // Assert: 15 + 5 (estorno) - 5 (ajuste) = 15, e o vínculo com o colaborador cai.
            Assert.Equal(EstoqueResultStatus.Sucesso, resultado.Status);
            Assert.Equal(15, produto.SaldoAtual);
            Assert.Null(movimentacao.ColaboradorId);
        }

        // ---------------------- ExcluirMovimentacaoAsync ----------------------

        [Fact]
        public async Task ExcluirMovimentacaoAsync_QuandoMovimentacaoNaoExiste_DeveRetornarNaoEncontrada()
        {
            // Arrange
            ConfigurarMovimentacao(null);

            // Act
            var resultado = await _service.ExcluirMovimentacaoAsync(1);

            // Assert
            Assert.Equal(EstoqueResultStatus.MovimentacaoNaoEncontrada, resultado.Status);
            _movimentacaoRepo.Verify(r => r.Delete(It.IsAny<Movimentacao>()), Times.Never);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task ExcluirMovimentacaoAsync_QuandoProdutoNaoExiste_DeveRetornarProdutoNaoEncontrado()
        {
            // Arrange
            ConfigurarMovimentacao(new Movimentacao { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 99 });
            ConfigurarProduto(null, produtoId: 99);

            // Act
            var resultado = await _service.ExcluirMovimentacaoAsync(1);

            // Assert
            Assert.Equal(EstoqueResultStatus.ProdutoNaoEncontrado, resultado.Status);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task ExcluirMovimentacaoAsync_EntradaDeveSubtrairDoSaldo()
        {
            // Arrange: saldo 20 contempla a entrada de 5 que será excluída.
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 20 };
            var movimentacao = new Movimentacao { MovimentacaoId = 1, Tipo = 'E', Quantidade = 5, ProdutoId = 1 };
            ConfigurarMovimentacao(movimentacao);
            ConfigurarProduto(produto);

            // Act
            var resultado = await _service.ExcluirMovimentacaoAsync(1);

            // Assert
            Assert.Equal(EstoqueResultStatus.Sucesso, resultado.Status);
            Assert.Equal(15, produto.SaldoAtual);
            _movimentacaoRepo.Verify(r => r.Delete(movimentacao), Times.Once);
            _uow.Verify(u => u.CommitAsync(), Times.Once);
            _produtoCache.Verify(c => c.InvalidarProdutos(), Times.Once);
        }

        [Fact]
        public async Task ExcluirMovimentacaoAsync_SaidaDeveDevolverAoSaldo()
        {
            // Arrange: saldo 15 já reflete a saída de 5 que será excluída.
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 15 };
            ConfigurarMovimentacao(new Movimentacao { MovimentacaoId = 1, Tipo = 'S', Quantidade = 5, ProdutoId = 1, ColaboradorId = 7 });
            ConfigurarProduto(produto);

            // Act
            var resultado = await _service.ExcluirMovimentacaoAsync(1);

            // Assert
            Assert.Equal(EstoqueResultStatus.Sucesso, resultado.Status);
            Assert.Equal(20, produto.SaldoAtual);
        }

        [Fact]
        public async Task ExcluirMovimentacaoAsync_QuandoEstornoDeixariaSaldoNegativo_NaoDeveAlterarNemPersistir()
        {
            // Arrange: saldo 3, mas a entrada a excluir era de 10 (as demais saídas já
            // consumiram o estoque) — estornar levaria o saldo a -7.
            var produto = new Produto { ProdutoId = 1, Descricao = "Mouse", SaldoAtual = 3 };
            ConfigurarMovimentacao(new Movimentacao { MovimentacaoId = 1, Tipo = 'E', Quantidade = 10, ProdutoId = 1 });
            ConfigurarProduto(produto);

            // Act
            var resultado = await _service.ExcluirMovimentacaoAsync(1);

            // Assert
            Assert.Equal(EstoqueResultStatus.SaldoNegativoAposEstorno, resultado.Status);
            Assert.Equal(3, produto.SaldoAtual);
            _movimentacaoRepo.Verify(r => r.Delete(It.IsAny<Movimentacao>()), Times.Never);
            _uow.Verify(u => u.CommitAsync(), Times.Never);
            _produtoCache.Verify(c => c.InvalidarProdutos(), Times.Never);
        }
    }
}
