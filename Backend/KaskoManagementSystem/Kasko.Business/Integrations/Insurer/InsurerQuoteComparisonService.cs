using Kasko.Business.Pricing;

namespace Kasko.Business.Integrations.Insurer;

public class InsurerQuoteComparisonService
{
    private readonly IEnumerable<IInsurerQuoteProvider> _providers;

    public InsurerQuoteComparisonService(
        IEnumerable<IInsurerQuoteProvider> providers)
    {
        _providers = providers;
    }

    public async Task<IReadOnlyList<InsurerQuoteResult>> CompareAsync(
        PricingRequest request,
        CancellationToken cancellationToken = default)
    {
        var results = new List<InsurerQuoteResult>();

        foreach (var provider in _providers)
        {
            var calculation =
                await provider.GetQuoteAsync(
                    request,
                    cancellationToken);

            results.Add(
                new InsurerQuoteResult
                {
                    ProviderName = provider.ProviderName,
                    Calculation = calculation
                });
        }

        return results
            .OrderBy(x => x.Premium)
            .ToList();
    }
}