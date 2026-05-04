using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;

namespace ApiControlePerifericos.Interfaces
{
    public interface IMovimentacaoRepository : IRepository<Movimentacao>
    {
        IEnumerable<Movimentacao> GetByProdutoId(int produtoId);
        IEnumerable<Movimentacao> GetByColaboradorId(int colaboradorId);

        PagedList<Movimentacao> GetMovimentacoes(MovimentacoesParameters movimentacoesParams);
    }
}
