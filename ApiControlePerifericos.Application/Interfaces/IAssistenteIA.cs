using ApiControlePerifericos.Services;

namespace ApiControlePerifericos.Interfaces
{
    // Porta para o modelo de linguagem. A Application entrega o QUE enviar; a
    // implementação decide COMO (formato dos blocos, cache, modelo, limites).

    public interface IAssistenteIA
    {
        /// <param name="instrucoes">Regras de comportamento do assistente.</param>
        /// <param name="contextoCacheavel">Conteúdo grande e estável (o manual); a
        /// implementação pode cacheá-lo entre requisições.</param>
        /// <param name="pergunta">Pergunta do usuário — sempre volátil.</param>
        /// <param name="ferramentas">O que o modelo pode consultar. A implementação executa
        /// o delegate da ferramenta que o modelo escolher e devolve o resultado a ele,
        /// repetindo enquanto ele pedir novas consultas.</param>
        Task<string> ResponderAsync(string instrucoes, string contextoCacheavel, string pergunta,
                                    IReadOnlyList<FerramentaAssistente> ferramentas,
                                    CancellationToken cancellationToken = default);
    }
}