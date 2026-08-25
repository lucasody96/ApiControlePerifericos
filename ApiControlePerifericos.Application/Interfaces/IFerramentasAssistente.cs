using ApiControlePerifericos.Services;

namespace ApiControlePerifericos.Interfaces
{
    // Catálogo do que o assistente pode consultar. Fica separado do AssistenteService
    // porque a lista cresce a cada ferramenta nova, e responder pergunta é uma
    // responsabilidade diferente de saber o que existe para consultar.
    public interface IFerramentasAssistente
    {
        // A ORDEM importa. As ferramentas são renderizadas antes do manual no prompt, e o
        // cache cobre um prefixo: lista fora de ordem, ou que varia entre requisições,
        // invalida o cache do manual junto.
        //
        // O ehAdmin é a ÚNICA coisa que pode variar a lista, e ele só ACRESCENTA ao fim —
        // a lista de não-admin é prefixo literal da de admin. São dois prefixos de cache
        // estáveis (issue #49), não uma lista que muda a cada chamada.
        //
        // A autorização mora aqui, e não no endpoint, porque as ferramentas de movimentação
        // leem dado que o MovimentacoesController inteiro protege com AdminOnly. Sem isso o
        // assistente seria porta lateral para dado restrito.
        IReadOnlyList<FerramentaAssistente> Obter(bool ehAdmin);
    }
}