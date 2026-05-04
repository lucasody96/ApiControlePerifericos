using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;

namespace ApiControlePerifericos.Interfaces
{
    public interface IColaboradorRepository : IRepository<Colaborador>
    {
        PagedList<Colaborador> GetColaboradores(ColaboradoresParameters colaboradoresParams);

    }
}
