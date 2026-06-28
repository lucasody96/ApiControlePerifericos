using ApiControlePerifericos.Context;
using ApiControlePerifericos.Interfaces;

namespace ApiControlePerifericos.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        // Os repositórios vêm do DI (não mais via `new`) — assim o container injeta
        // os decorators de cache (CachedProdutoRepository/CachedColaboradorRepository).
        // Todos compartilham o mesmo AppDbContext scoped, então o CommitAsync persiste
        // as alterações feitas por qualquer um deles na mesma transação.
        public UnitOfWork(
            AppDbContext context,
            IColaboradorRepository colaboradorRepository,
            IProdutoRepository produtoRepository,
            IMovimentacaoRepository movimentacaoRepository)
        {
            _context = context;
            ColaboradorRepository = colaboradorRepository;
            ProdutoRepository = produtoRepository;
            MovimentacaoRepository = movimentacaoRepository;
        }

        public IColaboradorRepository ColaboradorRepository { get; }
        public IProdutoRepository ProdutoRepository { get; }
        public IMovimentacaoRepository MovimentacaoRepository { get; }

        public async Task CommitAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
