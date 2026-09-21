using System.Linq.Expressions;

namespace Kasko.DataAccess.Repositories.Abstract;

public interface IGenericRepository<T> where T : class, new()
{
    Task<IEnumerable<T>> GetAllAsync();

    Task<T?> GetByIdAsync(Guid id);

    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);

    Task UpdateAsync(T entity);

    Task DeleteAsync(T entity);
   
}