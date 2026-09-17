using Kasko.Business.DTOs.PolicyCancellation;

namespace Kasko.Business.Interfaces;

public interface IPolicyCancellationService
{
    Task<IEnumerable<PolicyCancellationDto>> GetAllAsync();

    Task<PolicyCancellationDto> CreateAsync(CreatePolicyCancellationDto dto);

    Task ApproveAsync(Guid id, string? note);

    Task RejectAsync(Guid id, string? note);
}
