using ApiControlePerifericos.Models;

namespace ApiControlePerifericos.Services
{
    public enum EstoqueResultStatus
    {
        Sucesso,
        ProdutoNaoEncontrado,
        ColaboradorNaoEncontrado,
        SaldoInsuficiente
    }

    // Resultado de uma operação de estoque, permitindo ao controller mapear
    // o desfecho para o status HTTP adequado sem depender de exceções.
    public record EstoqueResult(EstoqueResultStatus Status, Movimentacao? Movimentacao, string? Mensagem)
    {
        public bool Sucesso => Status == EstoqueResultStatus.Sucesso;

        public static EstoqueResult Ok(Movimentacao movimentacao) =>
            new(EstoqueResultStatus.Sucesso, movimentacao, null);

        public static EstoqueResult Falha(EstoqueResultStatus status, string mensagem) =>
            new(status, null, mensagem);
    }
}
