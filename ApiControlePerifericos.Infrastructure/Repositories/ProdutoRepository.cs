using ApiControlePerifericos.Context;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using Microsoft.EntityFrameworkCore;
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
            // O uso de IQueryable em vez de GetAllAsync (que traz tudo para a memória)
            // permite que a paginação (e o filtro) sejam feitos no banco de dados.
            var query = _context.Set<Produto>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(parameters.Descricao))
                query = query.Where(p => p.Descricao!.Contains(parameters.Descricao));

            var produtosOrdenados = query.OrderBy(p => p.ProdutoId);

            var produtosPaginados = produtosOrdenados.ToPagedList(parameters.PageNumber, parameters.PageSize);

            return await Task.FromResult(produtosPaginados);
        }

        public async Task<Produto?> GetByIdTrackedAsync(int produtoId)
        {
            // FindAsync busca pela PK e devolve a entidade rastreada (sem AsNoTracking),
            // para que a alteração do SaldoAtual seja persistida no CommitAsync
            // (mesma transação da movimentação).
            return await _context.Set<Produto>().FindAsync(produtoId);
        }

        public async Task<IEnumerable<Produto>> GetAbaixoEstoqueMinimoAsync()
        {
            return await _context.Set<Produto>()
                                 .AsNoTracking()
                                 .Where(p => p.SaldoAtual < p.EstoqueMinimo)
                                 .OrderBy(p => p.Descricao)
                                 .ToListAsync();
        }   
    }
}
