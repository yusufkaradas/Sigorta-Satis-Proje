
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;


namespace Kasko.DataAccess.Repositories.Concrete;

public class RoleRepository
    : GenericRepository<Role>, IRoleRepository
{
    public RoleRepository(KaskoContext context)
        : base(context)
    {
    }
}