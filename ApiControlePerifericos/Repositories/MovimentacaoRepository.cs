using ApiControlePerifericos.Context;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

namespace ApiControlePerifericos.Repositories
{
    public class MovimentacaoRepository : Repository<Movimentacao>, IMovimentacaoRepository
    {
        public MovimentacaoRepository(AppDbContext context) : base(context)
        {

        }

        public async Task<IEnumerable<Movimentacao>> GetByProdutoIdAsync(int produtoId)
        {
            return await _context.Set<Movimentacao>()
                                 .Where(m => m.ProdutoId == produtoId)
                                 .AsNoTracking()
                                 .ToListAsync();
        }

        public async Task<IEnumerable<Movimentacao>> GetByColaboradorIdAsync(int colaboradorId)
        {
            return await _context.Set<Movimentacao>()
                                 .Where(m => m.ColaboradorId == colaboradorId)
                                 .AsNoTracking()
                                 .ToListAsync();
        }

        public async Task<IPagedList<Movimentacao>> GetMovimentacoesAsync(MovimentacoesParameters parameters)
        {
            // O uso de IQueryable em vez de GetAllAsync (que traz tudo para a memória)
            // permite que a paginação seja feita diretamente no banco de dados.
            var movimentacoesOrdenadas = _context.Set<Movimentacao>()
                                                 .OrderByDescending(m => m.DataMovimentacao);

            var movimentacoesPaginadas = movimentacoesOrdenadas.ToPagedList(parameters.PageNumber, parameters.PageSize);

            return await Task.FromResult(movimentacoesPaginadas);
        }

        // TODO - Filtrar por Data, Produto ou Colaborador
    }
}
