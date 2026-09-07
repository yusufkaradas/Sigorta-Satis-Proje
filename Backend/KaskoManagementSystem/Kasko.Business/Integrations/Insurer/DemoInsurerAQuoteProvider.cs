using Kasko.Business.Pricing;

namespace Kasko.Business.Integrations.Insurer;

public class DemoInsurerAQuoteProvider : IInsurerQuoteProvider
{
    private readonly IPricingService _pricingService;

    public DemoInsurerAQuoteProvider(
        IPricingService pricingService)
    {
        _pricingService = pricingService;
    }

    public string ProviderName => "Provider A";

    public async Task<PricingCalculation> GetQuoteAsync(
        PricingRequest request,
        CancellationToken cancellationToken = default)
    {
        var calculation =
            await _pricingService.CalculateAsync(
                request,
                cancellationToken);

        const decimal providerFactor = 0.98m;

        calculation.FinalPremium =
            calculation.FinalPremium * providerFactor;

        calculation.TotalPremium =
            calculation.TotalPremium * providerFactor;

        return calculation;
    }
}