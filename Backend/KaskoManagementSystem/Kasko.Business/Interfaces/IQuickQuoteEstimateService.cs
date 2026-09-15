using Kasko.Business.DTOs.QuickQuote;

namespace Kasko.Business.Interfaces;

public interface IQuickQuoteEstimateService
{
    Task<QuickQuoteEstimateResultDto> EstimateAsync(
        QuickQuoteEstimateRequestDto request,
        CancellationToken cancellationToken = default);
}
