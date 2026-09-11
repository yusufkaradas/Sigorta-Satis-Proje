using Kasko.Business.DTOs.Customer;
using Kasko.Business.DTOs.QuickQuote;

namespace Kasko.Business.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<CustomerListDto>> GetAllAsync();

    Task<CustomerDto?> GetByIdAsync(Guid id);

    Task CreateAsync(CreateCustomerDto dto);

    Task UpdateAsync(UpdateCustomerDto dto);

    Task DeleteAsync(Guid id);

    Task<QuickQuoteCustomerLookupResponseDto>
        GetForQuickQuoteAsync(
            string identityNumber,
            string phoneNumber);
}