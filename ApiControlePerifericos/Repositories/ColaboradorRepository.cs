using ApiControlePerifericos.Context;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
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
            // permite que a paginação seja feita diretamente no banco de dados.
            var colaboradoresOrdenados = _context.Set<Colaborador>()
                                                 .OrderBy(c => c.ColaboradorId);

            var colaboradoresPaginados = colaboradoresOrdenados.ToPagedList(parameters.PageNumber, parameters.PageSize);

            return await Task.FromResult(colaboradoresPaginados);
        }

        // TODO - Filtrar por ID ou Nome
    }
}
