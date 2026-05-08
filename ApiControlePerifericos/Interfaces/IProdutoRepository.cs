using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using X.PagedList;

namespace ApiControlePerifericos.Interfaces
{
    public interface IProdutoRepository : IRepository<Produto>
    {
        Task<IPagedList<Produto>> GetProdutosAsync(ProdutosParameters produtosParams);

    }
}
