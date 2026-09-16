using Kasko.Business.DTOs.BrandSetting;
using Kasko.Business.Exceptions;
using Kasko.Business.Interfaces;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.Business.Services;

public class BrandSettingService : IBrandSettingService
{
    private const int MaxImageLength = 2_800_000;

    private readonly IGenericRepository<BrandSetting> _repository;

    private readonly IUnitOfWork _unitOfWork;

    public BrandSettingService(
        IGenericRepository<BrandSetting> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;

        _unitOfWork = unitOfWork;
    }

    public async Task<BrandSettingDto> GetAsync(
        CancellationToken cancellationToken = default)
    {
        var setting = await GetCurrentAsync();

        if (setting == null)
        {
            return new BrandSettingDto
            {
                CompanyName = "Şirket Adı",
                SystemName = "Sigorta Yönetim Sistemi"
            };
        }

        return Map(setting);
    }

    public async Task<BrandSettingDto> UpdateAsync(
        UpdateBrandSettingDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.CompanyName))
        {
            throw new BadRequestException("Şirket adı boş olamaz.");
        }

        if (string.IsNullOrWhiteSpace(dto.SystemName))
        {
            throw new BadRequestException("Sistem adı boş olamaz.");
        }

        ValidateImage(dto.LogoImage, "Logo");

        ValidateImage(dto.LoginImage, "Giriş görseli");

        var setting = await GetCurrentAsync();

        if (setting == null)
        {
            setting = new BrandSetting
            {
                Id = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                IsDeleted = false
            };

            await _repository.AddAsync(setting);
        }

        setting.CompanyName = dto.CompanyName.Trim();

        setting.SystemName = dto.SystemName.Trim();

        setting.LogoImage = dto.LogoImage;

        setting.LoginImage = dto.LoginImage;

        setting.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return Map(setting);
    }

    private static void ValidateImage(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException($"{field} için geçerli bir görsel yükleyin.");
        }

        if (value.Length > MaxImageLength)
        {
            throw new BadRequestException($"{field} çok büyük. Lütfen 2 MB altında bir görsel seçin.");
        }
    }

    private async Task<BrandSetting?> GetCurrentAsync()
    {
        var records = await _repository.GetAllAsync();

        return records
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedDate)
            .FirstOrDefault();
    }

    private static BrandSettingDto Map(BrandSetting setting)
    {
        return new BrandSettingDto
        {
            CompanyName = setting.CompanyName,
            SystemName = setting.SystemName,
            LogoImage = setting.LogoImage,
            LoginImage = setting.LoginImage,
            UpdatedDate = setting.UpdatedDate
        };
    }
}
