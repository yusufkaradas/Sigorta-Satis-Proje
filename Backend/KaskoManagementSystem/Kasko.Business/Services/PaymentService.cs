using Kasko.Business.DTOs.Payment;
using Kasko.Business.Exceptions;
using Kasko.Business.Interfaces;
using Kasko.Business.Services.Abstract;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Kasko.Entities.Enums;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Kasko.Business.Services.Concrete
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IPolicyRepository policyRepository, 
            IUnitOfWork unitOfWork, 
            INotificationService notificationService,
            IHttpContextAccessor httpContextAccessor)
        {
            _paymentRepository = paymentRepository;
            _policyRepository = policyRepository;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<PaymentDto> CreateAsync(
            PaymentCreateDto dto)
        {

            var policy = await _policyRepository
                .GetByIdAsync(dto.PolicyId);

            if (policy == null || policy.IsDeleted)
            {
                throw new NotFoundException(
                    "Poliçe bulunamadı.");
            }

            var hasSuccessfulPayment =
                await _paymentRepository
                    .HasSuccessfulPaymentAsync(dto.PolicyId);

            if (hasSuccessfulPayment)
            {
                throw new BadRequestException(
                    "Bu poliçe için zaten başarılı bir ödeme bulunmaktadır.");
            }

            if (policy.Status == PolicyStatus.Active)
            {
                throw new BadRequestException(
                    "Bu poliçe zaten aktiftir.");
            }

            string transactionNumber;

            do
            {
                transactionNumber =
                    $"PAY-{DateTime.UtcNow.Year}-" +
                    $"{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
            }
            while (await _paymentRepository
                .TransactionNumberExistsAsync(transactionNumber));


            var paymentStatus = dto.SimulateFailure
                ? PaymentStatus.Failed
                : PaymentStatus.Successful;

            var failureReason = dto.SimulateFailure
                ? "Simüle edilen ödeme hatası."
                : null;


            var payment = new Payment
            {
                Id = Guid.NewGuid(),

                PolicyId = policy.Id,

                TransactionNumber = transactionNumber,

                Amount = policy.PremiumAmount,

                Status = paymentStatus,

                PaymentDate = dto.SimulateFailure
                    ? null
                    : DateTime.UtcNow,

                FailureReason = failureReason,

                InstallmentCount = dto.InstallmentCount is 3 or 6 or 9 ? dto.InstallmentCount : 1,

                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {

                await _paymentRepository.AddAsync(payment);


                if (payment.Status == PaymentStatus.Successful)
                {
                    policy.Status = PolicyStatus.Active;
                    policy.UpdatedDate = DateTime.UtcNow;

                    await _policyRepository.UpdateAsync(policy);

                    await _notificationService.CreateAsync(
                        new Kasko.Business.DTOs.Notification.NotificationCreateDto
                        {
                            CustomerId = policy.CustomerId,
                            Type = "PAYMENT_SUCCESS",
                            Title = "Ödeme Başarılı",
                            Message = $"Poliçeniz {policy.PolicyNumber} numarasıyla aktif hale getirildi.",
                            RelatedEntityId = policy.Id
                        });
                }

                await _unitOfWork.SaveChangesAsync();
            });

            
            return new PaymentDto
            {
                Id = payment.Id,
                PolicyId = payment.PolicyId,
                InstallmentCount = payment.InstallmentCount,
                TransactionNumber = payment.TransactionNumber,
                Amount = payment.Amount,
                Status = payment.Status,
                PaymentDate = payment.PaymentDate,
                FailureReason = payment.FailureReason,
                CreatedDate = payment.CreatedDate
            };
        }

        public async Task<PaymentDto?> GetByIdAsync(Guid id)
        {
            var payment =
                await _paymentRepository
                    .GetByIdIncludingDetailsAsync(id);

            if (payment == null || payment.IsDeleted)
            {
                throw new NotFoundException(
                    "Ödeme bulunamadı.");
            }

            if (_httpContextAccessor.HttpContext?
                .User.IsInRole("Customer") == true)
            {
                var userIdValue =
                    _httpContextAccessor.HttpContext.User
                        .FindFirst(
                            System.Security.Claims.ClaimTypes.NameIdentifier)?
                        .Value;

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    throw new NotFoundException(
                        "Ödeme bulunamadı.");
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (
                    currentUser?.CustomerId == null ||
                    payment.Policy.CustomerId !=
                    currentUser.CustomerId.Value)
                {
                    throw new NotFoundException(
                        "Ödeme bulunamadı.");
                }
            }

            return new PaymentDto
            {
                Id = payment.Id,
                PolicyId = payment.PolicyId,
                InstallmentCount = payment.InstallmentCount,
                TransactionNumber =
                    payment.TransactionNumber,
                Amount = payment.Amount,
                Status = payment.Status,
                PaymentDate = payment.PaymentDate,
                FailureReason =
                    payment.FailureReason,
                CreatedDate =
                    payment.CreatedDate
            };
        }

        public async Task<IEnumerable<PaymentListDto>> GetAllAsync()
        {
            var payments =
                await _paymentRepository.GetAllAsync();

            if (_httpContextAccessor.HttpContext?.User.IsInRole("Customer") == true)
            {
                var userIdValue =
                    _httpContextAccessor.HttpContext.User
                        .FindFirst(ClaimTypes.NameIdentifier)?
                        .Value;

                if (!Guid.TryParse(userIdValue, out var userId))
                {
                    return Enumerable.Empty<PaymentListDto>();
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (currentUser?.CustomerId == null)
                {
                    return Enumerable.Empty<PaymentListDto>();
                }

                var policies =
                    await _policyRepository.GetAllAsync();

                var customerPolicyIds =
                    policies
                        .Where(x =>
                            !x.IsDeleted &&
                            x.CustomerId ==
                            currentUser.CustomerId.Value)
                        .Select(x => x.Id)
                        .ToHashSet();

                payments = payments
                    .Where(x =>
                        customerPolicyIds.Contains(x.PolicyId))
                    .ToList();
            }

            return payments
                .Where(x => !x.IsDeleted)
                .Select(x => new PaymentListDto
                {
                    Id = x.Id,

                    InstallmentCount = x.InstallmentCount,

                    PolicyId = x.PolicyId,

                    TransactionNumber =
                        x.TransactionNumber,

                    Amount = x.Amount,

                    Status = x.Status,

                    PaymentDate =
                        x.PaymentDate
                })
                .ToList();
        }
        public async Task DeleteAsync(
            Guid id,
            Guid? deletedBy)
        {
            var payment =
                await _paymentRepository.GetByIdAsync(id);

            if (payment == null || payment.IsDeleted)
            {
                throw new NotFoundException(
                    "Ödeme bulunamadı.");
            }

            payment.IsDeleted = true;
            payment.DeletedDate = DateTime.UtcNow;
            payment.DeletedBy = deletedBy;

            await _paymentRepository.UpdateAsync(payment);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}