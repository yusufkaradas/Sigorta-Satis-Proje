using Kasko.Business.Pricing;

namespace Kasko.Business.Integrations.Insurer;

public class DemoInsurerCQuoteProvider : IInsurerQuoteProvider
{
    private readonly IPricingService _pricingService;

    public DemoInsurerCQuoteProvider(
        IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    public string ProviderName => "Provider C";

    public async Task<PricingCalculation> GetQuoteAsync(
        PricingRequest request,
        CancellationToken cancellationToken = default)
    {
        var calculation =
            await _pricingService.CalculateAsync(
                request,
                cancellationToken);

        const decimal providerFactor = 1.08m;

        calculation.FinalPremium =
            calculation.FinalPremium * providerFactor;

        calculation.TotalPremium =
            calculation.TotalPremium * providerFactor;

        return calculation;
    }
}