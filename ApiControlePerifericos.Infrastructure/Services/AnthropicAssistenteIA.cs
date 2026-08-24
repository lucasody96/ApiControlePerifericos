using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using ApiControlePerifericos.Interfaces;
using Microsoft.Extensions.Logging;


namespace ApiControlePerifericos.Services
{
    public class AnthropicAssistenteIA : IAssistenteIA
    {
        // Decisões validadas no protótipo de console (issue #40).
        private const string Modelo = "claude-sonnet-5";
        private const int MaxTokens = 1024;

        private readonly AnthropicClient _client;
        private readonly ILogger<AnthropicAssistenteIA> _logger;

        public AnthropicAssistenteIA(AnthropicClient client, ILogger<AnthropicAssistenteIA> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<string> ResponderAsync(string instrucoes, string contextoCacheavel, string pergunta, CancellationToken cancellationToken = default)
        {
            try
            {
                var resposta = await _client.Messages.Create(new MessageCreateParams
                {
                    Model = Modelo,
                    MaxTokens = MaxTokens,

                    // Q&A sobre um texto curto não precisa de raciocínio extra: desligar corta latência e
                    // tokens de saída. A extração por OfType<TextBlock> mais abaixo continua valendo — ela
                    // é defesa contra o formato da resposta, não contra este parâmetro.
                    Thinking = new ThinkingConfigDisabled(),

                    // Dois blocos: instruções fixas primeiro, manual depois com CacheControl.
                    // O cache cobre todo o prefixo até o breakpoint, então instruções + manual
                    // são reaproveitados entre requisições (TTL de 5 minutos).
                    System = new List<TextBlockParam>
                    {
                        new() { Text = instrucoes },
                        new() { Text = contextoCacheavel, CacheControl = new CacheControlEphemeral() }
                    },

                    // A pergunta fica FORA do prefixo cacheado. Se entrasse no System, cada
                    // pergunta nova mudaria o prefixo e invalidaria o cache de todas as outras.
                    Messages = [new() { Role = Role.User, Content = pergunta }]
                }, cancellationToken);

                _logger.LogInformation("Assistente respondeu. Cache: criado={CacheCriado}, lido={CacheLido}.", resposta.Usage.CacheCreationInputTokens, resposta.Usage.CacheReadInputTokens);

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
    }
}
