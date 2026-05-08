using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using X.PagedList;

namespace ApiControlePerifericos.Interfaces
{
    public interface IMovimentacaoRepository : IRepository<Movimentacao>
    {
        Task<IEnumerable<Movimentacao>> GetByProdutoIdAsync(int produtoId);
        Task<IEnumerable<Movimentacao>> GetByColaboradorIdAsync(int colaboradorId);

        Task<IPagedList<Movimentacao>> GetMovimentacoesAsync(MovimentacoesParameters movimentacoesParams);
    }
}
