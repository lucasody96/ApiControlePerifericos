using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using ApiControlePerifericos.Services;
using Moq;
using System.Text.Json;
using X.PagedList.Extensions;

namespace ApiControlePerifericos.Tests.Services
{
    // As ferramentas do assistente são consultas comuns ao repositório: dá para testar o que
    // elas devolvem sem envolver a IA, que é a parte cara e não determinística. O que se
    // verifica aqui é o contrato do resultado — é ele que o modelo vai ler.
    public class FerramentasAssistenteTests
    {
        private readonly Mock<IProdutoRepository> _produtos = new();
        private readonly Mock<IUnitOfWork> _uof = new();
        private readonly FerramentasAssistente _sut;

        public FerramentasAssistenteTests()
        {
            _uof.Setup(u => u.ProdutoRepository).Returns(_produtos.Object);

            _sut = new FerramentasAssistente(_uof.Object);
        }

        private FerramentaAssistente Ferramenta(string nome) =>
            _sut.Obter().Single(ferramenta => ferramenta.Nome == nome);

        private static async Task<JsonElement> ExecutarAsync(
            FerramentaAssistente ferramenta, Dictionary<string, string>? argumentos = null)
        {
            var json = await ferramenta.Executar(argumentos ?? [], CancellationToken.None);

            return JsonSerializer.Deserialize<JsonElement>(json);
        }

        private static Produto Produto(int id, string descricao, int saldo, int minimo) =>
            new() { ProdutoId = id, Descricao = descricao, SaldoAtual = saldo, EstoqueMinimo = minimo };

        [Fact]
        public void Obter_DeveDevolverSempreAMesmaListaNaMesmaOrdem()
        {
            // O prefixo do prompt (ferramentas + instruções + manual) é cacheado como um
            // bloco só: lista instável aqui derruba o cache do manual junto.
            var primeira = _sut.Obter().Select(ferramenta => ferramenta.Nome);
            var segunda = _sut.Obter().Select(ferramenta => ferramenta.Nome);

            Assert.Equal(primeira, segunda);
            Assert.Equal(["consultar_produto", "listar_produtos_abaixo_minimo"], primeira);
        }

        [Fact]
        public async Task ConsultarProduto_DeveDevolverSaldoEstoqueMinimoESituacao()
        {
            _produtos.Setup(r => r.GetProdutosAsync(It.IsAny<ProdutosParameters>()))
                     .ReturnsAsync(new List<Produto> { Produto(7, "Mouse USB", saldo: 2, minimo: 5) }
                                   .ToPagedList(1, 20));

            var resultado = await ExecutarAsync(Ferramenta("consultar_produto"),
                                                new() { ["descricao"] = "mouse" });

            var produto = resultado.GetProperty("produtos")[0];
            Assert.Equal(7, produto.GetProperty("id").GetInt32());
            Assert.Equal("Mouse USB", produto.GetProperty("descricao").GetString());
            Assert.Equal(2, produto.GetProperty("saldoAtual").GetInt32());
            Assert.Equal(5, produto.GetProperty("estoqueMinimo").GetInt32());
            // O modelo recebe a situação pronta, em vez de precisar comparar os números.
            Assert.True(produto.GetProperty("abaixoDoMinimo").GetBoolean());
        }

        [Fact]
        public async Task ConsultarProduto_ComSaldoIgualAoMinimo_NaoDeveMarcarComoAbaixo()
        {
            _produtos.Setup(r => r.GetProdutosAsync(It.IsAny<ProdutosParameters>()))
                     .ReturnsAsync(new List<Produto> { Produto(1, "Teclado", saldo: 5, minimo: 5) }
                                   .ToPagedList(1, 20));

            var resultado = await ExecutarAsync(Ferramenta("consultar_produto"),
                                                new() { ["descricao"] = "teclado" });

            // Saldo igual ao mínimo não está abaixo: a regra é menor que, não menor ou igual.
            Assert.False(resultado.GetProperty("produtos")[0].GetProperty("abaixoDoMinimo").GetBoolean());
        }

        [Fact]
        public async Task ConsultarProduto_DeveRepassarADescricaoELimitarAPagina()
        {
            ProdutosParameters? enviado = null;
            _produtos.Setup(r => r.GetProdutosAsync(It.IsAny<ProdutosParameters>()))
                     .Callback<ProdutosParameters>(parametros => enviado = parametros)
                     .ReturnsAsync(new List<Produto>().ToPagedList(1, 20));

            await ExecutarAsync(Ferramenta("consultar_produto"), new() { ["descricao"] = "mouse" });

            Assert.Equal("mouse", enviado!.Descricao);
            // O teto existe para a busca larga não arrastar o catálogo para dentro do prompt.
            Assert.Equal(20, enviado.PageSize);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ConsultarProduto_SemDescricao_DeveDevolverErroSemConsultarOBanco(string? descricao)
        {
            var argumentos = descricao is null
                ? []
                : new Dictionary<string, string> { ["descricao"] = descricao };

            var resultado = await ExecutarAsync(Ferramenta("consultar_produto"), argumentos);

            // O erro volta como resultado da consulta: o modelo lê e corrige a chamada.
            Assert.True(resultado.TryGetProperty("erro", out _));
            _produtos.Verify(r => r.GetProdutosAsync(It.IsAny<ProdutosParameters>()), Times.Never);
        }

        [Fact]
        public async Task ConsultarProduto_QuandoNaoEncontra_DeveDevolverListaVazia()
        {
            _produtos.Setup(r => r.GetProdutosAsync(It.IsAny<ProdutosParameters>()))
                     .ReturnsAsync(new List<Produto>().ToPagedList(1, 20));

            var resultado = await ExecutarAsync(Ferramenta("consultar_produto"),
                                                new() { ["descricao"] = "impressora 3d" });

            Assert.Equal(0, resultado.GetProperty("encontrados").GetInt32());
            Assert.Empty(resultado.GetProperty("produtos").EnumerateArray());
        }

        [Fact]
        public async Task ListarProdutosAbaixoMinimo_DeveUsarAConsultaDoRepositorio()
        {
            _produtos.Setup(r => r.GetAbaixoEstoqueMinimoAsync())
                     .ReturnsAsync(new List<Produto>
                     {
                         Produto(1, "Mouse USB", saldo: 2, minimo: 5),
                         Produto(2, "Teclado ABNT2", saldo: 1, minimo: 4)
                     });

            var resultado = await ExecutarAsync(Ferramenta("listar_produtos_abaixo_minimo"));

            Assert.Equal(2, resultado.GetProperty("total").GetInt32());
            Assert.Equal(2, resultado.GetProperty("produtos").GetArrayLength());
            // A regra de abaixo do mínimo vem do repositório, não é recalculada aqui.
            _produtos.Verify(r => r.GetAbaixoEstoqueMinimoAsync(), Times.Once);
        }

        [Fact]
        public async Task ListarProdutosAbaixoMinimo_SemProdutos_DeveDevolverTotalZero()
        {
            _produtos.Setup(r => r.GetAbaixoEstoqueMinimoAsync())
                     .ReturnsAsync(new List<Produto>());

            var resultado = await ExecutarAsync(Ferramenta("listar_produtos_abaixo_minimo"));

            Assert.Equal(0, resultado.GetProperty("total").GetInt32());
            Assert.Empty(resultado.GetProperty("produtos").EnumerateArray());
        }
    }
}
