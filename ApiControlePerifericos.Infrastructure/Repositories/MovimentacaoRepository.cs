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

        public async Task<IPagedList<Movimentacao>> GetMovimentacoesAsync(MovimentacoesParameters parameters)
        {
            // O uso de IQueryable em vez de GetAllAsync (que traz tudo para a memória)
            // permite que a paginação seja feita diretamente no banco de dados.
            var movimentacoesOrdenadas = _context.Set<Movimentacao>()
                                                 .OrderByDescending(m => m.DataMovimentacao);

            var movimentacoesPaginadas = movimentacoesOrdenadas.ToPagedList(parameters.PageNumber, parameters.PageSize);

            return await Task.FromResult(movimentacoesPaginadas);
        }

        public async Task<IPagedList<Movimentacao>> GetRelatorioAsync(MovimentacoesParameters parameters)
        {
            var query = _context.Set<Movimentacao>()
                                .Include(m => m.Produto)
                                .Include(m => m.Colaborador)
                                .AsNoTracking()
                                .AsQueryable();
            if (parameters.DataInicio.HasValue)
                query = query.Where(m => m.DataMovimentacao >= parameters.DataInicio.Value);

            if (parameters.DataFim.HasValue)
                query = query.Where(m => m.DataMovimentacao < parameters.DataFim.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(parameters.DescricaoProduto))
                query = query.Where(m => m.Produto!.Descricao!.Contains(parameters.DescricaoProduto));

            if (!string.IsNullOrWhiteSpace(parameters.NomeColaborador))
                query = query.Where(m => m.Colaborador!.Nome!.Contains(parameters.NomeColaborador));

            // Ordenar por DataMovimentacao em ordem decrescente
            var ordenadas = query.OrderByDescending(m => m.DataMovimentacao);
            // Aplicar paginação
            return await Task.FromResult(ordenadas.ToPagedList(parameters.PageNumber, parameters.PageSize));
        }

        public async Task<Movimentacao?> GetByIdTrackedAsync(int movimentacaoId)
        {
            // FindAsync busca pela PK e devolve a entidade rastreada (sem AsNoTracking),
            // para que a alteração/exclusão da movimentação e o ajuste do SaldoAtual do
            // produto sejam persistidos no mesmo CommitAsync.
            return await _context.Set<Movimentacao>().FindAsync(movimentacaoId);
        }

        public async Task<IEnumerable<Movimentacao>> GetByProdutoIdAsync(int produtoId)
        {
            return await _context.Set<Movimentacao>()
                                 .AsNoTracking()
                                 .Where(m => m.ProdutoId == produtoId)
                                 .OrderByDescending(m => m.DataMovimentacao)
                                 .ToListAsync();
        }

        public async Task<IEnumerable<Movimentacao>> GetByColaboradorIdAsync(int colaboradorId)
        {
            return await _context.Set<Movimentacao>()
                                 .AsNoTracking()
                                 .Where(m => m.ColaboradorId == colaboradorId)
                                 .OrderByDescending(m => m.DataMovimentacao)
                                 .ToListAsync();
        }
    }
}
