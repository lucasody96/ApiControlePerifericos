using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using ApiControlePerifericos.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;


namespace ApiControlePerifericos.Services
{
    public class AnthropicAssistenteIA : IAssistenteIA
    {
        // Decisões validadas no protótipo de console (issue #40).
        private const string Modelo = "claude-sonnet-5";
        private const int MaxTokens = 1024;

        // Teto de idas ao modelo numa mesma pergunta. Cada rodada é uma chamada paga, e é
        // este limite que impede que uma escolha ruim de ferramenta vire ciclo. Pergunta
        // comum resolve em duas: uma para consultar, outra para responder.
        private const int MaxRodadas = 5;

        private readonly AnthropicClient _client;
        private readonly ILogger<AnthropicAssistenteIA> _logger;

        public AnthropicAssistenteIA(AnthropicClient client, ILogger<AnthropicAssistenteIA> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<string> ResponderAsync(string instrucoes, string contextoCacheavel, string pergunta,
                                                 IReadOnlyList<FerramentaAssistente> ferramentas,
                                                 CancellationToken cancellationToken = default)
        {
            try
            {
                var porNome = ferramentas.ToDictionary(ferramenta => ferramenta.Nome);

                // A conversa cresce a cada rodada — pergunta, o que o modelo pediu, o que as
                // ferramentas responderam — e é reenviada inteira, porque a API não guarda
                // estado entre chamadas.
                var mensagens = new List<MessageParam>
                {
                    new() { Role = Role.User, Content = pergunta }
                };

                for (var rodada = 1; rodada <= MaxRodadas; rodada++)
                {
                    var resposta = await _client.Messages.Create(new MessageCreateParams
                    {
                        Model = Modelo,
                        MaxTokens = MaxTokens,

                        // O protótipo (issue #40) tinha desligado o thinking porque Q&A sobre
                        // texto curto não precisa. Com ferramentas a conta mudou: medido nas
                        // mesmas quatro perguntas, adaptive com effort baixo ficou ~22% mais
                        // rápido que desligado (3,1 s contra 4,0 s de média), escolhendo as
                        // mesmas ferramentas. O ganho vem do effort, que enxuga a resposta
                        // inteira — não é o raciocínio que estava custando caro.
                        Thinking = new ThinkingConfigAdaptive(),
                        OutputConfig = new OutputConfig { Effort = Effort.Low },

                        // As ferramentas entram ANTES do system no prefixo cacheado, então o
                        // breakpoint lá embaixo cobre ferramentas + instruções + manual. É por
                        // isso que a lista precisa vir sempre igual (ver IFerramentasAssistente).
                        Tools = [.. ferramentas.Select(Traduzir)],

                        System = new List<TextBlockParam>
                        {
                            new() { Text = instrucoes },
                            new() { Text = contextoCacheavel, CacheControl = new CacheControlEphemeral() }
                        },

                        // A conversa fica FORA do prefixo cacheado. Se entrasse no System, cada
                        // pergunta nova mudaria o prefixo e invalidaria o cache de todas as outras.
                        Messages = [.. mensagens]
                    }, cancellationToken);

                    _logger.LogInformation(
                        "Assistente rodada {Rodada}, parada por {Motivo}. Cache: criado={CacheCriado}, lido={CacheLido}.",
                        rodada, resposta.StopReason, resposta.Usage.CacheCreationInputTokens, resposta.Usage.CacheReadInputTokens);

                    // Sem pedido de ferramenta o turno acabou: o que veio é a resposta final.
                    if (resposta.StopReason != StopReason.ToolUse)
                        return ExtrairTexto(resposta);

                    // O turno do modelo volta para a conversa, seguido dos resultados. Os dois
                    // são obrigatórios: sem o eco, a API não sabe a que o resultado responde.
                    mensagens.Add(new() { Role = Role.Assistant, Content = ReconstruirResposta(resposta) });
                    mensagens.Add(new() { Role = Role.User, Content = await ExecutarFerramentasAsync(resposta, porNome, cancellationToken) });
                }

                // Chegar aqui é o modelo pedindo consulta depois de MaxRodadas idas. Isso é
                // anomalia, não resposta ruim: falhar visível é melhor que devolver meia resposta.
                throw new AssistenteIAException(
                    $"O assistente continuou pedindo consultas apos {MaxRodadas} rodadas e foi interrompido.");
            }
            // Traduz as exceções do SDK para o tipo que a Application conhece — do mais
            // específico para o mais genérico.
            catch (AnthropicRateLimitException ex)
            {
                throw new AssistenteIAException("Limite de requisições da API da Anthropic atingido.", ex);
            }
            catch (AnthropicApiException ex)
            {
                throw new AssistenteIAException("A API da Anthropic retornou erro.", ex);
            }
            catch (AnthropicIOException ex)
            {
                throw new AssistenteIAException("Falha de rede ao chamar a API da Anthropic.", ex);
            }
            // Cancelamento vindo do próprio cliente (janela fechada, request abortado) não é
            // falha da integração: deixa subir para o ASP.NET encerrar a requisição.
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            // Rede de segurança: timeout do HttpClient e qualquer outra exceção do SDK também
            // viram falha tratada (503), em vez de 500 genérico do ApiExceptionFilter.
            catch (Exception ex) when (ex is not AssistenteIAException)
            {
                throw new AssistenteIAException("Falha inesperada ao chamar a API da Anthropic.", ex);
            }
        }

        // Traduz a ferramenta da Application para o formato do SDK. O esquema JSON só existe
        // aqui: a Application descreve parâmetros, não protocolo.
        private static Tool Traduzir(FerramentaAssistente ferramenta) => new()
        {
            Name = ferramenta.Nome,
            Description = ferramenta.Descricao,
            InputSchema = new()
            {
                Properties = ferramenta.Parametros.ToDictionary(
                    parametro => parametro.Nome,
                    parametro => JsonSerializer.SerializeToElement(new
                    {
                        type = parametro.Tipo == TipoParametroFerramenta.Inteiro ? "integer" : "string",
                        description = parametro.Descricao
                    })),

                Required = [.. ferramenta.Parametros.Where(parametro => parametro.Obrigatorio)
                                                    .Select(parametro => parametro.Nome)]
            }
        };

        // O turno do assistente volta inteiro. Se um bloco ficar de fora, a rodada seguinte é
        // recusada: a API exige que todo tool_use tenha o seu par.
        private static List<ContentBlockParam> ReconstruirResposta(Message resposta)
        {
            var blocos = new List<ContentBlockParam>();

            foreach (var bloco in resposta.Content)
            {
                if (bloco.TryPickText(out var texto))
                {
                    blocos.Add(new TextBlockParam { Text = texto.Text });
                }
                else if (bloco.TryPickToolUse(out var chamada))
                {
                    blocos.Add(new ToolUseBlockParam
                    {
                        ID = chamada.ID,
                        Name = chamada.Name,
                        Input = chamada.Input
                    });
                }
                // Não acontece com o thinking desligado, mas a assinatura precisa voltar
                // intacta quando ele for ligado — a API recusa se ela for alterada.
                else if (bloco.TryPickThinking(out var raciocinio))
                {
                    blocos.Add(new ThinkingBlockParam
                    {
                        Thinking = raciocinio.Thinking,
                        Signature = raciocinio.Signature
                    });
                }
                else if (bloco.TryPickRedactedThinking(out var redigido))
                {
                    blocos.Add(new RedactedThinkingBlockParam { Data = redigido.Data });
                }
            }

            return blocos;
        }

        // Um resultado para CADA consulta pedida, todos na MESMA mensagem: dividir em
        // mensagens diferentes ensina o modelo a parar de pedir consultas em paralelo.
        private async Task<List<ContentBlockParam>> ExecutarFerramentasAsync(
            Message resposta,
            IReadOnlyDictionary<string, FerramentaAssistente> porNome,
            CancellationToken cancellationToken)
        {
            var resultados = new List<ContentBlockParam>();

            foreach (var bloco in resposta.Content)
            {
                if (!bloco.TryPickToolUse(out var chamada))
                    continue;

                // Nome fora do catálogo: o erro volta como resultado, não como exceção — o
                // modelo lê, corrige e tenta de novo na rodada seguinte.
                if (!porNome.TryGetValue(chamada.Name, out var ferramenta))
                {
                    _logger.LogWarning("O assistente pediu a ferramenta desconhecida {Ferramenta}.", chamada.Name);

                    resultados.Add(new ToolResultBlockParam
                    {
                        ToolUseID = chamada.ID,
                        Content = $"A ferramenta '{chamada.Name}' nao existe.",
                        IsError = true
                    });

                    continue;
                }

                _logger.LogInformation("O assistente consultou {Ferramenta}.", ferramenta.Nome);

                try
                {
                    var resultado = await ferramenta.Executar(ConverterArgumentos(chamada.Input), cancellationToken);

                    resultados.Add(new ToolResultBlockParam { ToolUseID = chamada.ID, Content = resultado });
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Falha do lado de cá (banco fora, argumento impossível). Devolver como
                    // erro da consulta deixa o modelo avisar que não conseguiu consultar, em
                    // vez de a pergunta inteira virar 503.
                    _logger.LogError(ex, "Falha ao executar a ferramenta {Ferramenta}.", ferramenta.Nome);

                    resultados.Add(new ToolResultBlockParam
                    {
                        ToolUseID = chamada.ID,
                        Content = "A consulta falhou. Informe que nao foi possivel consultar o estoque agora.",
                        IsError = true
                    });
                }
            }

            return resultados;
        }

        // O JSON dos argumentos vira dicionário de strings: quem escreve ferramenta não
        // precisa saber que o transporte é JSON, e texto e número chegam do mesmo jeito.
        private static IReadOnlyDictionary<string, string> ConverterArgumentos(
            IReadOnlyDictionary<string, JsonElement> argumentos) =>
            argumentos.ToDictionary(
                item => item.Key,
                item => item.Value.ValueKind == JsonValueKind.String
                    ? item.Value.GetString() ?? string.Empty
                    : item.Value.ToString());

        private string ExtrairTexto(Message resposta)
        {
            // A resposta pode trazer mais de um bloco (às vezes um de raciocínio antes do
            // texto): junta TODOS os blocos de texto, em vez de assumir o índice 0 ou
            // parar no primeiro — o resto sumiria sem ninguém perceber.
            var texto = string.Join("\n\n", resposta.Content
                .Select(bloco => bloco.Value)
                .OfType<TextBlock>()
                .Select(bloco => bloco.Text));

            if (string.IsNullOrWhiteSpace(texto))
                throw new AssistenteIAException("A resposta da IA não trouxe nenhum bloco de texto.");

            // Ao bater o MaxTokens a API corta a resposta no meio da frase. Sem este aviso,
            // o texto parcial chegaria ao usuário como se fosse a resposta completa.
            if (resposta.StopReason == StopReason.MaxTokens)
            {
                _logger.LogWarning("Resposta do assistente truncada no limite de {MaxTokens} tokens.", MaxTokens);

                texto += "\n\n(Resposta cortada por limite de tamanho. Refaça a pergunta de forma mais específica.)";
            }

            return texto;
        }
    }
}