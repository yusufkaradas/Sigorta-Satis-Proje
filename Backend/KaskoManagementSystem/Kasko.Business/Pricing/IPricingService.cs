namespace Kasko.Business.Pricing;

public interface IPricingService
{
    Task<PricingCalculation> CalculateAsync(
        decimal marketValue,
        int modelYear,
        IReadOnlyCollection<Guid> coverageIds,
        CancellationToken cancellationToken = default);
}