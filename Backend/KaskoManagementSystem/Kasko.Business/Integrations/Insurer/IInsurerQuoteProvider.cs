using Kasko.Business.Pricing;

namespace Kasko.Business.Integrations.Insurer;

public interface IInsurerQuoteProvider
{
    string ProviderName { get; }

    Task<PricingCalculation> GetQuoteAsync(
        PricingRequest request,
        CancellationToken cancellationToken = default);
}