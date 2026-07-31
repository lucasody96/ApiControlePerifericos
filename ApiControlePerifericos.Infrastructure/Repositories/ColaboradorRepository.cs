using ApiControlePerifericos.Context;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

namespace ApiControlePerifericos.Repositories
{
    public class ColaboradorRepository : Repository<Colaborador>, IColaboradorRepository
    {
        public ColaboradorRepository(AppDbContext context) : base(context)
        {

        }

        public async Task<IPagedList<Colaborador>> GetColaboradoresAsync(ColaboradoresParameters parameters)
        {
            // O uso de IQueryable em vez de GetAllAsync (que traz tudo para a memória)
            // permite que a paginação (e o filtro) sejam feitos no banco de dados.
            var query = _context.Set<Colaborador>().AsNoTracking();

            if (!string.IsNullOrWhiteSpace(parameters.Nome))
                query = query.Where(c => c.Nome!.Contains(parameters.Nome));

            var colaboradoresOrdenados = query.OrderBy(c => c.ColaboradorId);

            var colaboradoresPaginados = colaboradoresOrdenados.ToPagedList(parameters.PageNumber, parameters.PageSize);

            return await Task.FromResult(colaboradoresPaginados);
        }
    }
}
