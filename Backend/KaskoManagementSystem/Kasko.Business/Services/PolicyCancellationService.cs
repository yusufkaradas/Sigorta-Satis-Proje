using System.Security.Claims;
using Kasko.Business.DTOs.PolicyCancellation;
using Kasko.Business.Exceptions;
using Kasko.Business.Interfaces;
using Kasko.Business.Services.Abstract;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Kasko.Entities.Enums;
using Microsoft.AspNetCore.Http;

namespace Kasko.Business.Services;

public class PolicyCancellationService : IPolicyCancellationService
{
    private readonly IGenericRepository<PolicyCancellationRequest> _repository;

    private readonly IUnitOfWork _unitOfWork;

    private readonly IPolicyService _policyService;

    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly INotificationService _notificationService;

    public PolicyCancellationService(
        IGenericRepository<PolicyCancellationRequest> repository,
        IUnitOfWork unitOfWork,
        IPolicyService policyService,
        IHttpContextAccessor httpContextAccessor,
        INotificationService notificationService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _policyService = policyService;
        _httpContextAccessor = httpContextAccessor;
        _notificationService = notificationService;
    }

    public async Task<IEnumerable<PolicyCancellationDto>> GetAllAsync()
    {
        var requests = (await _repository.GetAllAsync())
            .Where(x => !x.IsDeleted)
            .ToList();

        if (IsCustomer())
        {
            var customerId = await GetCurrentCustomerIdAsync();

            requests = requests
                .Where(x => x.CustomerId == customerId)
                .ToList();
        }

        var policyIds = requests.Select(x => x.PolicyId).Distinct().ToList();

        var customerIds = requests.Select(x => x.CustomerId).Distinct().ToList();

        var policies = (await _unitOfWork.Policies.FindAsync(x => policyIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);

        var customers = (await _unitOfWork.Customers.FindAsync(x => customerIds.Contains(x.Id)))
            .ToDictionary(x => x.Id, x => $"{x.FirstName} {x.LastName}");

        return requests
            .OrderByDescending(x => x.RequestedDate)
            .Select(x =>
            {
                policies.TryGetValue(x.PolicyId, out var policy);
                customers.TryGetValue(x.CustomerId, out var customerName);

                return new PolicyCancellationDto
                {
                    Id = x.Id,
                    PolicyId = x.PolicyId,
                    PolicyNumber = policy?.PolicyNumber ?? string.Empty,
                    CustomerId = x.CustomerId,
                    CustomerName = customerName ?? string.Empty,
                    PremiumAmount = policy?.PremiumAmount ?? 0,
                    PolicyStartDate = policy?.StartDate ?? default,
                    PolicyEndDate = policy?.EndDate ?? default,
                    Reason = x.Reason,
                    Status = x.Status,
                    RefundAmount = x.RefundAmount,
                    RemainingDays = x.RemainingDays,
                    RequestedDate = x.RequestedDate,
                    DecidedDate = x.DecidedDate,
                    DecisionNote = x.DecisionNote
                };
            })
            .ToList();
    }

    public async Task<PolicyCancellationDto> CreateAsync(CreatePolicyCancellationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length < 5)
        {
            throw new BadRequestException("Lütfen iptal nedeninizi kısaca yazın.");
        }

        var policy = await _policyService.GetByIdAsync(dto.PolicyId);

        if (policy == null)
        {
            throw new NotFoundException("Poliçe bulunamadı.");
        }

        if (policy.Status != PolicyStatus.Active)
        {
            throw new BadRequestException("Sadece aktif poliçeler için iptal talebi oluşturulabilir.");
        }

        var existing = await _repository.GetAllAsync();

        if (existing.Any(x =>
                !x.IsDeleted &&
                x.PolicyId == dto.PolicyId &&
                x.Status == CancellationRequestStatus.Pending))
        {
            throw new BadRequestException("Bu poliçe için bekleyen bir iptal talebiniz zaten var.");
        }

        var (refund, remainingDays) =
            CalculateRefund(policy.PremiumAmount, policy.StartDate, policy.EndDate, DateTime.UtcNow);

        var request = new PolicyCancellationRequest
        {
            Id = Guid.NewGuid(),
            PolicyId = policy.Id,
            CustomerId = policy.CustomerId,
            Reason = dto.Reason.Trim(),
            Status = CancellationRequestStatus.Pending,
            RefundAmount = refund,
            RemainingDays = remainingDays,
            RequestedDate = DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        await _repository.AddAsync(request);

        await _unitOfWork.SaveChangesAsync();

        return (await GetAllAsync()).First(x => x.Id == request.Id);
    }

    public async Task ApproveAsync(Guid id, string? note)
    {
        var request = await GetPendingAsync(id);

        var policy = await _policyService.GetByIdAsync(request.PolicyId);

        if (policy == null)
        {
            throw new NotFoundException("Poliçe bulunamadı.");
        }

        var (refund, remainingDays) =
            CalculateRefund(policy.PremiumAmount, policy.StartDate, policy.EndDate, DateTime.UtcNow);

        await _policyService.CancelAsync(request.PolicyId, GetCurrentUserId());

        request.Status = CancellationRequestStatus.Approved;
        request.RefundAmount = refund;
        request.RemainingDays = remainingDays;
        request.DecidedDate = DateTime.UtcNow;
        request.DecidedBy = GetCurrentUserId();
        request.DecisionNote = note?.Trim();
        request.UpdatedDate = DateTime.UtcNow;

        await _repository.UpdateAsync(request);

        await _notificationService.CreateAsync(new Kasko.Business.DTOs.Notification.NotificationCreateDto
        {
            CustomerId = request.CustomerId,
            Type = "CANCELLATION_APPROVED",
            Title = "İptal talebiniz onaylandı",
            Message = $"{policy.PolicyNumber} numaralı poliçeniz iptal edildi. İade tutarı: {refund:N2} ₺." +
                      (string.IsNullOrWhiteSpace(note) ? string.Empty : $" Not: {note.Trim()}"),
            RelatedEntityId = request.PolicyId
        });
    }

    public async Task RejectAsync(Guid id, string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            throw new BadRequestException("Reddetme nedenini müşteriye iletmek için bir not yazın.");
        }

        var request = await GetPendingAsync(id);

        request.Status = CancellationRequestStatus.Rejected;
        request.DecidedDate = DateTime.UtcNow;
        request.DecidedBy = GetCurrentUserId();
        request.DecisionNote = note.Trim();
        request.UpdatedDate = DateTime.UtcNow;

        await _repository.UpdateAsync(request);

        await _notificationService.CreateAsync(new Kasko.Business.DTOs.Notification.NotificationCreateDto
        {
            CustomerId = request.CustomerId,
            Type = "CANCELLATION_REJECTED",
            Title = "İptal talebiniz reddedildi",
            Message = $"Poliçeniz aktif kalmaya devam ediyor. Gerekçe: {note.Trim()}",
            RelatedEntityId = request.PolicyId
        });
    }

