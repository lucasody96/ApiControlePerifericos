using ApiControlePerifericos.Models;

namespace ApiControlePerifericos.Services
{
    public enum EstoqueResultStatus
    {
        Sucesso,
        ProdutoNaoEncontrado,
        ColaboradorNaoEncontrado,
        SaldoInsuficiente,
        MovimentacaoNaoEncontrada,
        // Tipo fora de 'E'/'S'/'A' — sem ele não há como saber o efeito no saldo.
        TipoInvalido,
        // Saída sem colaborador: quem retirou o item é obrigatório no tipo 'S'.
        ColaboradorObrigatorio,
        // Alterar ou excluir uma movimentação estorna o efeito dela no saldo;
        // se o resultado desse estorno for negativo, a operação é recusada.
        SaldoNegativoAposEstorno
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
