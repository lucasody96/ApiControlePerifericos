using ApiControlePerifericos.Services;

namespace ApiControlePerifericos.Interfaces
{
    // Regra de negócio do assistente: valida a pergunta, monta o contexto e traduz
    // falha da integração em resultado tratado.
    public interface IAssistenteService
    {
        // O ehAdmin não é preferência de usuário: é o que define quais ferramentas entram
        // no prompt. Quem sabe a role é o controller, que lê do JWT — por isso desce como
        // parâmetro, no mesmo padrão do RegistradoPor no EstoqueService.
        Task<AssistenteResult> ResponderAsync(string? pergunta, bool ehAdmin,
                                              CancellationToken cancellationToken = default);
    }
}