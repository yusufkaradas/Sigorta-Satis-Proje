namespace Kasko.Business.Pricing;

public interface IPricingService
{
    Task<PricingCalculation> CalculateAsync(
        PricingRequest request,
        CancellationToken cancellationToken = default);
}