    public static (decimal Refund, int RemainingDays) CalculateRefund(
        decimal premium,
        DateTime startDate,
        DateTime endDate,
        DateTime now)
    {
        var totalDays = Math.Max(1, (endDate.Date - startDate.Date).Days);

        var remainingDays = Math.Clamp((endDate.Date - now.Date).Days, 0, totalDays);

        var refund = Math.Round(premium * remainingDays / totalDays, 2, MidpointRounding.AwayFromZero);

        return (refund, remainingDays);
    }

    private async Task<PolicyCancellationRequest> GetPendingAsync(Guid id)
    {
        var request = await _repository.GetByIdAsync(id);

        if (request == null || request.IsDeleted)
        {
            throw new NotFoundException("İptal talebi bulunamadı.");
        }

        if (request.Status != CancellationRequestStatus.Pending)
        {
            throw new BadRequestException("Bu talep daha önce sonuçlandırılmış.");
        }

        return request;
    }

    private bool IsCustomer()
    {
        return _httpContextAccessor.HttpContext?.User.IsInRole("Customer") == true;
    }

    private Guid? GetCurrentUserId()
    {
        var value = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(value, out var id) ? id : null;
    }

    private async Task<Guid?> GetCurrentCustomerIdAsync()
    {
        var userId = GetCurrentUserId();

        if (userId == null)
        {
            return null;
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);

        return user?.CustomerId;
    }
}
