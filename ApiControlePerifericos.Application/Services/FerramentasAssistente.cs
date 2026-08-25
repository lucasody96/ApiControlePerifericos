using System.Globalization;
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

        // Movimentação é linha mais curta que produto e a pergunta costuma ser quantitativa
        // ("quantas pilhas em julho"), então o teto é o máximo que o QueryStringParameters
        // permite: cortar cedo demais faria o modelo somar página em vez de período.
        private const int LimiteMovimentacoes = 50;

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
        // estável e o manual em cache entre as perguntas. A ferramenta de movimentação
        // entra no FIM, para a lista de não-admin continuar sendo prefixo da de admin.
        public IReadOnlyList<FerramentaAssistente> Obter(bool ehAdmin)
        {
            List<FerramentaAssistente> ferramentas =
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

            if (ehAdmin)
            {
                ferramentas.Add(new FerramentaAssistente(
                    "consultar_movimentacoes",
                    "Consulta o histórico do estoque: entradas, saídas (retiradas por um " +
                    "colaborador) e ajustes de perda ou quebra. Todos os filtros são opcionais e " +
                    "se combinam. Use para perguntas sobre o que aconteceu com um produto, sobre " +
                    "o que um colaborador retirou, ou sobre um período. Devolve as mais recentes " +
                    "primeiro, com 'encontrados' (quantas existem no filtro) e 'exibidos' " +
                    "(quantas vieram). Se 'encontrados' for maior que 'exibidos', NÃO some as " +
                    "quantidades: diga que o filtro tem mais registros do que os listados e peça " +
                    "um período ou produto mais específico.",
                    [
                        new ParametroFerramenta(
                            "descricaoProduto",
                            TipoParametroFerramenta.Texto,
                            "Trecho da descrição do produto, por exemplo 'pilha'.",
                            Obrigatorio: false),

                        new ParametroFerramenta(
                            "nomeColaborador",
                            TipoParametroFerramenta.Texto,
                            "Trecho do nome de quem retirou, por exemplo 'Lucas'. Encontra apenas " +
                            "saídas: entrada e ajuste não têm colaborador.",
                            Obrigatorio: false),

                        new ParametroFerramenta(
                            "dataInicio",
                            TipoParametroFerramenta.Texto,
                            "Início do período, no formato AAAA-MM-DD.",
                            Obrigatorio: false),

                        new ParametroFerramenta(
                            "dataFim",
                            TipoParametroFerramenta.Texto,
                            "Fim do período, inclusivo, no formato AAAA-MM-DD.",
                            Obrigatorio: false)
                    ],
                    ConsultarMovimentacoesAsync));
            }

            return ferramentas;
        }

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

        // GetRelatorioAsync, e não GetMovimentacoesAsync, porque só ele faz o Include de
        // Produto e Colaborador. O outro devolveria ids nus e o modelo gastaria uma rodada
        // a mais só para descobrir de que produto se trata.
        private async Task<string> ConsultarMovimentacoesAsync(
            IReadOnlyDictionary<string, string> argumentos, CancellationToken cancellationToken)
        {
            if (!TentarLerData(argumentos, "dataInicio", out var dataInicio))
                return Serializar(new { erro = "dataInicio inválida. Use o formato AAAA-MM-DD." });

            if (!TentarLerData(argumentos, "dataFim", out var dataFim))
                return Serializar(new { erro = "dataFim inválida. Use o formato AAAA-MM-DD." });

            // Intervalo invertido devolveria lista vazia em silêncio, e o modelo leria isso
            // como "não houve movimentação". Mesma validação do MovimentacoesController.
            if (dataInicio > dataFim)
                return Serializar(new { erro = "dataInicio não pode ser maior que dataFim." });

            var parametros = new MovimentacoesParameters
            {
                PageSize = LimiteMovimentacoes,
                DataInicio = dataInicio,
                DataFim = dataFim,
                DescricaoProduto = LerTexto(argumentos, "descricaoProduto"),
                NomeColaborador = LerTexto(argumentos, "nomeColaborador")
            };

            var movimentacoes = await _uof.MovimentacaoRepository.GetRelatorioAsync(parametros);

            // O 'encontrados' não é enfeite: é ele que impede o modelo de somar uma página e
            // apresentar o resultado como se fosse o total do período.
            return Serializar(new
            {
                encontrados = movimentacoes.TotalItemCount,
                exibidos = movimentacoes.Count,
                movimentacoes = movimentacoes.Select(Resumir)
            });
        }

        // Filtro ausente e filtro em branco são a mesma coisa: null, que o repositório ignora.
        private static string? LerTexto(IReadOnlyDictionary<string, string> argumentos, string nome) =>
            argumentos.TryGetValue(nome, out var valor) && !string.IsNullOrWhiteSpace(valor)
                ? valor.Trim()
                : null;

        // A data chega como texto escrito pelo modelo. O formato é fixado em ISO e o parse é
        // EXATO de propósito: com TryParse solto, "03/07" vira 3 de julho ou 7 de março
        // conforme a cultura do processo — e a do Cloud Run não é a da máquina de quem
        // desenvolve. Ausente é válido (o filtro é opcional); mal escrita devolve false,
        // para virar erro legível em vez de período silenciosamente errado.
        private static bool TentarLerData(IReadOnlyDictionary<string, string> argumentos,
                                          string nome, out DateTime? data)
        {
            data = null;

            var texto = LerTexto(argumentos, nome);
            if (texto is null)
                return true;

            if (!DateTime.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                        DateTimeStyles.None, out var convertida))
                return false;

            data = convertida;
            return true;
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

        // Nomes resolvidos e tipo por extenso: o modelo não precisa decifrar 'S' nem cruzar
        // ids. O colaborador vem nulo em entrada e ajuste, que não têm quem retirou.
        private static object Resumir(Movimentacao movimentacao) => new
        {
            tipo = DescreverTipo(movimentacao.Tipo),
            quantidade = movimentacao.Quantidade,
            data = movimentacao.DataMovimentacao?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            produto = movimentacao.Produto?.Descricao,
            colaborador = movimentacao.Colaborador?.Nome,
            registradoPor = movimentacao.RegistradoPor
        };

        // Espelha o TipoDescricao do MappingProfile: o que a tela mostra e o que o assistente
        // fala precisam usar a mesma palavra.
        private static string DescreverTipo(char tipo) => tipo switch
        {
            'E' => "Entrada",
            'S' => "Saída",
            _ => "Ajuste"
        };

        // JSON em vez de frase pronta: o modelo lê estrutura melhor que prosa, e a
        // formatação da resposta continua sendo decisão dele, não regra escondida aqui.
        private static string Serializar(object valor) => JsonSerializer.Serialize(valor, OpcoesJson);
    }
}