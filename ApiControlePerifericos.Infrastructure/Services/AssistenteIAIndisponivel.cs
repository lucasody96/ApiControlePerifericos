using ApiControlePerifericos.Interfaces;

namespace ApiControlePerifericos.Services
{
    // Usado quando a chave da Anthropic nao esta configurada. Em vez de impedir o startup
    // da aplicacao inteira por causa de um endpoint so, a API sobe normalmente e apenas o
    // assistente responde falha tratada (503) — produtos, movimentacoes e login seguem no ar.
    public class AssistenteIAIndisponivel : IAssistenteIA
    {
        private readonly string _chaveEsperada;

        public AssistenteIAIndisponivel(string chaveEsperada) => _chaveEsperada = chaveEsperada;

        public Task<string> ResponderAsync(string instrucoes, string contextoCacheavel, string pergunta,
                                           CancellationToken cancellationToken = default) =>
            throw new AssistenteIAException(
                $"O assistente esta desligado: a chave '{_chaveEsperada}' nao foi configurada. " +
                $"Defina-a nos user-secrets em desenvolvimento ou na variavel de ambiente " +
                $"'Anthropic__ApiKey' no ambiente de deploy.");
    }
}
