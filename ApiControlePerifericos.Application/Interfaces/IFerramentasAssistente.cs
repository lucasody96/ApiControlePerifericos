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
        // invalida o cache do manual junto. Sempre a mesma lista, na mesma ordem.
        IReadOnlyList<FerramentaAssistente> Obter();
    }
}