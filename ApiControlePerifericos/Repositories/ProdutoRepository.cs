using ApiControlePerifericos.Context;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Models;

namespace ApiControlePerifericos.Repositories
{
    public class ProdutoRepository: Repository<Produto>, IProdutoRepository
    {
        public ProdutoRepository(AppDbContext context) : base(context)
        {

        }

        //Não há métodos específicos para Produto além dos genéricos implementados na classe base Repository<T>.
    }
}
