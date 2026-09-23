using Kasko.Business.DTOs.PricingRuleChangeRequest;
using Kasko.Business.Exceptions;
using Kasko.Business.Interfaces;
using Kasko.Business.Notifications;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.Business.Services;

public class PricingRuleChangeRequestService
    : IPricingRuleChangeRequestService
{
    private readonly IUnitOfWork _unitOfWork;

    public PricingRuleChangeRequestService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PricingRuleChangeRequestDto> CreateAsync(
        CreatePricingRuleChangeRequestDto dto,
        Guid requestedBy)
    {
        var rule =
            await _unitOfWork.PricingRules
                .GetByIdAsync(dto.PricingRuleId);

        if (rule == null || rule.IsDeleted)
        {
            throw new NotFoundException(
                "Fiyat kuralı bulunamadı.");
        }

        if (dto.NewValue < 0)
        {
            throw new BadRequestException(
                "Yeni değer negatif olamaz.");
        }

        if (dto.EffectiveFrom <= DateTime.UtcNow)
        {
            throw new BadRequestException(
                "Geçerlilik başlangıcı ileri bir tarih olmalıdır.");
        }

        var request = new PricingRuleChangeRequest
        {
            Id = Guid.NewGuid(),
            PricingRuleId = rule.Id,
            OldValue = rule.Value,
            NewValue = dto.NewValue,
            Reason = dto.Reason,
            RequestedBy = requestedBy,
            RequestedDate = DateTime.UtcNow,
            Status = "Pending",
            EffectiveFrom = dto.EffectiveFrom,
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.PricingRuleChangeRequests
            .AddAsync(request);

        await NotificationWriter.ToRolesAsync(
            _unitOfWork,
            NotificationWriter.AdminOnly,
            "PRICING_REQUEST_CREATED",
            "Yeni fiyat değişikliği talebi",
            $"{rule.Name}: {request.OldValue:0.####} → {request.NewValue:0.####}, {request.EffectiveFrom:dd.MM.yyyy} tarihinden itibaren. Gerekçe: {request.Reason}",
            request.Id,
            requestedBy);

        await _unitOfWork.SaveChangesAsync();

        return MapToDto(request);
    }

    public async Task<IEnumerable<PricingRuleChangeRequestDto>>
        GetAllAsync()
    {
        var requests =
            await _unitOfWork.PricingRuleChangeRequests
                .GetAllAsync();

        return requests.Select(MapToDto);
    }

    public async Task<PricingRuleChangeRequestDto?>
        GetByIdAsync(Guid id)
    {
        var request =
            await _unitOfWork.PricingRuleChangeRequests
                .GetByIdAsync(id);

        if (request == null || request.IsDeleted)
        {
            return null;
        }

        return MapToDto(request);
    }

    public async Task ApproveAsync(
    Guid id,
    Guid approvedBy)
    {
        var request =
            await _unitOfWork.PricingRuleChangeRequests
                .GetByIdAsync(id);

        if (request == null || request.IsDeleted)
        {
            throw new NotFoundException(
                "Fiyat değişiklik talebi bulunamadı.");
        }

        if (request.Status != "Pending")
        {
            throw new BadRequestException(
                "Yalnızca onay bekleyen talepler onaylanabilir.");
        }

        var currentRule =
            await _unitOfWork.PricingRules
                .GetByIdAsync(request.PricingRuleId);

        if (currentRule == null || currentRule.IsDeleted)
        {
            throw new NotFoundException(
                "Fiyat kuralı bulunamadı.");
        }

        var allRules =
            await _unitOfWork.PricingRules.GetAllAsync();

        var latestVersion =
            allRules
                .Where(x =>
                    !x.IsDeleted &&
                    x.Code == currentRule.Code)
                .Select(x => x.Version)
                .DefaultIfEmpty(0)
                .Max();

        if (request.EffectiveFrom <= currentRule.EffectiveFrom)
        {
            throw new BadRequestException(
                "Yeni sürümün geçerlilik başlangıcı mevcut sürümden daha ileri bir tarih olmalıdır.");
        }

        currentRule.EffectiveUntil =
            request.EffectiveFrom.AddTicks(-1);

        var newRule = new PricingRule
        {
            Id = Guid.NewGuid(),

            Code = currentRule.Code,
            Name = currentRule.Name,
            Description = currentRule.Description,

            Value = request.NewValue,

            IsActive = true,
            IsDeleted = false,

            Version = latestVersion + 1,

            EffectiveFrom = request.EffectiveFrom,
            EffectiveUntil = null,

            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.PricingRules
            .UpdateAsync(currentRule);

        await _unitOfWork.PricingRules
            .AddAsync(newRule);

        request.Status = "Approved";
        request.ApprovedBy = approvedBy;
        request.ApprovedDate = DateTime.UtcNow;
        request.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.PricingRuleChangeRequests
            .UpdateAsync(request);

        if (request.RequestedBy != approvedBy)
        {
            await NotificationWriter.ToUserAsync(
                _unitOfWork,
                request.RequestedBy,
                "PRICING_REQUEST_APPROVED",
                "Fiyat talebiniz onaylandı",
                $"{currentRule.Name} için {request.NewValue:0.####} değeri {request.EffectiveFrom:dd.MM.yyyy} tarihinden itibaren geçerli olacak.",
                request.Id);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RejectAsync(
        Guid id,
        Guid approvedBy,
        string? reason = null)
    {
        var request =
            await _unitOfWork.PricingRuleChangeRequests
                .GetByIdAsync(id);

        if (request == null || request.IsDeleted)
        {
            throw new NotFoundException(
                "Fiyat değişiklik talebi bulunamadı.");
        }

        if (request.Status != "Pending")
        {
            throw new BadRequestException(
                "Yalnızca onay bekleyen talepler reddedilebilir.");
        }

        request.Status = "Rejected";
        request.RejectReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        request.ApprovedBy = approvedBy;
        request.ApprovedDate = DateTime.UtcNow;
        request.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.PricingRuleChangeRequests
            .UpdateAsync(request);

        if (request.RequestedBy != approvedBy)
        {
            await NotificationWriter.ToUserAsync(
                _unitOfWork,
                request.RequestedBy,
                "PRICING_REQUEST_REJECTED",
                "Fiyat talebiniz reddedildi",
                request.RejectReason == null
                    ? "Fiyat değişikliği talebiniz uygun bulunmadı."
                    : $"Fiyat değişikliği talebiniz uygun bulunmadı. Gerekçe: {request.RejectReason}",
                request.Id);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private static PricingRuleChangeRequestDto MapToDto(
        PricingRuleChangeRequest request)
    {
        return new PricingRuleChangeRequestDto
        {
            Id = request.Id,
            PricingRuleId = request.PricingRuleId,
            OldValue = request.OldValue,
            NewValue = request.NewValue,
            Reason = request.Reason,
            RequestedBy = request.RequestedBy,
            RequestedDate = request.RequestedDate,
            ApprovedBy = request.ApprovedBy,
            ApprovedDate = request.ApprovedDate,
            Status = request.Status,
            EffectiveFrom = request.EffectiveFrom,
            RejectReason = request.RejectReason
        };
    }
}