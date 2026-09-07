using Kasko.Business.Pricing;

namespace Kasko.Business.Integrations.Insurer;

public class DemoInsurerBQuoteProvider : IInsurerQuoteProvider
{
    private readonly IPricingService _pricingService;

    public DemoInsurerBQuoteProvider(
        IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    public string ProviderName => "Provider B";

    public async Task<PricingCalculation> GetQuoteAsync(
        PricingRequest request,
        CancellationToken cancellationToken = default)
    {
        var calculation =
            await _pricingService.CalculateAsync(
                request,
                cancellationToken);

        const decimal providerFactor = 1.03m;

        calculation.FinalPremium =
            calculation.FinalPremium * providerFactor;

        calculation.TotalPremium =
            calculation.TotalPremium * providerFactor;

        return calculation;
    }
}