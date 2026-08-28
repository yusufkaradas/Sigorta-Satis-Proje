using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.DataAccess.Repositories.Abstract
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository Users { get; }

        IRoleRepository Roles { get; }

        ICustomerRepository Customers { get; }

        IVehicleRepository Vehicles { get; }

        IQuoteRepository Quotes { get; }

        IPolicyRepository Policies { get; }

        IPaymentRepository  Payments { get; }

        ICoverageRepository Coverages { get; }

        IVehicleValueCatalogRepository VehicleValueCatalogs { get; }

        IPricingRuleRepository PricingRules { get; }

        IQuoteCoverageRepository QuoteCoverages { get; }

        Task<int> SaveChangesAsync();

        Task ExecuteInTransactionAsync(Func<Task> action);
    }
}
