using Kasko.Entities.Concrete;
using System.Security.Cryptography;

namespace Kasko.DataAccess.Repositories.Abstract;

public interface IUserRepository : IGenericRepository<User>
{
    Task<bool> DeleteUserAsync(Guid id, Guid? deletedBy);

    Task<User?> GetByIdWithRoleAsync(Guid id);


}