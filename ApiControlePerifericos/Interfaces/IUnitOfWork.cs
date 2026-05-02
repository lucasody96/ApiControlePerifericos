namespace ApiControlePerifericos.Interfaces
{
    public interface IUnitOfWork
    {
        IColaboradorRepository ColaboradorRepository { get; }
        IProdutoRepository ProdutoRepository { get; }
        IMovimentacaoRepository MovimentacaoRepository { get; }

        void Commit();
    }
}
