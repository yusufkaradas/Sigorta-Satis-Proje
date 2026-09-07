using Kasko.Business.DTOs.Policy;
using Kasko.Business.Exceptions;
using Kasko.Business.Services.Concrete;
using Kasko.DataAccess.Repositories;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Kasko.Entities.Enums;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Kasko.Business.Tests.Services;

public class PolicyServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPolicyRepository> _policyRepositoryMock;
    private readonly Mock<IQuoteRepository> _quoteRepositoryMock;
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    private readonly PolicyService _service;

    public PolicyServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _policyRepositoryMock = new Mock<IPolicyRepository>();
        _quoteRepositoryMock = new Mock<IQuoteRepository>();
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _vehicleRepositoryMock = new Mock<IVehicleRepository>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _unitOfWorkMock
            .Setup(x => x.Policies)
            .Returns(_policyRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.Quotes)
            .Returns(_quoteRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.Customers)
            .Returns(_customerRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.Vehicles)
            .Returns(_vehicleRepositoryMock.Object);

        _service = new PolicyService(
            _unitOfWorkMock.Object,
            _policyRepositoryMock.Object,
            _quoteRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _vehicleRepositoryMock.Object,
            _httpContextAccessorMock.Object);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdatePolicy_WhenRowVersionIsValid()
    {

        var policyId = Guid.NewGuid();

        var rowVersionBytes = new byte[]
        {
            1, 2, 3, 4, 5, 6, 7, 8
        };

        var rowVersionBase64 =
            Convert.ToBase64String(rowVersionBytes);

        var policy = new Policy
        {
            Id = policyId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteId = Guid.NewGuid(),
            PolicyNumber = "POL-TEST-001",
            PremiumAmount = 25000,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = PolicyStatus.Draft,
            IsDeleted = false,
            RowVersion = rowVersionBytes
        };

        var newEndDate = policy.StartDate.AddYears(2);

        var dto = new PolicyUpdateDto
        {
            EndDate = newEndDate,
            RowVersion = rowVersionBase64
        };

        _policyRepositoryMock
            .Setup(x => x.GetByIdAsync(policyId))
            .ReturnsAsync(policy);

        _policyRepositoryMock
            .Setup(x => x.UpdateAsync(policy))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);


        await _service.UpdateAsync(policyId, dto);


        Assert.Equal(newEndDate, policy.EndDate);

        _policyRepositoryMock.Verify(
            x => x.SetOriginalRowVersion(
                policy,
                It.Is<byte[]>(rv =>
                    rv.SequenceEqual(rowVersionBytes))),
            Times.Once);

        _policyRepositoryMock.Verify(
            x => x.UpdateAsync(policy),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowConflict_WhenRowVersionIsStale()
    {

        var policyId = Guid.NewGuid();

        var currentRowVersion = new byte[]
        {
        1, 2, 3, 4, 5, 6, 7, 8
        };

        var oldRowVersion = new byte[]
        {
        9, 10, 11, 12, 13, 14, 15, 16
        };

        var dto = new PolicyUpdateDto
        {
            EndDate = DateTime.UtcNow.Date.AddYears(2),
            RowVersion = Convert.ToBase64String(oldRowVersion)
        };

        var policy = new Policy
        {
            Id = policyId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteId = Guid.NewGuid(),
            PolicyNumber = "POL-TEST-002",
            PremiumAmount = 25000,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = PolicyStatus.Draft,
            IsDeleted = false,
            RowVersion = currentRowVersion
        };

        _policyRepositoryMock
            .Setup(x => x.GetByIdAsync(policyId))
            .ReturnsAsync(policy);

        _policyRepositoryMock
            .Setup(x => x.UpdateAsync(policy))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException());


        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _service.UpdateAsync(policyId, dto));


        Assert.Equal(
            "Poliçe başka bir kullanıcı tarafından güncellenmiş. Lütfen güncel veriyi tekrar alın.",
            exception.Message);

        _policyRepositoryMock.Verify(
            x => x.SetOriginalRowVersion(
                policy,
                It.Is<byte[]>(rv =>
                    rv.SequenceEqual(oldRowVersion))),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task UpdateAsync_ShouldThrowNotFound_WhenPolicyDoesNotExist()
    {
        
        var policyId = Guid.NewGuid();

        var dto = new PolicyUpdateDto
        {
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            RowVersion = Convert.ToBase64String(new byte[8])
        };

        _policyRepositoryMock
            .Setup(x => x.GetByIdAsync(policyId))
            .ReturnsAsync((Policy?)null);

        
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateAsync(policyId, dto));

        
        Assert.Equal(
            "Poliçe bulunamadı.",
            exception.Message);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task UpdateAsync_ShouldThrowBadRequest_WhenEndDateIsInvalid()
    {
        
        var policyId = Guid.NewGuid();

        var policy = new Policy
        {
            Id = policyId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteId = Guid.NewGuid(),
            PolicyNumber = "POL-TEST-ENDDATE",
            PremiumAmount = 25000,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = PolicyStatus.Draft,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        var dto = new PolicyUpdateDto
        {
            
            EndDate = policy.StartDate.AddDays(-1),

            
            RowVersion = Convert.ToBase64String(policy.RowVersion)
        };

        _policyRepositoryMock
            .Setup(x => x.GetByIdAsync(policyId))
            .ReturnsAsync(policy);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.UpdateAsync(policyId, dto));

        
        Assert.Equal(
            "Poliçe bitiş tarihi başlangıç tarihinden sonra olmalıdır.",
            exception.Message);

        _policyRepositoryMock.Verify(
            x => x.SetOriginalRowVersion(
                It.IsAny<Policy>(),
                It.IsAny<byte[]>()),
            Times.Never);

        _policyRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Policy>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task UpdateAsync_ShouldThrowBadRequest_WhenPolicyIsCancelled()
    {
        // Arrange
        var policyId = Guid.NewGuid();

        var rowVersion = new byte[]
        {
        1, 2, 3, 4, 5, 6, 7, 8
        };

        var policy = new Policy
        {
            Id = policyId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteId = Guid.NewGuid(),
            PolicyNumber = "POL-TEST-CANCELLED",
            PremiumAmount = 25000,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = PolicyStatus.Cancelled,
            IsDeleted = false,
            RowVersion = rowVersion
        };

        var dto = new PolicyUpdateDto
        {
            EndDate = policy.StartDate.AddYears(2),
            RowVersion = Convert.ToBase64String(rowVersion)
        };

        _policyRepositoryMock
            .Setup(x => x.GetByIdAsync(policyId))
            .ReturnsAsync(policy);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.UpdateAsync(policyId, dto));

        
        Assert.Equal(
            "Süresi dolmuş veya iptal edilmiş poliçe güncellenemez.",
            exception.Message);

        _policyRepositoryMock.Verify(
            x => x.SetOriginalRowVersion(
                It.IsAny<Policy>(),
                It.IsAny<byte[]>()),
            Times.Never);

        _policyRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Policy>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task UpdateAsync_ShouldThrowBadRequest_WhenRowVersionIsInvalid()
    {
        
        var policyId = Guid.NewGuid();

        var policy = new Policy
        {
            Id = policyId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteId = Guid.NewGuid(),
            PolicyNumber = "POL-TEST-ROWVERSION",
            PremiumAmount = 25000,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = PolicyStatus.Draft,
            IsDeleted = false,
            RowVersion = new byte[]
            {
            1, 2, 3, 4, 5, 6, 7, 8
            }
        };

        var dto = new PolicyUpdateDto
        {
            EndDate = policy.StartDate.AddYears(2),
            RowVersion = "gecersiz-row-version"
        };

        _policyRepositoryMock
            .Setup(x => x.GetByIdAsync(policyId))
            .ReturnsAsync(policy);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.UpdateAsync(policyId, dto));

        
        Assert.Equal(
            "Geçersiz RowVersion değeri.",
            exception.Message);

        _policyRepositoryMock.Verify(
            x => x.SetOriginalRowVersion(
                It.IsAny<Policy>(),
                It.IsAny<byte[]>()),
            Times.Never);

        _policyRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Policy>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenPolicyDoesNotExist()
    {
        
        var policyId = Guid.NewGuid();

        _policyRepositoryMock
            .Setup(x => x.GetByIdIncludingDetailsAsync(policyId))
            .ReturnsAsync((Policy?)null);

        
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.GetByIdAsync(policyId));

        
        Assert.Equal(
            "Poliçe bulunamadı.",
            exception.Message);
    }
    [Fact]
    public async Task GetByIdAsync_ShouldReturnPolicy_WhenPolicyExists()
    {
        
        var policyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var quoteId = Guid.NewGuid();

        var rowVersion = new byte[]
        {
        1, 2, 3, 4, 5, 6, 7, 8
        };

        var policy = new Policy
        {
            Id = policyId,
            CustomerId = customerId,
            VehicleId = vehicleId,
            QuoteId = quoteId,
            PolicyNumber = "POL-TEST-008",
            PremiumAmount = 25000m,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = PolicyStatus.Active,
            IsDeleted = false,
            RowVersion = rowVersion
        };

        _policyRepositoryMock
            .Setup(x => x.GetByIdIncludingDetailsAsync(policyId))
            .ReturnsAsync(policy);

        
        var result = await _service.GetByIdAsync(policyId);

        
        Assert.NotNull(result);

        Assert.Equal(policyId, result.Id);
        Assert.Equal(customerId, result.CustomerId);
        Assert.Equal(vehicleId, result.VehicleId);
        Assert.Equal(quoteId, result.QuoteId);
        Assert.Equal("POL-TEST-008", result.PolicyNumber);
        Assert.Equal(25000m, result.PremiumAmount);
        Assert.Equal(policy.StartDate, result.StartDate);
        Assert.Equal(policy.EndDate, result.EndDate);
        Assert.Equal(PolicyStatus.Active, result.Status);
        Assert.True(result.IsActive);

        Assert.Equal(
            Convert.ToBase64String(rowVersion),
            result.RowVersion);
    }
    [Fact]
    public async Task DeleteAsync_ShouldThrowNotFound_WhenPolicyDoesNotExist()
    {
        
        var policyId = Guid.NewGuid();

        _policyRepositoryMock
            .Setup(x => x.GetByIdAsync(policyId))
            .ReturnsAsync((Policy?)null);

        
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.DeleteAsync(policyId, null));

        
        Assert.Equal(
            "Poliçe bulunamadı.",
            exception.Message);

        _policyRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Policy>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task DeleteAsync_ShouldSoftDeletePolicy_WhenPolicyExists()
    {
       
        var policyId = Guid.NewGuid();
        var deletedBy = Guid.NewGuid();

        var policy = new Policy
        {
            Id = policyId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteId = Guid.NewGuid(),
            PolicyNumber = "POL-TEST-011",
            PremiumAmount = 25000m,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = PolicyStatus.Active,
            IsDeleted = false
        };

        _policyRepositoryMock
            .Setup(x => x.GetByIdAsync(policyId))
            .ReturnsAsync(policy);

        _policyRepositoryMock
            .Setup(x => x.UpdateAsync(policy))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        
        await _service.DeleteAsync(policyId, deletedBy);

        
        Assert.True(policy.IsDeleted);
        Assert.NotNull(policy.DeletedDate);
        Assert.Equal(deletedBy, policy.DeletedBy);

        _policyRepositoryMock.Verify(
            x => x.UpdateAsync(policy),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task CancelAsync_ShouldThrowNotFound_WhenPolicyDoesNotExist()
    {
        
        var policyId = Guid.NewGuid();

        _policyRepositoryMock
            .Setup(x => x.GetByIdAsync(policyId))
            .ReturnsAsync((Policy?)null);

       
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CancelAsync(policyId, null));

       
        Assert.Equal(
            "Poliçe bulunamadı.",
            exception.Message);

        _policyRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Policy>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task CancelAsync_ShouldThrowBadRequest_WhenPolicyIsNotActive()
    {
        
        var policyId = Guid.NewGuid();

        var policy = new Policy
        {
            Id = policyId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteId = Guid.NewGuid(),
            PolicyNumber = "POL-TEST-013",
            PremiumAmount = 25000m,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = PolicyStatus.Draft,
            IsDeleted = false
        };

        _policyRepositoryMock
            .Setup(x => x.GetByIdAsync(policyId))
            .ReturnsAsync(policy);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CancelAsync(policyId, null));

        
        Assert.Equal(
            "Sadece aktif poliçe iptal edilebilir.",
            exception.Message);

        Assert.Equal(
            PolicyStatus.Draft,
            policy.Status);

        _policyRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Policy>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task CancelAsync_ShouldCancelPolicy_WhenPolicyIsActive()
    {
        
        var policyId = Guid.NewGuid();
        var cancelledBy = Guid.NewGuid();

        var policy = new Policy
        {
            Id = policyId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteId = Guid.NewGuid(),
            PolicyNumber = "POL-TEST-014",
            PremiumAmount = 25000m,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = PolicyStatus.Active,
            IsDeleted = false
        };

        _policyRepositoryMock
            .Setup(x => x.GetByIdAsync(policyId))
            .ReturnsAsync(policy);

        _policyRepositoryMock
            .Setup(x => x.UpdateAsync(policy))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        
        await _service.CancelAsync(policyId, cancelledBy);

        
        Assert.Equal(
            PolicyStatus.Cancelled,
            policy.Status);

        Assert.NotNull(policy.UpdatedDate);

        _policyRepositoryMock.Verify(
            x => x.UpdateAsync(policy),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task ExpireAsync_ShouldThrowNotFound_WhenPolicyDoesNotExist()
    {
        
        var policyId = Guid.NewGuid();

        _policyRepositoryMock
            .Setup(x => x.GetByIdAsync(policyId))
            .ReturnsAsync((Policy?)null);

        
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.ExpireAsync(policyId));

        
        Assert.Equal(
            "Poliçe bulunamadı.",
            exception.Message);

        _policyRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Policy>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task ExpireAsync_ShouldThrowBadRequest_WhenPolicyIsNotActive()
    {
        
        var policyId = Guid.NewGuid();

        var policy = new Policy
        {
            Id = policyId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteId = Guid.NewGuid(),
            PolicyNumber = "POL-TEST-016",
            PremiumAmount = 25000m,
            StartDate = DateTime.UtcNow.Date.AddYears(-1),
            EndDate = DateTime.UtcNow.Date.AddDays(-1),
            Status = PolicyStatus.Draft,
            IsDeleted = false
        };

        _policyRepositoryMock
            .Setup(x => x.GetByIdAsync(policyId))
            .ReturnsAsync(policy);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.ExpireAsync(policyId));

        
        Assert.Equal(
            "Sadece aktif poliçe süresi dolabilir.",
            exception.Message);

        _policyRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Policy>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task ExpireAsync_ShouldThrowBadRequest_WhenPolicyHasNotExpired()
    {
        
        var policyId = Guid.NewGuid();

        var policy = new Policy
        {
            Id = policyId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteId = Guid.NewGuid(),
            PolicyNumber = "POL-TEST-017",
            PremiumAmount = 25000m,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(10),
            Status = PolicyStatus.Active,
            IsDeleted = false
        };

        _policyRepositoryMock
            .Setup(x => x.GetByIdAsync(policyId))
            .ReturnsAsync(policy);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.ExpireAsync(policyId));

        
        Assert.Equal(
            "Poliçenin süresi henüz dolmamıştır.",
            exception.Message);

        Assert.Equal(
            PolicyStatus.Active,
            policy.Status);

        _policyRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Policy>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task ExpireAsync_ShouldExpirePolicy_WhenPolicyIsActiveAndExpired()
    {
        
        var policyId = Guid.NewGuid();

        var policy = new Policy
        {
            Id = policyId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteId = Guid.NewGuid(),
            PolicyNumber = "POL-TEST-018",
            PremiumAmount = 25000m,
            StartDate = DateTime.UtcNow.Date.AddYears(-1),
            EndDate = DateTime.UtcNow.Date.AddDays(-1),
            Status = PolicyStatus.Active,
            IsDeleted = false
        };

        _policyRepositoryMock
            .Setup(x => x.GetByIdAsync(policyId))
            .ReturnsAsync(policy);

        _policyRepositoryMock
            .Setup(x => x.UpdateAsync(policy))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        
        await _service.ExpireAsync(policyId);

       
        Assert.Equal(
            PolicyStatus.Expired,
            policy.Status);

        Assert.NotNull(policy.UpdatedDate);

        _policyRepositoryMock.Verify(
            x => x.UpdateAsync(policy),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task GetAllAsync_ShouldExcludeDeletedPolicies()
    {
        
        var activePolicy = new Policy
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteId = Guid.NewGuid(),
            PolicyNumber = "POL-TEST-019-A",
            PremiumAmount = 25000m,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = PolicyStatus.Active,
            IsDeleted = false
        };

        var deletedPolicy = new Policy
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteId = Guid.NewGuid(),
            PolicyNumber = "POL-TEST-019-D",
            PremiumAmount = 30000m,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = PolicyStatus.Cancelled,
            IsDeleted = true
        };

        _policyRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Policy>
            {
            activePolicy,
            deletedPolicy
            });

        
        var result = (await _service.GetAllAsync()).ToList();

       
        Assert.Single(result);

        Assert.Equal(
            activePolicy.Id,
            result[0].Id);

        Assert.DoesNotContain(
            result,
            x => x.Id == deletedPolicy.Id);
    }
    [Fact]
    public async Task GetAllAsync_ShouldMapPoliciesToListDto()
    {
        
        var policy = new Policy
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteId = Guid.NewGuid(),
            PolicyNumber = "POL-TEST-020",
            PremiumAmount = 35000m,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1),
            Status = PolicyStatus.Active,
            IsDeleted = false
        };

        _policyRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Policy>
            {
            policy
            });

    
        var result = (await _service.GetAllAsync()).ToList();

        
        Assert.Single(result);

        Assert.Equal(policy.Id, result[0].Id);
        Assert.Equal(policy.CustomerId, result[0].CustomerId);
        Assert.Equal(policy.VehicleId, result[0].VehicleId);
        Assert.Equal(policy.PolicyNumber, result[0].PolicyNumber);
        Assert.Equal(policy.PremiumAmount, result[0].PremiumAmount);
        Assert.Equal(policy.StartDate, result[0].StartDate);
        Assert.Equal(policy.EndDate, result[0].EndDate);
        Assert.Equal(policy.Status, result[0].Status);
    }
    [Fact]
    public async Task CreateAsync_ShouldThrowNotFound_WhenCustomerDoesNotExist()
    {
        
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var quoteId = Guid.NewGuid();

        var dto = new PolicyCreateDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            QuoteId = quoteId,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1)
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync((Customer?)null);

        
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateAsync(dto));

        Assert.Equal(
            "Müşteri bulunamadı.",
            exception.Message);

        _vehicleRepositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>()),
            Times.Never);

        _policyRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Policy>()),
            Times.Never);
    }
    [Fact]
    public async Task CreateAsync_ShouldThrowNotFound_WhenVehicleDoesNotExist()
    {
        
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var quoteId = Guid.NewGuid();

        var dto = new PolicyCreateDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            QuoteId = quoteId,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1)
        };

        var customer = new Customer
        {
            Id = customerId,
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync((Vehicle?)null);

        
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateAsync(dto));

        
        Assert.Equal(
            "Araç bulunamadı.",
            exception.Message);

        _quoteRepositoryMock.Verify(
            x => x.GetByIdIncludingDetailsAsync(It.IsAny<Guid>()),
            Times.Never);

        _policyRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Policy>()),
            Times.Never);
    }
    [Fact]
    public async Task CreateAsync_ShouldThrowBadRequest_WhenVehicleBelongsToAnotherCustomer()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var anotherCustomerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var quoteId = Guid.NewGuid();

        var dto = new PolicyCreateDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            QuoteId = quoteId,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1)
        };

        var customer = new Customer
        {
            Id = customerId,
            IsDeleted = false
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = anotherCustomerId,
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateAsync(dto));

        
        Assert.Equal(
            "Araç belirtilen müşteriye ait değildir.",
            exception.Message);

        _quoteRepositoryMock.Verify(
            x => x.GetByIdIncludingDetailsAsync(It.IsAny<Guid>()),
            Times.Never);
    }
    [Fact]
    public async Task CreateAsync_ShouldThrowNotFound_WhenQuoteDoesNotExist()
    {
        
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var quoteId = Guid.NewGuid();

        var dto = new PolicyCreateDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            QuoteId = quoteId,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1)
        };

        var customer = new Customer
        {
            Id = customerId,
            IsDeleted = false
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _quoteRepositoryMock
            .Setup(x => x.GetByIdIncludingDetailsAsync(quoteId))
            .ReturnsAsync((Quote?)null);

        
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateAsync(dto));

        
        Assert.Equal(
            "Teklif bulunamadı.",
            exception.Message);
    }
    [Fact]
    public async Task CreateAsync_ShouldThrowBadRequest_WhenQuoteBelongsToAnotherCustomer()
    {
        
        var customerId = Guid.NewGuid();
        var anotherCustomerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var quoteId = Guid.NewGuid();

        var dto = new PolicyCreateDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            QuoteId = quoteId,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1)
        };

        var customer = new Customer
        {
            Id = customerId,
            IsDeleted = false
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            IsDeleted = false
        };

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = anotherCustomerId,
            VehicleId = vehicleId,
            PremiumAmount = 25000m,
            Status = QuoteStatus.Accepted,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _quoteRepositoryMock
            .Setup(x => x.GetByIdIncludingDetailsAsync(quoteId))
            .ReturnsAsync(quote);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateAsync(dto));

        
        Assert.Equal(
            "Teklif belirtilen müşteriye ait değildir.",
            exception.Message);
    }
    [Fact]
    public async Task CreateAsync_ShouldThrowBadRequest_WhenQuoteIsExpired()
    {
        
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var quoteId = Guid.NewGuid();

        var dto = new PolicyCreateDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            QuoteId = quoteId,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1)
        };

        var customer = new Customer
        {
            Id = customerId,
            IsDeleted = false
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            IsDeleted = false
        };

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = customerId,
            VehicleId = vehicleId,
            PremiumAmount = 25000m,
            Status = QuoteStatus.Accepted,
            ValidUntil = DateTime.UtcNow.AddDays(-1),
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _quoteRepositoryMock
            .Setup(x => x.GetByIdIncludingDetailsAsync(quoteId))
            .ReturnsAsync(quote);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateAsync(dto));

        
        Assert.Equal(
            "Teklifin geçerlilik süresi dolmuştur.",
            exception.Message);
    }
    [Fact]
    public async Task CreateAsync_ShouldThrowBadRequest_WhenQuoteIsNotAccepted()
    {
        
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var quoteId = Guid.NewGuid();

        var dto = new PolicyCreateDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            QuoteId = quoteId,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1)
        };

        var customer = new Customer
        {
            Id = customerId,
            IsDeleted = false
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            IsDeleted = false
        };

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = customerId,
            VehicleId = vehicleId,
            PremiumAmount = 25000m,
            Status = QuoteStatus.Offered,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _quoteRepositoryMock
            .Setup(x => x.GetByIdIncludingDetailsAsync(quoteId))
            .ReturnsAsync(quote);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateAsync(dto));

        
        Assert.Equal(
            "Sadece kabul edilmiş teklifler poliçeye dönüştürülebilir.",
            exception.Message);
    }
    [Fact]
    public async Task CreateAsync_ShouldThrowBadRequest_WhenStartDateIsNotBeforeEndDate()
    {
        
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var quoteId = Guid.NewGuid();

        var startDate = DateTime.UtcNow.Date.AddDays(10);
        var endDate = DateTime.UtcNow.Date.AddDays(5);

        var dto = new PolicyCreateDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            QuoteId = quoteId,
            StartDate = startDate,
            EndDate = endDate
        };

        var customer = new Customer
        {
            Id = customerId,
            IsDeleted = false
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            IsDeleted = false
        };

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = customerId,
            VehicleId = vehicleId,
            PremiumAmount = 25000m,
            Status = QuoteStatus.Accepted,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _quoteRepositoryMock
            .Setup(x => x.GetByIdIncludingDetailsAsync(quoteId))
            .ReturnsAsync(quote);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateAsync(dto));

        
        Assert.Equal(
            "Poliçe başlangıç tarihi bitiş tarihinden önce olmalıdır.",
            exception.Message);

        _policyRepositoryMock.Verify(
            x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Policy, bool>>>()),
            Times.Never);
    }
    [Fact]
    public async Task CreateAsync_ShouldThrowBadRequest_WhenPolicyAlreadyExistsForQuote()
    {
       
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var quoteId = Guid.NewGuid();

        var dto = new PolicyCreateDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            QuoteId = quoteId,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddYears(1)
        };

        var customer = new Customer
        {
            Id = customerId,
            IsDeleted = false
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            IsDeleted = false
        };

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = customerId,
            VehicleId = vehicleId,
            PremiumAmount = 25000m,
            Status = QuoteStatus.Accepted,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            IsDeleted = false
        };

        var existingPolicy = new Policy
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            VehicleId = vehicleId,
            QuoteId = quoteId,
            PolicyNumber = "POL-EXISTING",
            PremiumAmount = 25000m,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = PolicyStatus.Draft,
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _quoteRepositoryMock
            .Setup(x => x.GetByIdIncludingDetailsAsync(quoteId))
            .ReturnsAsync(quote);

        _policyRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Policy, bool>>>()))
            .ReturnsAsync(new List<Policy>
            {
            existingPolicy
            });

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateAsync(dto));

        
        Assert.Equal(
            "Bu teklif için zaten bir poliçe oluşturulmuştur.",
            exception.Message);

        _policyRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Policy>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task CreateAsync_ShouldCreatePolicy_WhenDataIsValid()
    {
        
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var quoteId = Guid.NewGuid();

        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddYears(1);

        var dto = new PolicyCreateDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            QuoteId = quoteId,
            StartDate = startDate,
            EndDate = endDate
        };

        var customer = new Customer
        {
            Id = customerId,
            IsDeleted = false
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            IsDeleted = false
        };

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = customerId,
            VehicleId = vehicleId,
            PremiumAmount = 25000m,
            Status = QuoteStatus.Accepted,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _quoteRepositoryMock
            .Setup(x => x.GetByIdIncludingDetailsAsync(quoteId))
            .ReturnsAsync(quote);

        _policyRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Policy, bool>>>()))
            .ReturnsAsync(new List<Policy>());
        
        _policyRepositoryMock
            .Setup(x => x.PolicyNumberExistsAsync(
            It.IsAny<string>(),
            It.IsAny<Guid?>()))
            .ReturnsAsync(false);

        _policyRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Policy>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        
        var result = await _service.CreateAsync(dto);

        
        Assert.NotNull(result);

        Assert.NotEqual(
            Guid.Empty,
            result.Id);

        Assert.Equal(
            customerId,
            result.CustomerId);

        Assert.Equal(
            vehicleId,
            result.VehicleId);

        Assert.Equal(
            quoteId,
            result.QuoteId);

        Assert.Equal(
            25000m,
            result.PremiumAmount);

        Assert.Equal(
            startDate,
            result.StartDate);

        Assert.Equal(
            endDate,
            result.EndDate);

        Assert.Equal(
            PolicyStatus.Draft,
            result.Status);

        Assert.False(result.IsActive);

        Assert.StartsWith(
            "POL-",
            result.PolicyNumber);

        _policyRepositoryMock.Verify(
            x => x.AddAsync(It.Is<Policy>(p =>
                p.CustomerId == customerId &&
                p.VehicleId == vehicleId &&
                p.QuoteId == quoteId &&
                p.PremiumAmount == 25000m &&
                p.Status == PolicyStatus.Draft &&
                !p.IsDeleted)),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
}
    