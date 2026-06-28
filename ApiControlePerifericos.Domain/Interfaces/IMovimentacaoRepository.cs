using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using X.PagedList;

namespace ApiControlePerifericos.Interfaces
{
    public interface IMovimentacaoRepository : IRepository<Movimentacao>
    {
        Task<IPagedList<Movimentacao>> GetMovimentacoesAsync(MovimentacoesParameters parameters);

        Task<IPagedList<Movimentacao>> GetRelatorioAsync(MovimentacoesParameters parameters);

        // Filtros sem paginação: histórico de movimentações de um produto/colaborador.
        Task<IEnumerable<Movimentacao>> GetByProdutoIdAsync(int produtoId);

        Task<IEnumerable<Movimentacao>> GetByColaboradorIdAsync(int colaboradorId);
    }
}
