using ApiControlePerifericos.Interfaces;
using Microsoft.Extensions.Logging;

namespace ApiControlePerifericos.Services
{
    public class AssistenteService : IAssistenteService
    {
        // Limite da pergunta. É regra, então mora aqui — o PerguntaDTO referencia esta
        // constante para que a validação do ModelState não divirja da do serviço.
        public const int TamanhoMaximoPergunta = 500;

        // O guardrail é regra de negócio, não detalhe de integração: quem define o que o
        // assistente pode responder é a Application.
        private const string Instrucoes = """
            Você é o assistente do Controle de Periféricos e tira dúvidas de quem usa o sistema.

            Regras:
            - Para dúvidas sobre COMO usar o sistema, responda apenas com base no manual
              fornecido a seguir.
            - Para perguntas sobre a situação ATUAL do estoque (saldo, estoque mínimo, o que
              está faltando), use as ferramentas de consulta. Nunca tire um número de estoque
              do manual e nunca estime um saldo.
            - Se a ferramenta não encontrar o produto, diga que não encontrou e sugira conferir
              a descrição. Não invente o resultado.
            - Se a resposta não estiver no manual nem nas ferramentas, diga que não sabe e
              oriente procurar o administrador. Nunca invente telas, botões ou regras.
            - Responda em português do Brasil, curto e direto.
            """;

        private readonly IAssistenteIA _ia;
        private readonly IManualProvider _manual;
        private readonly ILogger<AssistenteService> _logger;
        private readonly IFerramentasAssistente _ferramentas;

        public AssistenteService(IAssistenteIA ia, IManualProvider manual,
                                 IFerramentasAssistente ferramentas, ILogger<AssistenteService> logger)
        {
            _ia = ia;
            _manual = manual;
            _ferramentas = ferramentas;
            _logger = logger;
        }

        public async Task<AssistenteResult> ResponderAsync(string? pergunta, bool ehAdmin,
                                                           CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pergunta))
                return AssistenteResult.Falha(AssistenteResultStatus.PerguntaVazia,
                    "Informe uma pergunta.");

            pergunta = pergunta.Trim();

            if (pergunta.Length > TamanhoMaximoPergunta)
                return AssistenteResult.Falha(AssistenteResultStatus.PerguntaMuitoLonga,
                    $"A pergunta deve ter no máximo {TamanhoMaximoPergunta} caracteres.");

            try
            {
                var resposta = await _ia.ResponderAsync(Instrucoes, _manual.ObterConteudo(),
                                                        pergunta, _ferramentas.Obter(ehAdmin), cancellationToken);
                return AssistenteResult.Ok(resposta);
            }
            catch (AssistenteIAException ex)
            {
                // A falha da integração não vaza para o cliente: vira resultado tratado e
                // o detalhe técnico fica no log.
                _logger.LogError(ex, "Falha ao consultar o assistente de IA.");

                return AssistenteResult.Falha(AssistenteResultStatus.FalhaNaIA,
                    "O assistente está indisponível no momento. Tente novamente em alguns instantes.");
            }
        }
    }
}