using System.Text.Encodings.Web;
using System.Text.Json;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;

namespace ApiControlePerifericos.Services
{
    // As consultas que o assistente pode fazer ao estoque. Todas somente leitura: nenhuma
    // grava movimentação nem mexe em saldo — para isso existe o EstoqueService, chamado
    // pelo controller, com o RegistradoPor vindo do JWT.
    public class FerramentasAssistente : IFerramentasAssistente
    {
        // Teto do que uma consulta devolve. Cada produto listado vira token na requisição
        // seguinte, então a busca larga ("mouse") é cortada aqui em vez de arrastar meio
        // catálogo para dentro do prompt.
        private const int LimiteProdutos = 20;

        // O resultado vai para a API da Anthropic, não para uma página HTML: escapar acento
        // em \uXXXX só gastaria token e deixaria o log ilegível.
        private static readonly JsonSerializerOptions OpcoesJson = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly IUnitOfWork _uof;

        public FerramentasAssistente(IUnitOfWork uof)
        {
            _uof = uof;
        }

        // Lista literal, sempre na mesma ordem: é isso que mantém o prefixo do prompt
        // estável e o manual em cache entre as perguntas.
        public IReadOnlyList<FerramentaAssistente> Obter() =>
        [
            new FerramentaAssistente(
                "consultar_produto",
                "Consulta produtos do estoque pela descrição e devolve saldo atual, estoque " +
                "mínimo e se o produto está abaixo do mínimo. Use quando a pergunta for sobre " +
                "um produto específico.",
                [
                    new ParametroFerramenta(
                        "descricao",
                        TipoParametroFerramenta.Texto,
                        "Trecho da descrição do produto, por exemplo 'mouse' ou 'teclado sem fio'.")
                ],
                ConsultarProdutoAsync),

            new FerramentaAssistente(
                "listar_produtos_abaixo_minimo",
                "Lista os produtos cujo saldo atual está abaixo do estoque mínimo. Use quando " +
                "a pergunta for sobre reposição ou sobre o que está faltando no estoque.",
                [],
                ListarProdutosAbaixoMinimoAsync)
        ];

        // O CancellationToken chega até aqui mas não tem para onde ir: os métodos do
        // repositório ainda não recebem token. Fica no delegate porque quem cancela é o
        // loop de ferramentas, entre uma consulta e outra.
        private async Task<string> ConsultarProdutoAsync(
            IReadOnlyDictionary<string, string> argumentos, CancellationToken cancellationToken)
        {
            // O modelo pode chamar a ferramenta sem o parâmetro. Isso não é exceção: devolver
            // o motivo como resultado deixa ele corrigir a chamada na rodada seguinte.
            if (!argumentos.TryGetValue("descricao", out var descricao) || string.IsNullOrWhiteSpace(descricao))
                return Serializar(new { erro = "Informe a descrição do produto a consultar." });

            var parametros = new ProdutosParameters { Descricao = descricao, PageSize = LimiteProdutos };
            var produtos = await _uof.ProdutoRepository.GetProdutosAsync(parametros);

            return Serializar(new
            {
                encontrados = produtos.TotalItemCount,
                exibidos = produtos.Count,
                produtos = produtos.Select(Resumir)
            });
        }

        private async Task<string> ListarProdutosAbaixoMinimoAsync(
            IReadOnlyDictionary<string, string> argumentos, CancellationToken cancellationToken)
        {
            var produtos = (await _uof.ProdutoRepository.GetAbaixoEstoqueMinimoAsync()).ToList();

            return Serializar(new
            {
                total = produtos.Count,
                exibidos = Math.Min(produtos.Count, LimiteProdutos),
                produtos = produtos.Take(LimiteProdutos).Select(Resumir)
            });
        }

        // Formato único para as duas ferramentas: o modelo aprende um formato só, e recebe
        // o "abaixoDoMinimo" pronto em vez de precisar comparar os números por conta.
        private static object Resumir(Produto produto) => new
        {
            id = produto.ProdutoId,
            descricao = produto.Descricao,
            saldoAtual = produto.SaldoAtual,
            estoqueMinimo = produto.EstoqueMinimo,
            abaixoDoMinimo = produto.AbaixoDoMinimo
        };

        // JSON em vez de frase pronta: o modelo lê estrutura melhor que prosa, e a
        // formatação da resposta continua sendo decisão dele, não regra escondida aqui.
        private static string Serializar(object valor) => JsonSerializer.Serialize(valor, OpcoesJson);
    }
}