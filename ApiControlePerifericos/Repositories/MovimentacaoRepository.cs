using ApiControlePerifericos.Context;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;

namespace ApiControlePerifericos.Repositories
{
    public class MovimentacaoRepository : Repository<Movimentacao>, IMovimentacaoRepository
    {
        public MovimentacaoRepository(AppDbContext context) : base(context)
        {

        }

        public IEnumerable<Movimentacao> GetByProdutoId(int produtoId)
        {
            return _context.Movimentacoes.Where(m => m.ProdutoId == produtoId).ToList();
        }

        public IEnumerable<Movimentacao> GetByColaboradorId(int colaboradorId)
        {
            return _context.Movimentacoes.Where(m => m.ColaboradorId == colaboradorId).ToList();
        }

        public PagedList<Movimentacao> GetMovimentacoes(MovimentacoesParameters movimentacoesParams)
        {
            var movimentacoes = _context.Movimentacoes.AsQueryable();

            var movimentacoesOrdenadas = PagedList<Movimentacao>.ToPagedList(movimentacoes, movimentacoesParams.PageNumber, movimentacoesParams.PageSize);
            return movimentacoesOrdenadas;
        }

        //Futuramente pensar num filter por data, ou mesmo por produto ou colaborador
    }
}
