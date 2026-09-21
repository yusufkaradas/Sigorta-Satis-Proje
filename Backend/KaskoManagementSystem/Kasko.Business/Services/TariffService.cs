using System.Security.Claims;
using Kasko.Business.DTOs.Tariff;
using Kasko.Business.Exceptions;
using Kasko.Business.Interfaces;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Kasko.Entities.Enums;
using Microsoft.AspNetCore.Http;

namespace Kasko.Business.Services;

public class TariffService : ITariffService
{
    private const string PackageTarget = "Package";

    private const string CoverageTarget = "Coverage";

    private const string OptionTarget = "Option";

    private readonly IUnitOfWork _unitOfWork;

    private readonly IGenericRepository<CoverageOption> _options;

    private readonly IGenericRepository<TariffChangeRequest> _requests;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public TariffService(
        IUnitOfWork unitOfWork,
        IGenericRepository<CoverageOption> options,
        IGenericRepository<TariffChangeRequest> requests,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _options = options;
        _requests = requests;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TariffDto> GetTariffAsync()
    {
        var packages = (await _unitOfWork.InsurancePackages.FindAsync(x => !x.IsDeleted && x.IsActive))
            .OrderBy(x => x.Factor)
            .ToList();

        var coverages = (await _unitOfWork.Coverages.FindAsync(x => !x.IsDeleted && x.IsActive))
            .ToList();

        var packageCoverages = (await _unitOfWork.PackageCoverages.FindAsync(x => !x.IsDeleted))
            .ToList();

        var options = (await _options.FindAsync(x => !x.IsDeleted))
            .ToList();

        var coverageNames = coverages.ToDictionary(x => x.Id, x => x.Name);

        return new TariffDto
        {
            Packages = packages
                .Select(package => new TariffPackageDto
                {
                    Id = package.Id,
                    Code = package.Code,
                    Name = package.Name,
                    Description = package.Description,
                    Factor = package.Factor,
                    CoverageNames = packageCoverages
                        .Where(x => x.InsurancePackageId == package.Id && coverageNames.ContainsKey(x.CoverageId))
                        .Select(x => coverageNames[x.CoverageId])
                        .OrderBy(x => x)
                        .ToList(),
                    CoverageIds = packageCoverages
                        .Where(x => x.InsurancePackageId == package.Id)
                        .Select(x => x.CoverageId)
                        .ToList()
                })
                .ToList(),
            Coverages = coverages
                .OrderBy(x => x.IsRequired ? 0 : 1)
                .ThenBy(x => x.Name)
                .Select(coverage => new TariffCoverageDto
                {
                    Id = coverage.Id,
                    IsRequired = coverage.IsRequired,
                    Name = coverage.Name,
                    Description = coverage.Description,
                    PricingType = coverage.PricingType,
                    BasePrice = coverage.BasePrice,
                    Rate = coverage.Rate,
                    Options = options
                        .Where(x => x.CoverageId == coverage.Id)
                        .OrderBy(x => x.SortOrder)
                        .Select(x => new TariffOptionDto
                        {
                            Id = x.Id,
                            Name = x.Name,
                            ExtraPrice = x.ExtraPrice,
                            IsDefault = x.IsDefault
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    public async Task SetPackageCoverageAsync(Guid packageId, Guid coverageId, bool included)
    {
        var package = await _unitOfWork.InsurancePackages.GetByIdAsync(packageId);

        if (package == null || package.IsDeleted)
        {
            throw new NotFoundException("Paket bulunamadı.");
        }

        var coverage = await _unitOfWork.Coverages.GetByIdAsync(coverageId);

        if (coverage == null || coverage.IsDeleted)
        {
            throw new NotFoundException("Teminat bulunamadı.");
        }

        if (!included && coverage.IsRequired)
        {
            throw new BadRequestException($"{coverage.Name} zorunlu teminattır, paketten çıkarılamaz.");
        }

        var existing = (await _unitOfWork.PackageCoverages.FindAsync(x =>
                x.InsurancePackageId == packageId &&
                x.CoverageId == coverageId))
            .ToList();

        var active = existing.FirstOrDefault(x => !x.IsDeleted);

        if (included)
        {
            if (active != null)
            {
                return;
            }

            var restored = existing.FirstOrDefault();

            if (restored != null)
            {
                restored.IsDeleted = false;
                restored.DeletedDate = null;
                restored.IsDefault = true;
                await _unitOfWork.PackageCoverages.UpdateAsync(restored);
            }
            else
            {
                await _unitOfWork.PackageCoverages.AddAsync(new PackageCoverage
                {
                    InsurancePackageId = packageId,
                    CoverageId = coverageId,
                    IsDefault = true
                });
            }
        }
        else
        {
            if (active == null)
            {
                return;
            }

            var remaining = (await _unitOfWork.PackageCoverages.FindAsync(x =>
                    x.InsurancePackageId == packageId && !x.IsDeleted))
                .Count();

            if (remaining <= 1)
            {
                throw new BadRequestException("Pakette en az bir teminat bulunmalıdır.");
            }

            active.IsDeleted = true;
            active.DeletedDate = DateTime.UtcNow;
            await _unitOfWork.PackageCoverages.UpdateAsync(active);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdatePackageInfoAsync(Guid packageId, string? description)
    {
        var package = await _unitOfWork.InsurancePackages.GetByIdAsync(packageId);

        if (package == null || package.IsDeleted)
        {
            throw new NotFoundException("Paket bulunamadı.");
        }

        var text = description?.Trim();

        if (string.IsNullOrEmpty(text) || text.Length > 250)
        {
            throw new BadRequestException("Paket açıklaması 1-250 karakter olmalıdır.");
        }

        package.Description = text;
        await _unitOfWork.InsurancePackages.UpdateAsync(package);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<TariffChangeRequestDto>> GetRequestsAsync()
    {
        return (await _requests.FindAsync(x => !x.IsDeleted))
            .OrderByDescending(x => x.RequestedDate)
            .Select(ToDto)
            .ToList();
    }

    public async Task<TariffChangeRequestDto> CreateRequestAsync(CreateTariffChangeRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length < 5)
        {
            throw new BadRequestException("Değişikliğin gerekçesini kısaca yazın.");
        }

        var (targetName, oldValue) = await ResolveTargetAsync(dto.TargetType, dto.TargetId, dto.Field);

        ValidateValue(dto.Field, dto.NewValue);

        if (oldValue == dto.NewValue)
        {
            throw new BadRequestException("Yeni değer mevcut değerle aynı.");
        }

        var hasPending = await _requests.AnyAsync(x =>
            !x.IsDeleted &&
            x.Status == TariffRequestStatus.Pending &&
            x.TargetId == dto.TargetId &&
            x.Field == dto.Field);

        if (hasPending)
        {
            throw new BadRequestException("Bu kalem için onay bekleyen bir talep zaten var.");
        }

        var userId = GetCurrentUserId();

        var request = new TariffChangeRequest
        {
            Id = Guid.NewGuid(),
            TargetType = dto.TargetType,
            TargetId = dto.TargetId,
            TargetName = targetName,
            Field = dto.Field,
            OldValue = oldValue,
            NewValue = dto.NewValue,
            Reason = dto.Reason.Trim(),
            Status = TariffRequestStatus.Pending,
            RequestedBy = userId,
            RequestedByName = await GetUserNameAsync(userId),
            RequestedDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow
        };

        await _requests.AddAsync(request);

        if (IsAdmin())
        {
            await ApplyAsync(request);

            request.Status = TariffRequestStatus.Approved;
            request.DecidedBy = userId;
            request.DecidedDate = DateTime.UtcNow;
            request.DecisionNote = "Yönetici tarafından doğrudan uygulandı.";
        }

        await _unitOfWork.SaveChangesAsync();

        return ToDto(request);
    }

    public async Task ApproveAsync(Guid id, string? note)
    {
        var request = await GetPendingAsync(id);

        await ApplyAsync(request);

        request.Status = TariffRequestStatus.Approved;
        request.DecidedBy = GetCurrentUserId();
        request.DecidedDate = DateTime.UtcNow;
        request.DecisionNote = note?.Trim();
        request.UpdatedDate = DateTime.UtcNow;

        await _requests.UpdateAsync(request);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RejectAsync(Guid id, string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            throw new BadRequestException("Reddetme nedenini yazın.");
        }

        var request = await GetPendingAsync(id);

        request.Status = TariffRequestStatus.Rejected;
        request.DecidedBy = GetCurrentUserId();
        request.DecidedDate = DateTime.UtcNow;
        request.DecisionNote = note.Trim();
        request.UpdatedDate = DateTime.UtcNow;

        await _requests.UpdateAsync(request);

        await _unitOfWork.SaveChangesAsync();
    }

    private async Task ApplyAsync(TariffChangeRequest request)
    {
        switch (request.TargetType)
        {
            case PackageTarget:
                var package = await _unitOfWork.InsurancePackages.GetByIdAsync(request.TargetId)
                    ?? throw new NotFoundException("Paket bulunamadı.");
                package.Factor = request.NewValue;
                package.UpdatedDate = DateTime.UtcNow;
                await _unitOfWork.InsurancePackages.UpdateAsync(package);
                break;

            case CoverageTarget:
                var coverage = await _unitOfWork.Coverages.GetByIdAsync(request.TargetId)
                    ?? throw new NotFoundException("Teminat bulunamadı.");
                if (request.Field == "Rate")
                {
                    coverage.Rate = request.NewValue;
                }
                else
                {
                    coverage.BasePrice = request.NewValue;
                }
                coverage.UpdatedDate = DateTime.UtcNow;
                await _unitOfWork.Coverages.UpdateAsync(coverage);
                break;

            case OptionTarget:
                var option = await _options.GetByIdAsync(request.TargetId)
                    ?? throw new NotFoundException("Limit seçeneği bulunamadı.");
                option.ExtraPrice = request.NewValue;
                option.UpdatedDate = DateTime.UtcNow;
                await _options.UpdateAsync(option);
                break;
        }
    }

    private async Task<(string Name, decimal Value)> ResolveTargetAsync(string targetType, Guid targetId, string field)
    {
        switch (targetType)
        {
            case PackageTarget when field == "Factor":
                var package = await _unitOfWork.InsurancePackages.GetByIdAsync(targetId);
                if (package == null || package.IsDeleted)
                {
                    throw new NotFoundException("Paket bulunamadı.");
                }
                return (package.Name, package.Factor);

            case CoverageTarget when field == "BasePrice" || field == "Rate":
                var coverage = await _unitOfWork.Coverages.GetByIdAsync(targetId);
                if (coverage == null || coverage.IsDeleted)
                {
                    throw new NotFoundException("Teminat bulunamadı.");
                }
                return (coverage.Name, field == "Rate" ? coverage.Rate ?? 0m : coverage.BasePrice);

            case OptionTarget when field == "ExtraPrice":
                var option = await _options.GetByIdAsync(targetId);
                if (option == null || option.IsDeleted)
                {
                    throw new NotFoundException("Limit seçeneği bulunamadı.");
                }
                var parent = await _unitOfWork.Coverages.GetByIdAsync(option.CoverageId);
                return ($"{parent?.Name} · {option.Name}", option.ExtraPrice);

            default:
                throw new BadRequestException("Değiştirilmek istenen tarife kalemi geçersiz.");
        }
    }

    private static void ValidateValue(string field, decimal value)
    {
        var valid = field switch
        {
            "Factor" => value >= 0.5m && value <= 2m,
            "Rate" => value >= 0m && value <= 5m,
            _ => value >= 0m && value <= 100000m
        };

        if (!valid)
        {
            throw new BadRequestException(field switch
            {
                "Factor" => "Paket katsayısı 0,50 ile 2,00 arasında olmalıdır.",
                "Rate" => "Oran %0 ile %5 arasında olmalıdır.",
                _ => "Tutar 0 ile 100.000 ₺ arasında olmalıdır."
            });
        }
    }

    private async Task<TariffChangeRequest> GetPendingAsync(Guid id)
    {
        var request = await _requests.GetByIdAsync(id);

        if (request == null || request.IsDeleted)
        {
            throw new NotFoundException("Tarife talebi bulunamadı.");
        }

        if (request.Status != TariffRequestStatus.Pending)
        {
            throw new BadRequestException("Bu talep daha önce sonuçlandırılmış.");
        }

        return request;
    }

    private static TariffChangeRequestDto ToDto(TariffChangeRequest x)
    {
        return new TariffChangeRequestDto
        {
            Id = x.Id,
            TargetType = x.TargetType,
            TargetId = x.TargetId,
            TargetName = x.TargetName,
            Field = x.Field,
            OldValue = x.OldValue,
            NewValue = x.NewValue,
            Reason = x.Reason,
            Status = x.Status,
            RequestedByName = x.RequestedByName,
            RequestedDate = x.RequestedDate,
            DecidedDate = x.DecidedDate,
            DecisionNote = x.DecisionNote
        };
    }

    private bool IsAdmin()
    {
        return _httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true;
    }

    private Guid? GetCurrentUserId()
    {
        var value = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(value, out var id) ? id : null;
    }

    private async Task<string> GetUserNameAsync(Guid? userId)
    {
        if (userId == null)
        {
            return string.Empty;
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);

        return user == null ? string.Empty : $"{user.FirstName} {user.LastName}".Trim();
    }
}
