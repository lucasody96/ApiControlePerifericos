using ApiControlePerifericos.DTOs;
using ApiControlePerifericos.Services;

namespace ApiControlePerifericos.Interfaces
{
    // Regras de negócio de movimentação de estoque: cada operação grava a
    // movimentação E recalcula o SaldoAtual do produto na mesma transação.
    public interface IEstoqueService
    {
        // Entrada: soma ao saldo. Tipo 'E'.
        Task<EstoqueResult> RegistrarEntradaAsync(int produtoId, int quantidade, string? registradoPor);

        // Saída: subtrai do saldo, associada ao colaborador que retirou. Tipo 'S'.
        Task<EstoqueResult> RegistrarSaidaAsync(int produtoId, int quantidade, int colaboradorId, string? registradoPor);

        // Ajuste (perda/quebra): subtrai do saldo. Tipo 'A'.
        Task<EstoqueResult> RegistrarAjusteAsync(int produtoId, int quantidade, string? registradoPor);

        // Correção de histórico: estorna do saldo o efeito da movimentação atual e
        // aplica o da versão nova (que pode inclusive apontar para outro produto).
        Task<EstoqueResult> AtualizarMovimentacaoAsync(int movimentacaoId, MovimentacaoDTO dto);

        // Exclusão de histórico: estorna do saldo o efeito da movimentação removida.
        Task<EstoqueResult> ExcluirMovimentacaoAsync(int movimentacaoId);
    }
}
