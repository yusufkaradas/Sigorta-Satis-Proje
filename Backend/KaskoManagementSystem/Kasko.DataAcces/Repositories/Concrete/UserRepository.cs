
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Kasko.DataAccess.Repositories.Concrete;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(KaskoContext context)
        : base(context)
    {
    }

    public async Task<User?> GetByIdWithRoleAsync(Guid id)
    {
        return await _dbSet
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> DeleteUserAsync(Guid id, Guid? deletedBy)
    {
        var user = await _dbSet.FirstOrDefaultAsync(
            x => x.Id == id);

        if (user == null)
        {
            return false;
        }

        user.IsDeleted = true;
        user.DeletedDate = DateTime.UtcNow;
        user.DeletedBy = deletedBy;

        _dbSet.Update(user);

        return true;
    }
}