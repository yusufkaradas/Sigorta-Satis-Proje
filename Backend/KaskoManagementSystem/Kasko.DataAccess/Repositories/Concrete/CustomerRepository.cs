using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;



namespace Kasko.DataAccess.Repositories.Concrete
{
    public class CustomerRepository : GenericRepository<Customer>,ICustomerRepository
    {
        public CustomerRepository(KaskoContext context) : base(context) {
        }
    }
}
