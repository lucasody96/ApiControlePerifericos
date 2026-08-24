using ApiControlePerifericos.Services;

namespace ApiControlePerifericos.Interfaces
{
    // Regra de negócio do assistente: valida a pergunta, monta o contexto e traduz
    // falha da integração em resultado tratado.
    public interface IAssistenteService
    {
        Task<AssistenteResult> ResponderAsync(string? pergunta, CancellationToken cancellationToken = default);
    }
}