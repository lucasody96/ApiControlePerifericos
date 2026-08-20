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
            // permite que o filtro e a paginação sejam feitos no banco de dados.
            // Sem Include: o filtro por Produto/Colaborador já vira JOIN sozinho e o
            // MovimentacaoDTO não lê as propriedades de navegação (quem lê é o relatório).
            var query = _context.Set<Movimentacao>().AsNoTracking();

            query = AplicarFiltros(query, parameters);

            var movimentacoesOrdenadas = query.OrderByDescending(m => m.DataMovimentacao);

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

            query = AplicarFiltros(query, parameters);

            // Ordenar por DataMovimentacao em ordem decrescente
            var ordenadas = query.OrderByDescending(m => m.DataMovimentacao);
            // Aplicar paginação
            return await Task.FromResult(ordenadas.ToPagedList(parameters.PageNumber, parameters.PageSize));
        }

        // Filtros compartilhados por /pagination e /relatorio: os dois expõem o mesmo
        // MovimentacoesParameters, então a semântica precisa ser idêntica nos dois.
        private static IQueryable<Movimentacao> AplicarFiltros(IQueryable<Movimentacao> query,
                                                               MovimentacoesParameters parameters)
        {
            if (parameters.DataInicio.HasValue)
                query = query.Where(m => m.DataMovimentacao >= parameters.DataInicio.Value);

            // DataFim é inclusiva do dia inteiro: quem filtra até 18/01 espera as
            // movimentações das 18h daquele dia, não só as da meia-noite.
            if (parameters.DataFim.HasValue)
                query = query.Where(m => m.DataMovimentacao < parameters.DataFim.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(parameters.DescricaoProduto))
                query = query.Where(m => m.Produto!.Descricao!.Contains(parameters.DescricaoProduto));

            // Entrada e ajuste não têm colaborador, então naturalmente não passam neste filtro.
            if (!string.IsNullOrWhiteSpace(parameters.NomeColaborador))
                query = query.Where(m => m.Colaborador!.Nome!.Contains(parameters.NomeColaborador));

            // Os filtros por id convivem com os de texto: quem manda os dois recebe a
            // interseção. Aqui a comparação é exata, sem o Contains.
            if (parameters.ProdutoId.HasValue)
                query = query.Where(m => m.ProdutoId == parameters.ProdutoId.Value);

            if (parameters.ColaboradorId.HasValue)
                query = query.Where(m => m.ColaboradorId == parameters.ColaboradorId.Value);

            return query;
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
