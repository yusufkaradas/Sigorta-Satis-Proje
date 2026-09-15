using Kasko.Business.DTOs.Payment;
using Kasko.Business.Exceptions;
using Kasko.Business.Interfaces;
using Kasko.Business.Services.Concrete;
using Kasko.DataAccess.Repositories.Abstract;
using Microsoft.AspNetCore.Http;
using Kasko.Entities.Concrete;
using Kasko.Entities.Enums;
using Moq;

namespace Kasko.Business.Tests.Services
{
    public class PaymentServiceTests
    {
        private Mock<IPaymentRepository> _paymentRepositoryMock = null!;
        private Mock<IPolicyRepository> _policyRepositoryMock = null!;
        private Mock<IUnitOfWork> _unitOfWorkMock = null!;
        private PaymentService _paymentService = null!;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

        public PaymentServiceTests()
        {
            _paymentRepositoryMock = new Mock<IPaymentRepository>();
            _policyRepositoryMock = new Mock<IPolicyRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _notificationServiceMock = new Mock<INotificationService>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            _paymentService = new PaymentService(
                _paymentRepositoryMock.Object,
                _policyRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _notificationServiceMock.Object,
                _httpContextAccessorMock.Object
                );
        }

       
        [Fact]
        public async Task CreateAsync_WhenPolicyNotFound_ShouldThrowNotFoundException()
        {
            
            var policyId = Guid.NewGuid();

            var dto = new PaymentCreateDto
            {
                PolicyId = policyId,
                SimulateFailure = false
            };

            _policyRepositoryMock
                .Setup(x => x.GetByIdAsync(policyId))
                .ReturnsAsync((Policy?)null);

         
            var act = async () =>
                await _paymentService.CreateAsync(dto);

         
            await Assert.ThrowsAsync<NotFoundException>(act);

            _policyRepositoryMock.Verify(
                x => x.GetByIdAsync(policyId),
                Times.Once);

            _paymentRepositoryMock.Verify(
                x => x.HasSuccessfulPaymentAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenPolicyDeleted_ShouldThrowNotFoundException()
        {
            
            var policyId = Guid.NewGuid();

            var policy = new Policy
            {
                Id = policyId,
                IsDeleted = true,
                Status = PolicyStatus.Draft,
                PremiumAmount = 10000m
            };

            var dto = new PaymentCreateDto
            {
                PolicyId = policyId,
                SimulateFailure = false
            };

            _policyRepositoryMock
                .Setup(x => x.GetByIdAsync(policyId))
                .ReturnsAsync(policy);

            
            var act = async () =>
                await _paymentService.CreateAsync(dto);

            
            await Assert.ThrowsAsync<NotFoundException>(act);

            _paymentRepositoryMock.Verify(
                x => x.HasSuccessfulPaymentAsync(It.IsAny<Guid>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenPolicyIsActive_ShouldThrowBadRequestException()
        {
           
            var policyId = Guid.NewGuid();

            var policy = new Policy
            {
                Id = policyId,
                IsDeleted = false,
                Status = PolicyStatus.Active,
                PremiumAmount = 10000m
            };

            var dto = new PaymentCreateDto
            {
                PolicyId = policyId,
                SimulateFailure = false
            };

            _policyRepositoryMock
                .Setup(x => x.GetByIdAsync(policyId))
                .ReturnsAsync(policy);

            var act = async () =>
                await _paymentService.CreateAsync(dto);

            
            await Assert.ThrowsAsync<BadRequestException>(act);
        }

        [Fact]
        public async Task CreateAsync_WhenSuccessfulPaymentAlreadyExists_ShouldThrowBadRequestException()
        {
            
            var policyId = Guid.NewGuid();

            var policy = new Policy
            {
                Id = policyId,
                IsDeleted = false,
                Status = PolicyStatus.Draft,
                PremiumAmount = 10000m
            };

            var dto = new PaymentCreateDto
            {
                PolicyId = policyId,
                SimulateFailure = false
            };

            _policyRepositoryMock
                .Setup(x => x.GetByIdAsync(policyId))
                .ReturnsAsync(policy);

            _paymentRepositoryMock
                .Setup(x => x.HasSuccessfulPaymentAsync(policyId))
                .ReturnsAsync(true);

            
            var act = async () =>
                await _paymentService.CreateAsync(dto);

            
            await Assert.ThrowsAsync<BadRequestException>(act);

            _paymentRepositoryMock.Verify(
                x => x.HasSuccessfulPaymentAsync(policyId),
                Times.Once);

            _paymentRepositoryMock.Verify(
                x => x.AddAsync(It.IsAny<Payment>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_WhenPaymentSuccessful_ShouldCreatePaymentAndActivatePolicy()
        {
           
            var policyId = Guid.NewGuid();

            var policy = new Policy
            {
                Id = policyId,
                IsDeleted = false,
                Status = PolicyStatus.Draft,
                PremiumAmount = 12500m
            };

            var dto = new PaymentCreateDto
            {
                PolicyId = policyId,
                SimulateFailure = false
            };

            _policyRepositoryMock
                .Setup(x => x.GetByIdAsync(policyId))
                .ReturnsAsync(policy);

            _paymentRepositoryMock
                .Setup(x => x.HasSuccessfulPaymentAsync(policyId))
                .ReturnsAsync(false);

            _paymentRepositoryMock
                .Setup(x => x.TransactionNumberExistsAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync(false);

            _paymentRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Payment>()))
                .Returns(Task.CompletedTask);

            _policyRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Policy>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(action => action());

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

           
            var result = await _paymentService.CreateAsync(dto);

            
            Assert.NotNull(result);
            Assert.Equal(policyId, result.PolicyId);
            Assert.Equal(policy.PremiumAmount, result.Amount);
            Assert.Equal(PaymentStatus.Successful, result.Status);
            Assert.NotNull(result.PaymentDate);
            Assert.Null(result.FailureReason);

            Assert.Equal(PolicyStatus.Active, policy.Status);

            _paymentRepositoryMock.Verify(
                x => x.AddAsync(It.Is<Payment>(p =>
                    p.PolicyId == policyId &&
                    p.Amount == policy.PremiumAmount &&
                    p.Status == PaymentStatus.Successful &&
                    p.IsDeleted == false &&
                    p.FailureReason == null &&
                    p.PaymentDate != null)),
                Times.Once);

            _policyRepositoryMock.Verify(
                x => x.UpdateAsync(It.Is<Policy>(p =>
                    p.Id == policyId &&
                    p.Status == PolicyStatus.Active)),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenSimulateFailureIsTrue_ShouldCreateFailedPaymentAndKeepPolicyDraft()
        {
            
            var policyId = Guid.NewGuid();

            var policy = new Policy
            {
                Id = policyId,
                IsDeleted = false,
                Status = PolicyStatus.Draft,
                PremiumAmount = 8500m
            };

            var dto = new PaymentCreateDto
            {
                PolicyId = policyId,
                SimulateFailure = true
            };

            _policyRepositoryMock
                .Setup(x => x.GetByIdAsync(policyId))
                .ReturnsAsync(policy);

            _paymentRepositoryMock
                .Setup(x => x.HasSuccessfulPaymentAsync(policyId))
                .ReturnsAsync(false);

            _paymentRepositoryMock
                .Setup(x => x.TransactionNumberExistsAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync(false);

            _paymentRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Payment>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(action => action());

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            
            var result = await _paymentService.CreateAsync(dto);

            
            Assert.NotNull(result);

            Assert.Equal(policyId, result.PolicyId);
            Assert.Equal(policy.PremiumAmount, result.Amount);
            Assert.Equal(PaymentStatus.Failed, result.Status);

            Assert.Null(result.PaymentDate);

            Assert.Equal(
                "Simüle edilen ödeme hatası.",
                result.FailureReason);

            Assert.Equal(
                PolicyStatus.Draft,
                policy.Status);

            _paymentRepositoryMock.Verify(
                x => x.AddAsync(It.Is<Payment>(p =>
                    p.PolicyId == policyId &&
                    p.Amount == policy.PremiumAmount &&
                    p.Status == PaymentStatus.Failed &&
                    p.PaymentDate == null &&
                    p.FailureReason == "Simüle edilen ödeme hatası." &&
                    p.IsDeleted == false)),
                Times.Once);

            _policyRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Policy>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldSetPaymentAmountFromPolicyPremium()
        {
            
            var policyId = Guid.NewGuid();

            var policy = new Policy
            {
                Id = policyId,
                IsDeleted = false,
                Status = PolicyStatus.Draft,
                PremiumAmount = 15750m
            };

            var dto = new PaymentCreateDto
            {
                PolicyId = policyId,
                SimulateFailure = false
            };

            _policyRepositoryMock
                .Setup(x => x.GetByIdAsync(policyId))
                .ReturnsAsync(policy);

            _paymentRepositoryMock
                .Setup(x => x.HasSuccessfulPaymentAsync(policyId))
                .ReturnsAsync(false);

            _paymentRepositoryMock
                .Setup(x => x.TransactionNumberExistsAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync(false);

            _paymentRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Payment>()))
                .Returns(Task.CompletedTask);

            _policyRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Policy>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(action => action());

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

          
            var result = await _paymentService.CreateAsync(dto);

           
            Assert.Equal(policy.PremiumAmount, result.Amount);
        }

        [Fact]
        public async Task CreateAsync_ShouldGenerateValidTransactionNumber()
        {
            
            var policyId = Guid.NewGuid();

            var policy = new Policy
            {
                Id = policyId,
                IsDeleted = false,
                Status = PolicyStatus.Draft,
                PremiumAmount = 10000m
            };

            var dto = new PaymentCreateDto
            {
                PolicyId = policyId,
                SimulateFailure = false
            };

            _policyRepositoryMock
                .Setup(x => x.GetByIdAsync(policyId))
                .ReturnsAsync(policy);

            _paymentRepositoryMock
                .Setup(x => x.HasSuccessfulPaymentAsync(policyId))
                .ReturnsAsync(false);

            _paymentRepositoryMock
                .Setup(x => x.TransactionNumberExistsAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync(false);

            _paymentRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Payment>()))
                .Returns(Task.CompletedTask);

            _policyRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Policy>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(action => action());

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            
            var result = await _paymentService.CreateAsync(dto);

            
            Assert.NotNull(result.TransactionNumber);
            Assert.StartsWith(
                $"PAY-{DateTime.UtcNow.Year}-",
                result.TransactionNumber);

            var suffix = result.TransactionNumber[
                $"PAY-{DateTime.UtcNow.Year}-".Length..];

            Assert.Equal(8, suffix.Length);
        }

        [Fact]
        public async Task CreateAsync_ShouldMapCreatedPaymentToPaymentDto()
        {
           
            var policyId = Guid.NewGuid();

            var policy = new Policy
            {
                Id = policyId,
                IsDeleted = false,
                Status = PolicyStatus.Draft,
                PremiumAmount = 9000m
            };

            var dto = new PaymentCreateDto
            {
                PolicyId = policyId,
                SimulateFailure = false
            };

            _policyRepositoryMock
                .Setup(x => x.GetByIdAsync(policyId))
                .ReturnsAsync(policy);

            _paymentRepositoryMock
                .Setup(x => x.HasSuccessfulPaymentAsync(policyId))
                .ReturnsAsync(false);

            _paymentRepositoryMock
                .Setup(x => x.TransactionNumberExistsAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync(false);

            _paymentRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Payment>()))
                .Returns(Task.CompletedTask);

            _policyRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Policy>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(action => action());

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

          
            var result = await _paymentService.CreateAsync(dto);

            
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(policyId, result.PolicyId);
            Assert.Equal(policy.PremiumAmount, result.Amount);
            Assert.Equal(PaymentStatus.Successful, result.Status);
            Assert.False(string.IsNullOrWhiteSpace(result.TransactionNumber));
        }

        [Fact]
        public async Task CreateAsync_WhenTransactionNumberAlreadyExists_ShouldCheckAgainUntilUnique()
        {
           
            var policyId = Guid.NewGuid();

            var policy = new Policy
            {
                Id = policyId,
                IsDeleted = false,
                Status = PolicyStatus.Draft,
                PremiumAmount = 10000m
            };

            var dto = new PaymentCreateDto
            {
                PolicyId = policyId,
                SimulateFailure = false
            };

            var callCount = 0;

            _policyRepositoryMock
                .Setup(x => x.GetByIdAsync(policyId))
                .ReturnsAsync(policy);

            _paymentRepositoryMock
                .Setup(x => x.HasSuccessfulPaymentAsync(policyId))
                .ReturnsAsync(false);

            _paymentRepositoryMock
                .Setup(x => x.TransactionNumberExistsAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid?>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    return callCount == 1;
                });

            _paymentRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Payment>()))
                .Returns(Task.CompletedTask);

            _policyRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Policy>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(action => action());

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            
            var result = await _paymentService.CreateAsync(dto);

            
            Assert.NotNull(result.TransactionNumber);

            _paymentRepositoryMock.Verify(
                x => x.TransactionNumberExistsAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid?>()),
                Times.Exactly(2));
        }

       
        [Fact]
        public async Task GetByIdAsync_WhenPaymentNotFound_ShouldThrowNotFoundException()
        {
           
            var paymentId = Guid.NewGuid();

            _paymentRepositoryMock
                .Setup(x => x.GetByIdIncludingDetailsAsync(paymentId))
                .ReturnsAsync((Payment?)null);

            
            var act = async () =>
                await _paymentService.GetByIdAsync(paymentId);

            
            await Assert.ThrowsAsync<NotFoundException>(act);

            _paymentRepositoryMock.Verify(
                x => x.GetByIdIncludingDetailsAsync(paymentId),
                Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnMappedDto()
        {
           
            var paymentId = Guid.NewGuid();
            var policyId = Guid.NewGuid();

            var paymentDate = DateTime.UtcNow;

            var payment = new Payment
            {
                Id = paymentId,
                PolicyId = policyId,
                TransactionNumber = "PAY-2026-ABC12345",
                Amount = 12500m,
                Status = PaymentStatus.Successful,
                PaymentDate = paymentDate,
                FailureReason = null,
                IsDeleted = false
            };

            _paymentRepositoryMock
                .Setup(x => x.GetByIdIncludingDetailsAsync(paymentId))
                .ReturnsAsync(payment);

           
            var result = await _paymentService.GetByIdAsync(paymentId);

            
            Assert.NotNull(result);
            Assert.Equal(paymentId, result!.Id);
            Assert.Equal(policyId, result.PolicyId);
            Assert.Equal(payment.TransactionNumber, result.TransactionNumber);
            Assert.Equal(payment.Amount, result.Amount);
            Assert.Equal(payment.Status, result.Status);
            Assert.Equal(paymentDate, result.PaymentDate);
            Assert.Equal(payment.FailureReason, result.FailureReason);
        }

        
        [Fact]
        public async Task GetAllAsync_ShouldExcludeDeletedPayments()
        {
            
            var activePayment = new Payment
            {
                Id = Guid.NewGuid(),
                PolicyId = Guid.NewGuid(),
                TransactionNumber = "PAY-2026-AABBCCDD",
                Amount = 10000m,
                Status = PaymentStatus.Successful,
                PaymentDate = DateTime.UtcNow,
                IsDeleted = false
            };

            var deletedPayment = new Payment
            {
                Id = Guid.NewGuid(),
                PolicyId = Guid.NewGuid(),
                TransactionNumber = "PAY-2026-11223344",
                Amount = 15000m,
                Status = PaymentStatus.Successful,
                PaymentDate = DateTime.UtcNow,
                IsDeleted = true
            };

            _paymentRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Payment>
                {
                    activePayment,
                    deletedPayment
                });

            
            var result = await _paymentService.GetAllAsync();

            
            var payments = result.ToList();

            Assert.Single(payments);
            Assert.Equal(activePayment.Id, payments[0].Id);
        }

        [Fact]
        public async Task GetAllAsync_ShouldMapPaymentsToPaymentListDtos()
        {
            
            var payment1 = new Payment
            {
                Id = Guid.NewGuid(),
                PolicyId = Guid.NewGuid(),
                TransactionNumber = "PAY-2026-AAAA1111",
                Amount = 10000m,
                Status = PaymentStatus.Successful,
                PaymentDate = DateTime.UtcNow,
                IsDeleted = false
            };

            var payment2 = new Payment
            {
                Id = Guid.NewGuid(),
                PolicyId = Guid.NewGuid(),
                TransactionNumber = "PAY-2026-BBBB2222",
                Amount = 20000m,
                Status = PaymentStatus.Failed,
                PaymentDate = null,
                IsDeleted = false
            };

            _paymentRepositoryMock
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<Payment>
                {
                    payment1,
                    payment2
                });

            
            var result = (await _paymentService.GetAllAsync()).ToList();

           
            Assert.Equal(2, result.Count);

            Assert.Equal(payment1.Id, result[0].Id);
            Assert.Equal(payment1.PolicyId, result[0].PolicyId);
            Assert.Equal(payment1.TransactionNumber, result[0].TransactionNumber);
            Assert.Equal(payment1.Amount, result[0].Amount);
            Assert.Equal(payment1.Status, result[0].Status);
            Assert.Equal(payment1.PaymentDate, result[0].PaymentDate);

            Assert.Equal(payment2.Id, result[1].Id);
            Assert.Equal(payment2.PolicyId, result[1].PolicyId);
            Assert.Equal(payment2.TransactionNumber, result[1].TransactionNumber);
            Assert.Equal(payment2.Amount, result[1].Amount);
            Assert.Equal(payment2.Status, result[1].Status);
            Assert.Equal(payment2.PaymentDate, result[1].PaymentDate);
        }

       
        [Fact]
        public async Task DeleteAsync_WhenPaymentNotFound_ShouldThrowNotFoundException()
        {
            
            var paymentId = Guid.NewGuid();

            _paymentRepositoryMock
                .Setup(x => x.GetByIdAsync(paymentId))
                .ReturnsAsync((Payment?)null);

            
            var act = async () =>
                await _paymentService.DeleteAsync(
                    paymentId,
                    Guid.NewGuid());

            
            await Assert.ThrowsAsync<NotFoundException>(act);

            _paymentRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Payment>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WhenPaymentAlreadyDeleted_ShouldThrowNotFoundException()
        {
            
            var paymentId = Guid.NewGuid();

            var payment = new Payment
            {
                Id = paymentId,
                IsDeleted = true
            };

            _paymentRepositoryMock
                .Setup(x => x.GetByIdAsync(paymentId))
                .ReturnsAsync(payment);

            
            var act = async () =>
                await _paymentService.DeleteAsync(
                    paymentId,
                    Guid.NewGuid());

            
            await Assert.ThrowsAsync<NotFoundException>(act);

            _paymentRepositoryMock.Verify(
                x => x.UpdateAsync(It.IsAny<Payment>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldSoftDeletePayment()
        {
            
            var paymentId = Guid.NewGuid();
            var deletedBy = Guid.NewGuid();

            var payment = new Payment
            {
                Id = paymentId,
                IsDeleted = false
            };

            _paymentRepositoryMock
                .Setup(x => x.GetByIdAsync(paymentId))
                .ReturnsAsync(payment);

            _paymentRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Payment>()))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(1);

            
            await _paymentService.DeleteAsync(paymentId, deletedBy);

            
            Assert.True(payment.IsDeleted);
            Assert.NotNull(payment.DeletedDate);
            Assert.Equal(deletedBy, payment.DeletedBy);

            _paymentRepositoryMock.Verify(
                x => x.UpdateAsync(It.Is<Payment>(p =>
                    p.Id == paymentId &&
                    p.IsDeleted &&
                    p.DeletedBy == deletedBy &&
                    p.DeletedDate != null)),
                Times.Once);

            _unitOfWorkMock.Verify(
                x => x.SaveChangesAsync(),
                Times.Once);
        }
    }
}