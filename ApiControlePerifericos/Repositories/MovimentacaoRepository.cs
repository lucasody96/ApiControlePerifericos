using ApiControlePerifericos.Context;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;

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
            var movimentacoes = await GetAllAsync();

            var movimentacoesLocalizadas = movimentacoes.Where(m => m.ProdutoId == produtoId).ToList();

            return movimentacoesLocalizadas;
        }

        public async Task<IEnumerable<Movimentacao>> GetByColaboradorIdAsync(int colaboradorId)
        {
            var movimentacoes = await GetAllAsync();

            var movimentacoesLocalizadas = movimentacoes.Where(m => m.ColaboradorId == colaboradorId).ToList();

            return movimentacoesLocalizadas;
        }

        public async Task<IPagedList<Movimentacao>> GetMovimentacoesAsync(MovimentacoesParameters parameters)
        {
            var movimentacoesOrdenadas = _context.Set<Movimentacao>()
                                                 .OrderByDescending(m => m.DataMovimentacao);

            var resultado = movimentacoesOrdenadas.ToPagedList(parameters.PageNumber, parameters.PageSize);

            return await Task.FromResult(resultado);
        }

        // TODO - Filtrar por Data, Produto ou Colaborador
    }
}
