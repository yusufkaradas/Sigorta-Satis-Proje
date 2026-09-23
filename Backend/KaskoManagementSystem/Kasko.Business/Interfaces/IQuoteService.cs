using Kasko.Business.DTOs.Quote;
using Kasko.Business.Pricing;
using Kasko.Entities.Enums;

namespace Kasko.Business.Services
{
    public interface IQuoteService
    {
        Task<IEnumerable<QuoteListDto>> GetAllAsync();

        Task<QuoteDto?> GetByIdAsync(Guid id);

        Task<QuoteDto> CreateAsync(CreateQuoteDto dto);

        Task UpdateAsync(Guid id, UpdateQuoteDto dto);

        Task DeleteAsync(Guid id);

        Task ChangeStatusAsync(Guid id, QuoteStatus newStatus);

        Task<PricingCalculation> CalculateAsync(CreateQuoteDto dto);

        Task<QuoteEligibilityDto> CheckVehicleEligibilityAsync(Guid vehicleId);

        Task EnsureNoOpenQuoteAsync(Guid vehicleId);

        Task<QuoteEligibilityDto> CheckPlateEligibilityAsync(string plateNumber);
    }
}