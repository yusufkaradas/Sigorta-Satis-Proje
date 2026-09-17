using Kasko.Business.DTOs.Tariff;

namespace Kasko.Business.Interfaces;

public interface ITariffService
{
    Task<TariffDto> GetTariffAsync();

    Task<IEnumerable<TariffChangeRequestDto>> GetRequestsAsync();

    Task<TariffChangeRequestDto> CreateRequestAsync(CreateTariffChangeRequestDto dto);

    Task ApproveAsync(Guid id, string? note);

    Task RejectAsync(Guid id, string? note);
}
