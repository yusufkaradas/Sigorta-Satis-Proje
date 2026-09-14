using Kasko.Business.DTOs.QuickQuote;

namespace Kasko.Business.Interfaces;

public interface IPolicyPdfService
{
    Task<byte[]> GenerateAsync(Guid policyId);
}