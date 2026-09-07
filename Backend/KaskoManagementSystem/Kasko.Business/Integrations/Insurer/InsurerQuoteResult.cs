using Kasko.Business.Pricing;

namespace Kasko.Business.Integrations.Insurer;

public class InsurerQuoteResult
{
    public string ProviderName { get; set; } = string.Empty;

    public PricingCalculation Calculation { get; set; } = new();

    public decimal Premium => Calculation.FinalPremium;
}