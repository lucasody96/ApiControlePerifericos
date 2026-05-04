using ApiControlePerifericos.Context;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;

namespace ApiControlePerifericos.Repositories
{
    public class ColaboradorRepository : Repository<Colaborador>, IColaboradorRepository
    {
        public ColaboradorRepository(AppDbContext context) : base(context)
        {

        }

        public PagedList<Colaborador> GetColaboradores(ColaboradoresParameters colaboradoresParams)
        {
            var colaboradores = _context.Colaboradores.AsQueryable();

            var colaboradoresOrdenados = PagedList<Colaborador>.ToPagedList(colaboradores, colaboradoresParams.PageNumber, colaboradoresParams.PageSize);
            return colaboradoresOrdenados;
        }

        //Pensar num futuro filter por ID ou nome

    }
}
