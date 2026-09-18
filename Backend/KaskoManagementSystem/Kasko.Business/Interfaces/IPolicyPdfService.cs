using Kasko.Business.DTOs.QuickQuote;

namespace Kasko.Business.Interfaces;

public interface IPolicyPdfService
{
    Task<byte[]> GenerateAsync(Guid policyId);

    Task<byte[]> GenerateQuoteAsync(Guid quoteId);

    Task<byte[]> GenerateTermsAsync(Guid policyId);

    Task<byte[]> GenerateEstimateAsync(Kasko.Business.DTOs.QuickQuote.QuickQuoteEstimateResultDto estimate, Kasko.Business.DTOs.QuickQuote.QuickQuoteEstimatePdfRequestDto request);
}