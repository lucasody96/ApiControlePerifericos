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

            return movimentacoes;
        }

        public async Task<IPagedList<Movimentacao>> GetMovimentacoesAsync(MovimentacoesParameters movimentacoesParams)
        {
            var movimentacoesOrdenadas = _context.Set<Movimentacao>()
                                                 .OrderByDescending(m => m.DataMovimentacao);

            var resultado = movimentacoesOrdenadas.ToPagedList(movimentacoesParams.PageNumber, movimentacoesParams.PageSize);

            return await Task.FromResult(resultado);
        }

        //Futuramente pensar num filter por data, ou mesmo por produto ou colaborador
    }
}
