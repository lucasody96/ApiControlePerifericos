using ApiControlePerifericos.Models;

namespace ApiControlePerifericos.Interfaces
{
    public interface IMovimentacaoRepository : IRepository<Movimentacao>
    {
        IEnumerable<Movimentacao> GetByProdutoId(int produtoId);
        IEnumerable<Movimentacao> GetByColaboradorId(int colaboradorId);
    }
}
