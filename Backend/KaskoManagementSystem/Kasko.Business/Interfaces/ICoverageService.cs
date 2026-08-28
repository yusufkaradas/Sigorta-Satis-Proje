using Kasko.Business.DTOs.Coverage;

namespace Kasko.Business.Services.Abstract;

public interface ICoverageService
{
    Task<IEnumerable<CoverageDto>> GetAllAsync();

    Task<CoverageDto?> GetByIdAsync(Guid id);

    Task<CoverageDto> CreateAsync(
        CreateCoverageDto dto);

    Task UpdateAsync(
        UpdateCoverageDto dto);

    Task DeleteAsync(Guid id);
}