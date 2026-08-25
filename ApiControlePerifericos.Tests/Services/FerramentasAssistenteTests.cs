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
        private readonly Mock<IMovimentacaoRepository> _movimentacoes = new();
        private readonly Mock<IUnitOfWork> _uof = new();
        private readonly FerramentasAssistente _sut;

        public FerramentasAssistenteTests()
        {
            _uof.Setup(u => u.ProdutoRepository).Returns(_produtos.Object);
            _uof.Setup(u => u.MovimentacaoRepository).Returns(_movimentacoes.Object);

            _sut = new FerramentasAssistente(_uof.Object);
        }

        // Admin por padrão: os testes de produto não se importam com a role, e os que se
        // importam passam o valor explicitamente.
        private FerramentaAssistente Ferramenta(string nome, bool ehAdmin = true) =>
            _sut.Obter(ehAdmin).Single(ferramenta => ferramenta.Nome == nome);

        private static async Task<JsonElement> ExecutarAsync(
            FerramentaAssistente ferramenta, Dictionary<string, string>? argumentos = null)
        {
            var json = await ferramenta.Executar(argumentos ?? [], CancellationToken.None);

            return JsonSerializer.Deserialize<JsonElement>(json);
        }

        private static Produto Produto(int id, string descricao, int saldo, int minimo) =>
            new() { ProdutoId = id, Descricao = descricao, SaldoAtual = saldo, EstoqueMinimo = minimo };

        private static Movimentacao Movimentacao(char tipo, int quantidade, string produto,
                                                 string? colaborador = null,
                                                 string? registradoPor = "lucas.ody") =>
            new()
            {
                Tipo = tipo,
                Quantidade = quantidade,
                DataMovimentacao = new DateTime(2026, 7, 15),
                RegistradoPor = registradoPor,
                Produto = new Produto { Descricao = produto },
                Colaborador = colaborador is null ? null : new Colaborador { Nome = colaborador }
            };

        private void ConfigurarRelatorio(List<Movimentacao> movimentacoes,
                                         int pageNumber = 1, int pageSize = 50) =>
            _movimentacoes.Setup(r => r.GetRelatorioAsync(It.IsAny<MovimentacoesParameters>()))
                          .ReturnsAsync(movimentacoes.ToPagedList(pageNumber, pageSize));

        [Fact]
        public void Obter_DeveDevolverSempreAMesmaListaNaMesmaOrdem()
        {
            // O prefixo do prompt (ferramentas + instruções + manual) é cacheado como um
            // bloco só: lista instável aqui derruba o cache do manual junto.
            var primeira = _sut.Obter(ehAdmin: true).Select(ferramenta => ferramenta.Nome);
            var segunda = _sut.Obter(ehAdmin: true).Select(ferramenta => ferramenta.Nome);

            Assert.Equal(primeira, segunda);
            Assert.Equal(["consultar_produto", "listar_produtos_abaixo_minimo", "consultar_movimentacoes"],
                         primeira);
        }

        [Fact]
        public void Obter_ParaNaoAdmin_NaoDeveTrazerAsMovimentacoes()
        {
            var nomes = _sut.Obter(ehAdmin: false).Select(ferramenta => ferramenta.Nome).ToList();

            // O MovimentacoesController inteiro é AdminOnly: o assistente não pode ser um
            // caminho alternativo para o mesmo dado.
            Assert.DoesNotContain("consultar_movimentacoes", nomes);
            Assert.Equal(["consultar_produto", "listar_produtos_abaixo_minimo"], nomes);
        }

        [Fact]
        public void Obter_ListaDeNaoAdmin_DeveSerPrefixoDaDeAdmin()
        {
            var naoAdmin = _sut.Obter(ehAdmin: false).Select(ferramenta => ferramenta.Nome).ToList();
            var admin = _sut.Obter(ehAdmin: true).Select(ferramenta => ferramenta.Nome).ToList();

            // A ferramenta de admin só ACRESCENTA ao fim. Se ela entrasse no meio, os dois
            // prefixos de cache divergiriam já na primeira ferramenta.
            Assert.Equal(naoAdmin, admin.Take(naoAdmin.Count));
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

        [Fact]
        public async Task ConsultarMovimentacoes_DeveDevolverTipoQuantidadeDataProdutoEColaborador()
        {
            ConfigurarRelatorio([Movimentacao('S', 4, "Pilha AA", colaborador: "Lucas Ody")]);

            var resultado = await ExecutarAsync(Ferramenta("consultar_movimentacoes"),
                                                new() { ["descricaoProduto"] = "pilha" });

            var movimentacao = resultado.GetProperty("movimentacoes")[0];
            // Tipo por extenso: o modelo não precisa decifrar 'S'.
            Assert.Equal("Saída", movimentacao.GetProperty("tipo").GetString());
            Assert.Equal(4, movimentacao.GetProperty("quantidade").GetInt32());
            Assert.Equal("2026-07-15", movimentacao.GetProperty("data").GetString());
            Assert.Equal("Pilha AA", movimentacao.GetProperty("produto").GetString());
            Assert.Equal("Lucas Ody", movimentacao.GetProperty("colaborador").GetString());
            Assert.Equal("lucas.ody", movimentacao.GetProperty("registradoPor").GetString());
        }

        [Fact]
        public async Task ConsultarMovimentacoes_DeveRepassarOsFiltrosELimitarAPagina()
        {
            MovimentacoesParameters? enviado = null;
            _movimentacoes.Setup(r => r.GetRelatorioAsync(It.IsAny<MovimentacoesParameters>()))
                          .Callback<MovimentacoesParameters>(parametros => enviado = parametros)
                          .ReturnsAsync(new List<Movimentacao>().ToPagedList(1, 50));

            await ExecutarAsync(Ferramenta("consultar_movimentacoes"), new()
            {
                ["descricaoProduto"] = "pilha",
                ["nomeColaborador"] = "Lucas",
                ["dataInicio"] = "2026-07-01",
                ["dataFim"] = "2026-07-31"
            });

            Assert.Equal("pilha", enviado!.DescricaoProduto);
            Assert.Equal("Lucas", enviado.NomeColaborador);
            Assert.Equal(new DateTime(2026, 7, 1), enviado.DataInicio);
            Assert.Equal(new DateTime(2026, 7, 31), enviado.DataFim);
            Assert.Equal(50, enviado.PageSize);
        }

        [Fact]
        public async Task ConsultarMovimentacoes_SemFiltro_DeveMandarTudoNulo()
        {
            MovimentacoesParameters? enviado = null;
            _movimentacoes.Setup(r => r.GetRelatorioAsync(It.IsAny<MovimentacoesParameters>()))
                          .Callback<MovimentacoesParameters>(parametros => enviado = parametros)
                          .ReturnsAsync(new List<Movimentacao>().ToPagedList(1, 50));

            await ExecutarAsync(Ferramenta("consultar_movimentacoes"));

            // Todos os filtros são opcionais: string vazia viraria Contains("") e não filtro
            // nenhum, então o que desce é null mesmo.
            Assert.Null(enviado!.DescricaoProduto);
            Assert.Null(enviado.NomeColaborador);
            Assert.Null(enviado.DataInicio);
            Assert.Null(enviado.DataFim);
        }

        [Fact]
        public async Task ConsultarMovimentacoes_QuandoHaMaisQueAPagina_DeveAvisarNoEncontrados()
        {
            ConfigurarRelatorio(
            [
                Movimentacao('S', 2, "Pilha AA", "Lucas Ody"),
                Movimentacao('S', 3, "Pilha AA", "Lucas Ody"),
                Movimentacao('S', 5, "Pilha AA", "Lucas Ody")
            ], pageNumber: 1, pageSize: 1);

            var resultado = await ExecutarAsync(Ferramenta("consultar_movimentacoes"));

            // É este par que impede o modelo de somar uma página e apresentar o número como
            // se fosse o total do período.
            Assert.Equal(3, resultado.GetProperty("encontrados").GetInt32());
            Assert.Equal(1, resultado.GetProperty("exibidos").GetInt32());
        }

        [Theory]
        [InlineData("dataInicio", "15/07/2026")]
        [InlineData("dataInicio", "julho")]
        [InlineData("dataFim", "2026-13-01")]
        public async Task ConsultarMovimentacoes_ComDataInvalida_DeveDevolverErroSemConsultarOBanco(
            string parametro, string valor)
        {
            var resultado = await ExecutarAsync(Ferramenta("consultar_movimentacoes"),
                                                new() { [parametro] = valor });

            // Data mal escrita vira erro legível: consultar assim devolveria o período
            // errado, ou vazio, com cara de resposta certa.
            Assert.True(resultado.TryGetProperty("erro", out _));
            _movimentacoes.Verify(r => r.GetRelatorioAsync(It.IsAny<MovimentacoesParameters>()), Times.Never);
        }

        [Fact]
        public async Task ConsultarMovimentacoes_ComIntervaloInvertido_DeveDevolverErro()
        {
            var resultado = await ExecutarAsync(Ferramenta("consultar_movimentacoes"), new()
            {
                ["dataInicio"] = "2026-07-31",
                ["dataFim"] = "2026-07-01"
            });

            Assert.True(resultado.TryGetProperty("erro", out _));
            _movimentacoes.Verify(r => r.GetRelatorioAsync(It.IsAny<MovimentacoesParameters>()), Times.Never);
        }

        [Fact]
        public async Task ConsultarMovimentacoes_ComApenasUmaData_NaoDeveCairNaValidacaoDeIntervalo()
        {
            ConfigurarRelatorio([Movimentacao('E', 100, "Pilha AA")]);

            var resultado = await ExecutarAsync(Ferramenta("consultar_movimentacoes"),
                                                new() { ["dataInicio"] = "2026-07-01" });

            // Só uma ponta do intervalo é filtro válido: comparar DateTime? com null dá
            // false, e isso não pode virar erro por acidente.
            Assert.False(resultado.TryGetProperty("erro", out _));
            Assert.Equal(1, resultado.GetProperty("encontrados").GetInt32());
        }

        [Fact]
        public async Task ConsultarMovimentacoes_EmEntrada_DeveDevolverColaboradorNulo()
        {
            ConfigurarRelatorio([Movimentacao('E', 100, "Pilha AA")]);

            var resultado = await ExecutarAsync(Ferramenta("consultar_movimentacoes"));

            var movimentacao = resultado.GetProperty("movimentacoes")[0];
            Assert.Equal("Entrada", movimentacao.GetProperty("tipo").GetString());
            // Entrada e ajuste não têm quem retirou.
            Assert.Equal(JsonValueKind.Null, movimentacao.GetProperty("colaborador").ValueKind);
        }

        [Fact]
        public async Task ConsultarMovimentacoes_EmAjuste_DeveDescreverOTipo()
        {
            ConfigurarRelatorio([Movimentacao('A', 3, "Pilha AA")]);

            var resultado = await ExecutarAsync(Ferramenta("consultar_movimentacoes"));

            // Mesma palavra que o TipoDescricao do MappingProfile mostra na tela.
            Assert.Equal("Ajuste", resultado.GetProperty("movimentacoes")[0].GetProperty("tipo").GetString());
        }
    }
}
