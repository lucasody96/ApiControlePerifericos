using System.Linq.Expressions;

namespace ApiControlePerifericos.Interfaces
{
    public interface IRepository<T>
    {
        //cuidado para não violar o principio ISP
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetAsync(Expression<Func<T, bool>> predicate);
        // Verifica existência sem materializar a entidade (use em vez de GetAsync
        // quando o objetivo é apenas checar se o registro existe).
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        T Create(T entity);
        T Update(T entity);
        T Delete(T entity);
    }
}
