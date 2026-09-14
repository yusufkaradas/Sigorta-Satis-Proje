using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.DataAccess.Repositories.Concrete
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly KaskoContext _context;
        public IUserRepository Users { get; }
        public IRoleRepository Roles { get; }

        public ICustomerRepository Customers { get; }

        public IVehicleRepository Vehicles { get; }

        public IQuoteRepository Quotes { get; }

        public IPolicyRepository Policies { get; }

        public IPaymentRepository Payments { get; }

        public ICoverageRepository Coverages { get; }
        public IVehicleValueCatalogRepository VehicleValueCatalogs { get; }

        public IPricingRuleRepository PricingRules { get; }

        public IQuoteCoverageRepository QuoteCoverages { get; }

        public IInsurancePackageRepository InsurancePackages { get; }

        public IPackageCoverageRepository PackageCoverages { get; }

        public IPreviousPolicyRepository PreviousPolicies { get; }

        public IGenericRepository<QuotePricingSnapshot> QuotePricingSnapshots { get; }

        public IPricingRuleChangeRequestRepository PricingRuleChangeRequests { get; }
        public INotificationRepository Notifications { get; }
        public UnitOfWork(KaskoContext context, IUserRepository userRepository, IRoleRepository roleRepository, 
            ICustomerRepository customerRepository, IVehicleRepository vehicleRepository,
            IQuoteRepository quoteRepository, IPolicyRepository policyRepository,
            IPaymentRepository paymentRepository, ICoverageRepository coverageRepository,
            IVehicleValueCatalogRepository vehicleValueCatalogRepository, IPricingRuleRepository pricingRuleRepository,
            IQuoteCoverageRepository quoteCoverageRepository, IInsurancePackageRepository insurancePackageRepository,
            IPackageCoverageRepository packageCoverageRepository, IPreviousPolicyRepository previousPolicyRepository,
            IGenericRepository<QuotePricingSnapshot> quotePricingSnapshotRepository, IPricingRuleChangeRequestRepository pricingRuleChangeRequestRepository
,           INotificationRepository notifications)
        {
            _context = context;
            Users = userRepository;
            Roles = roleRepository;
            Customers = customerRepository;
            QuoteCoverages = quoteCoverageRepository;
            PricingRules = pricingRuleRepository;
            VehicleValueCatalogs = vehicleValueCatalogRepository;
            Coverages = coverageRepository;
            Vehicles = vehicleRepository;
            Quotes = quoteRepository;
            Policies = policyRepository;
            Payments = paymentRepository;
            InsurancePackages = insurancePackageRepository;
            PackageCoverages = packageCoverageRepository;
            PreviousPolicies = previousPolicyRepository;
            QuotePricingSnapshots = quotePricingSnapshotRepository;
            PricingRuleChangeRequests = pricingRuleChangeRequestRepository;
            Notifications = notifications;
        }
        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
        public void Dispose()
        {
            _context.Dispose();
        }
        public async Task ExecuteInTransactionAsync(Func<Task> action)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                await action();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            
        }
    }
}