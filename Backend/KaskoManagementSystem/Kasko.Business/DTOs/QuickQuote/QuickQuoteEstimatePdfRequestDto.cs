namespace Kasko.Business.DTOs.QuickQuote;

public class QuickQuoteEstimatePdfRequestDto : QuickQuoteEstimateRequestDto
{
    public string? PlateNumber { get; set; }

    public string? Reference { get; set; }
}
