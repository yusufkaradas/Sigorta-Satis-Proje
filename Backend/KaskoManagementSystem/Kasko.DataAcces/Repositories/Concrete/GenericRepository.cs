using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Abstract;

namespace Kasko.DataAccess.Repositories.Concrete;

public class GenericRepository<T> : IGenericRepository<T>
    where T : class, new()
{
    protected readonly KaskoContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(KaskoContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }
    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FirstOrDefaultAsync(
            x => EF.Property<Guid>(x, "Id") == id);
    }
    public async Task<IEnumerable<T>> FindAsync(
     Expression<Func<T, bool>> predicate)
    {
        return await _dbSet
            .Where(predicate)
            .ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);

        return Task.CompletedTask;
    }
    public Task DeleteAsync(T entity)
    {
        if (entity is BaseEntity baseEntity)
        {
            baseEntity.IsDeleted = true;
            baseEntity.DeletedDate = DateTime.UtcNow;

            _dbSet.Update(entity);
        }

        return Task.CompletedTask;
    }

    public async Task<bool> AnyAsync(
    Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }
}