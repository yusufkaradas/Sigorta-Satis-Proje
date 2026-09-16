using Kasko.Business.DTOs.BrandSetting;

namespace Kasko.Business.Interfaces;

public interface IBrandSettingService
{
    Task<BrandSettingDto> GetAsync(CancellationToken cancellationToken = default);

    Task<BrandSettingDto> UpdateAsync(
        UpdateBrandSettingDto dto,
        CancellationToken cancellationToken = default);
}
