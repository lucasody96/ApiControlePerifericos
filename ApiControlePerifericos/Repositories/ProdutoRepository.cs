using ApiControlePerifericos.Context;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;

using X.PagedList;
using X.PagedList.Extensions;

namespace ApiControlePerifericos.Repositories
{
    public class ProdutoRepository : Repository<Produto>, IProdutoRepository
    {
        public ProdutoRepository(AppDbContext context) : base(context)
        {

        }

        public async Task<IPagedList<Produto>> GetProdutosAsync(ProdutosParameters parameters)
        {
            var produtosOrdenados = _context.Set<Produto>()
                                            .OrderBy(p => p.ProdutoId);

            var resultado = produtosOrdenados.ToPagedList(parameters.PageNumber, parameters.PageSize);

            return await Task.FromResult(resultado);
        }

        //pensar num futuro filter por ID ou descrição do produto
    }
}
