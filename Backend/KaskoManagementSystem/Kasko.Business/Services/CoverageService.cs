using Kasko.Business.DTOs.Coverage;
using Kasko.Business.Exceptions;
using Kasko.Business.Services.Abstract;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.Business.Services;

public class CoverageService : ICoverageService
{
    private readonly IUnitOfWork _unitOfWork;

    public CoverageService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<CoverageDto>> GetAllAsync()
    {
        var coverages =
            await _unitOfWork.Coverages
                .GetAllAsync();

        return coverages
            .Select(x => new CoverageDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                BasePrice = x.BasePrice,
                PricingType = x.PricingType,
                Rate = x.Rate,
                DefaultLimit = x.DefaultLimit,
                IsRequired = x.IsRequired,
                IsActive = x.IsActive

            })
            .ToList();
    }


    public async Task<CoverageDto?> GetByIdAsync(
        Guid id)
    {
        var coverage =
            await _unitOfWork.Coverages
                .GetByIdAsync(id);

        if (coverage == null)
        {
            return null;
        }

        return new CoverageDto
        {
            Id = coverage.Id,
            Name = coverage.Name,
            Description = coverage.Description,
            BasePrice = coverage.BasePrice,
            PricingType = coverage.PricingType,
            Rate = coverage.Rate,
            DefaultLimit = coverage.DefaultLimit,
            IsRequired = coverage.IsRequired,
            IsActive = coverage.IsActive
        };
    }


    public async Task<CoverageDto> CreateAsync(
    CreateCoverageDto dto)
    {
        var nameExists =
            await _unitOfWork.Coverages
                .AnyAsync(x =>
                    x.Name == dto.Name);

        if (nameExists)
        {
            throw new BadRequestException(
                "Bu teminat zaten mevcut.");
        }

        var coverage = new Coverage
        {
            Id = Guid.NewGuid(),

            Name = dto.Name.Trim(),

            Description =
                string.IsNullOrWhiteSpace(dto.Description)
                    ? null
                    : dto.Description.Trim(),

            PricingType = dto.PricingType,

            BasePrice = dto.BasePrice,

            Rate = dto.Rate,

            DefaultLimit = dto.DefaultLimit,

            IsRequired = dto.IsRequired,

            IsActive = true,

            IsDeleted = false,

            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.Coverages
            .AddAsync(coverage);

        await _unitOfWork
            .SaveChangesAsync();

        return new CoverageDto
        {
            Id = coverage.Id,
            Name = coverage.Name,
            Description = coverage.Description,
            PricingType = coverage.PricingType,
            BasePrice = coverage.BasePrice,
            Rate = coverage.Rate,
            DefaultLimit = coverage.DefaultLimit,
            IsRequired = coverage.IsRequired,
            IsActive = coverage.IsActive
        };
    }


    public async Task UpdateAsync(
        UpdateCoverageDto dto)
    {
        var coverage =
            await _unitOfWork.Coverages
                .GetByIdAsync(dto.Id);

        if (coverage == null)
        {
            throw new NotFoundException(
                "Teminat bulunamadı.");
        }

        var nameExists =
            await _unitOfWork.Coverages
                .AnyAsync(x =>
                    x.Name == dto.Name &&
                    x.Id != dto.Id);

        if (nameExists)
        {
            throw new BadRequestException(
                "Bu teminat zaten mevcut.");
        }

        coverage.Name =
            dto.Name.Trim();

        coverage.Description =
            string.IsNullOrWhiteSpace(dto.Description)
                ? null
                : dto.Description.Trim();

        coverage.BasePrice =
            dto.BasePrice;

        coverage.IsActive =
            dto.IsActive;

        coverage.UpdatedDate =
            DateTime.UtcNow;
        coverage.PricingType =
    dto.PricingType;

        coverage.BasePrice =
            dto.BasePrice;

        coverage.Rate =
            dto.Rate;

        coverage.DefaultLimit =
            dto.DefaultLimit;

        coverage.IsRequired =
            dto.IsRequired;

        coverage.IsActive =
            dto.IsActive;

        await _unitOfWork.Coverages
            .UpdateAsync(coverage);

        await _unitOfWork
            .SaveChangesAsync();
    }


    public async Task DeleteAsync(Guid id)
    {
        var coverage =
            await _unitOfWork.Coverages
                .GetByIdAsync(id);

        if (coverage == null)
        {
            throw new NotFoundException(
                "Teminat bulunamadı.");
        }

        coverage.IsDeleted = true;

        coverage.DeletedDate =
            DateTime.UtcNow;

        await _unitOfWork.Coverages
            .UpdateAsync(coverage);

        await _unitOfWork
            .SaveChangesAsync();
    }
}