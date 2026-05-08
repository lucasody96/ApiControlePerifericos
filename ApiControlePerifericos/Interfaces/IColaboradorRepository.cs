using ApiControlePerifericos.Models;
using ApiControlePerifericos.Pagination;
using X.PagedList;

namespace ApiControlePerifericos.Interfaces
{
    public interface IColaboradorRepository : IRepository<Colaborador>
    {
        Task<IPagedList<Colaborador>> GetColaboradoresAsync(ColaboradoresParameters colaboradoresParams);

    }
}
