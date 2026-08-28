using Kasko.Business.DTOs.Payment;

namespace Kasko.Business.Services.Abstract
{
    public interface IPaymentService
    {
        Task<PaymentDto> CreateAsync(PaymentCreateDto dto);

        Task<PaymentDto?> GetByIdAsync(Guid id);

        Task<IEnumerable<PaymentListDto>> GetAllAsync();

        Task DeleteAsync(Guid id, Guid? deletedBy);
    }
}