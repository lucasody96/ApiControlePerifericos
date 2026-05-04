using ApiControlePerifericos.Context;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;

namespace ApiControlePerifericos.Repositories
{
    public class ProdutoRepository : Repository<Produto>, IProdutoRepository
    {
        public ProdutoRepository(AppDbContext context) : base(context)
        {

        }
        public PagedList<Produto> GetProdutos(ProdutosParameters produtosParams)
        {
            var produtos = _context.Produtos.AsQueryable();

            var produtosOrdenados = PagedList<Produto>.ToPagedList(produtos, produtosParams.PageNumber, produtosParams.PageSize);
            return produtosOrdenados;
        }

        //pensar num futuro filter por ID ou descrição do produto
    }
}
