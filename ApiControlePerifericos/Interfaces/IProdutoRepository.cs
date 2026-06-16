using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using X.PagedList;

namespace ApiControlePerifericos.Interfaces
{
    public interface IProdutoRepository : IRepository<Produto>
    {
        Task<IPagedList<Produto>> GetProdutosAsync(ProdutosParameters parameters);

        // Retorna o produto rastreado pelo contexto, para permitir atualização do saldo.
        Task<Produto?> GetByIdTrackedAsync(int produtoId);
    }
}
