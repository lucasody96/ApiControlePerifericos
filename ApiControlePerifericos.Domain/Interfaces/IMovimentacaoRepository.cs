using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using X.PagedList;

namespace ApiControlePerifericos.Interfaces
{
    public interface IMovimentacaoRepository : IRepository<Movimentacao>
    {
        Task<IPagedList<Movimentacao>> GetMovimentacoesAsync(MovimentacoesParameters parameters);

        Task<IPagedList<Movimentacao>> GetRelatorioAsync(MovimentacoesParameters parameters);

        // Retorna a movimentação rastreada pelo contexto, para permitir alterá-la ou
        // excluí-la no mesmo CommitAsync que ajusta o saldo do produto.
        Task<Movimentacao?> GetByIdTrackedAsync(int movimentacaoId);

        // Filtros sem paginação: histórico de movimentações de um produto/colaborador.
        Task<IEnumerable<Movimentacao>> GetByProdutoIdAsync(int produtoId);

        Task<IEnumerable<Movimentacao>> GetByColaboradorIdAsync(int colaboradorId);
    }
}
