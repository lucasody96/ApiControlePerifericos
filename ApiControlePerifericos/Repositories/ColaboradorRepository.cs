using ApiControlePerifericos.Context;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;

namespace ApiControlePerifericos.Repositories
{
    public class ColaboradorRepository : Repository<Colaborador>, IColaboradorRepository
    {
        public ColaboradorRepository(AppDbContext context) : base(context)
        {

        }

        //Não há métodos específicos para Colaborador além dos genéricos implementados na classe base Repository<T>.
    }
}
