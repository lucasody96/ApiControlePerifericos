using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using X.PagedList;

namespace ApiControlePerifericos.Interfaces
{
    public interface IMovimentacaoRepository : IRepository<Movimentacao>
    {
        Task<IPagedList<Movimentacao>> GetMovimentacoesAsync(MovimentacoesParameters parameters);

        Task<IPagedList<Movimentacao>> GetRelatorioAsync(MovimentacoesParameters parameters);
    }
}
