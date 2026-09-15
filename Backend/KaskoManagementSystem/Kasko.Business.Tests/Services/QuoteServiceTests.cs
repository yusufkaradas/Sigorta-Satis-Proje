using Kasko.Business.DTOs.Quote;
using Kasko.Business.Exceptions;
using Kasko.Business.Pricing;
using Kasko.Business.Services;
using Kasko.DataAccess.Repositories;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Kasko.Entities.Enums;
using System.Threading;
using Moq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;

namespace Kasko.Business.Tests.Services;

public class QuoteServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IQuoteRepository> _quoteRepositoryMock;
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock;
    private readonly Mock<IPricingService> _pricingServiceMock;
    private readonly Mock<IQuoteCoverageRepository> _quoteCoverageRepositoryMock;
    private readonly Mock<IPackageCoverageRepository> _packageCoverageRepositoryMock;
    private readonly Mock<IGenericRepository<QuotePricingSnapshot>> _quotePricingSnapshotRepositoryMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    private readonly QuoteService _service;

    public QuoteServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _quoteRepositoryMock = new Mock<IQuoteRepository>();
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _vehicleRepositoryMock = new Mock<IVehicleRepository>();
        _pricingServiceMock = new Mock<IPricingService>();
        _quoteCoverageRepositoryMock = new Mock<IQuoteCoverageRepository>();
        _packageCoverageRepositoryMock = new Mock<IPackageCoverageRepository>();
        _quotePricingSnapshotRepositoryMock = new Mock<IGenericRepository<QuotePricingSnapshot>>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _unitOfWorkMock
            .Setup(x => x.Quotes)
            .Returns(_quoteRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(x => x.Customers)
            .Returns(_customerRepositoryMock.Object);

        _unitOfWorkMock
             .Setup(x => x.Vehicles)
             .Returns(_vehicleRepositoryMock.Object);
        _unitOfWorkMock
             .Setup(x => x.QuoteCoverages)
             .Returns(_quoteCoverageRepositoryMock.Object);
        _unitOfWorkMock
    .Setup(x => x.PackageCoverages)
    .Returns(_packageCoverageRepositoryMock.Object);
        _unitOfWorkMock
    .Setup(x => x.QuotePricingSnapshots)
    .Returns(_quotePricingSnapshotRepositoryMock.Object);

        _service = new QuoteService(
        _unitOfWorkMock.Object,
        _pricingServiceMock.Object,
        _httpContextAccessorMock.Object);
    }
    [Fact]
    public async Task CalculateAsync_ShouldIncludeDefaultPackageCoverages()
    {
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var packageId = Guid.NewGuid();

        var coverageId1 = Guid.NewGuid();
        var coverageId2 = Guid.NewGuid();

        var customer = new Customer
        {
            Id = customerId,
            DateOfBirth = DateTime.UtcNow.AddYears(-31),
            City = "Istanbul",
            IsActive = true,
            IsDeleted = false
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year,
            IsActive = true,
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _packageCoverageRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<PackageCoverage, bool>>>()))
            .ReturnsAsync(new[]
            {
            new PackageCoverage
            {
                Id = Guid.NewGuid(),
                InsurancePackageId = packageId,
                CoverageId = coverageId1,
                IsDefault = true,
                IsDeleted = false
            },
            new PackageCoverage
            {
                Id = Guid.NewGuid(),
                InsurancePackageId = packageId,
                CoverageId = coverageId2,
                IsDefault = true,
                IsDeleted = false
            }
            });

        _pricingServiceMock
            .Setup(x => x.CalculateAsync(
                It.Is<PricingRequest>(r =>
                    r.PackageId == packageId &&
                    r.CoverageIds.Contains(coverageId1) &&
                    r.CoverageIds.Contains(coverageId2)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingCalculation
            {
                MarketValue = 1_000_000m,
                BasePremium = 20_000m,
                RiskAdjustedPremium = 20_000m,
                CoveragePremium = 0m,
                TotalPremium = 20_000m
            });
        _quotePricingSnapshotRepositoryMock
    .Setup(x => x.AddAsync(It.IsAny<QuotePricingSnapshot>()))
    .Returns(Task.CompletedTask);
        var result =
            await _service.CalculateAsync(
                new CreateQuoteDto
                {
                    CustomerId = customerId,
                    VehicleId = vehicleId,
                    PackageId = packageId,
                    Usage = "PRIVATE",
                    ClaimsCount = 0,
                    Deductible = 0,
                    CoverageIds = Array.Empty<Guid>(),
                    ValidUntil = DateTime.UtcNow.AddDays(10)
                });

        Assert.Equal(20_000m, result.TotalPremium);

        _pricingServiceMock.Verify(
            x => x.CalculateAsync(
                It.Is<PricingRequest>(r =>
                    r.CoverageIds.Count == 2 &&
                    r.CoverageIds.Contains(coverageId1) &&
                    r.CoverageIds.Contains(coverageId2)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    [Fact]
    public async Task ChangeStatusAsync_ShouldChangeDraftToOffered()
    {
        
        var quoteId = Guid.NewGuid();

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-001",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Draft,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsDeleted = false
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

        _quoteRepositoryMock
            .Setup(x => x.UpdateAsync(quote))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        
        await _service.ChangeStatusAsync(
            quoteId,
            QuoteStatus.Offered);

        
        Assert.Equal(
            QuoteStatus.Offered,
            quote.Status);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(quote),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task ChangeStatusAsync_ShouldChangeOfferedToAccepted()
    {
        
        var quoteId = Guid.NewGuid();

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-002",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Offered,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsDeleted = false
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

        _quoteRepositoryMock
            .Setup(x => x.UpdateAsync(quote))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        
        await _service.ChangeStatusAsync(
            quoteId,
            QuoteStatus.Accepted);

        
        Assert.Equal(
            QuoteStatus.Accepted,
            quote.Status);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(quote),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task ChangeStatusAsync_ShouldThrowBadRequest_WhenTransitionIsInvalid()
    {
       
        var quoteId = Guid.NewGuid();

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-003",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Draft,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsDeleted = false
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.ChangeStatusAsync(
                quoteId,
                QuoteStatus.Accepted));

        
        Assert.Contains(
            "geçiş yapılamaz",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task ChangeStatusAsync_ShouldChangeOfferedToRejected()
    {
        
        var quoteId = Guid.NewGuid();

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-004",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Offered,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsDeleted = false
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

        _quoteRepositoryMock
            .Setup(x => x.UpdateAsync(quote))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        
        await _service.ChangeStatusAsync(
            quoteId,
            QuoteStatus.Rejected);

        
        Assert.Equal(
            QuoteStatus.Rejected,
            quote.Status);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(quote),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task ChangeStatusAsync_ShouldChangeOfferedToCancelled()
    {
        
        
        var quoteId = Guid.NewGuid();

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-005",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Offered,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsDeleted = false
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

        _quoteRepositoryMock
            .Setup(x => x.UpdateAsync(quote))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        
        await _service.ChangeStatusAsync(
            quoteId,
            QuoteStatus.Cancelled);

        
        Assert.Equal(
            QuoteStatus.Cancelled,
            quote.Status);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(quote),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task ChangeStatusAsync_ShouldChangeOfferedToExpired()
    {
       
        var quoteId = Guid.NewGuid();

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-006",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Offered,
            ValidUntil = DateTime.UtcNow.AddDays(-1),
            IsDeleted = false
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

        _quoteRepositoryMock
            .Setup(x => x.UpdateAsync(quote))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        
        await _service.ChangeStatusAsync(
            quoteId,
            QuoteStatus.Expired);

        
        Assert.Equal(
            QuoteStatus.Expired,
            quote.Status);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(quote),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task ChangeStatusAsync_ShouldExpireAndThrowBadRequest_WhenValidUntilIsPast()
    {
        
        var quoteId = Guid.NewGuid();

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-007",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Offered,
            ValidUntil = DateTime.UtcNow.AddDays(-1),
            IsDeleted = false
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

        _quoteRepositoryMock
            .Setup(x => x.UpdateAsync(quote))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

       
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.ChangeStatusAsync(
                quoteId,
                QuoteStatus.Accepted));

        
        Assert.Equal(
            QuoteStatus.Expired,
            quote.Status);

        Assert.Contains(
            "geçerlilik süresi dolmuştur",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(quote),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task ChangeStatusAsync_ShouldThrowBadRequest_WhenTryingToExpireValidQuote()
    {
        
        var quoteId = Guid.NewGuid();

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-008",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Offered,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsDeleted = false
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.ChangeStatusAsync(
                quoteId,
                QuoteStatus.Expired));

        
        Assert.Equal(
            QuoteStatus.Offered,
            quote.Status);

        Assert.Contains(
            "geçerlilik süresi dolmamış",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task ChangeStatusAsync_ShouldThrowBadRequest_WhenAcceptedQuoteStatusIsChanged()
    {
        
        var quoteId = Guid.NewGuid();

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-009",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Accepted,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsDeleted = false
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.ChangeStatusAsync(
                quoteId,
                QuoteStatus.Rejected));

       
        Assert.Equal(
            QuoteStatus.Accepted,
            quote.Status);

        Assert.Contains(
            "durumu değiştirilemez",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task ChangeStatusAsync_ShouldThrowBadRequest_WhenRejectedQuoteStatusIsChanged()
    {
        
        var quoteId = Guid.NewGuid();

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-010",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Rejected,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsDeleted = false
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.ChangeStatusAsync(
                quoteId,
                QuoteStatus.Offered));

        
        Assert.Equal(
            QuoteStatus.Rejected,
            quote.Status);

        Assert.Contains(
            "durumu değiştirilemez",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task ChangeStatusAsync_ShouldThrowBadRequest_WhenExpiredQuoteStatusIsChanged()
    {
       
        var quoteId = Guid.NewGuid();

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-011",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Expired,
            ValidUntil = DateTime.UtcNow.AddDays(-1),
            IsDeleted = false
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

       
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.ChangeStatusAsync(
                quoteId,
                QuoteStatus.Offered));

      
        Assert.Equal(
            QuoteStatus.Expired,
            quote.Status);

        Assert.Contains(
            "durumu değiştirilemez",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task ChangeStatusAsync_ShouldThrowBadRequest_WhenCancelledQuoteStatusIsChanged()
    {
        
        var quoteId = Guid.NewGuid();

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-012",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Cancelled,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsDeleted = false
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.ChangeStatusAsync(
                quoteId,
                QuoteStatus.Offered));

      
        Assert.Equal(
            QuoteStatus.Cancelled,
            quote.Status);

        Assert.Contains(
            "durumu değiştirilemez",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task ChangeStatusAsync_ShouldThrowNotFound_WhenQuoteDoesNotExist()
    {
        
        var quoteId = Guid.NewGuid();

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync((Quote?)null);

        
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.ChangeStatusAsync(
                quoteId,
                QuoteStatus.Offered));

        
        Assert.Contains(
            "Teklif bulunamadı",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task UpdateAsync_ShouldThrowNotFound_WhenQuoteDoesNotExist()
    {
        
        var quoteId = Guid.NewGuid();

        var updateDto = new UpdateQuoteDto
        {
            ValidUntil = DateTime.UtcNow.AddDays(10)
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync((Quote?)null);

       
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateAsync(
                quoteId,
                updateDto));

       
        Assert.Contains(
            "Teklif bulunamadı",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task UpdateAsync_ShouldThrowBadRequest_WhenQuoteIsAccepted()
    {
        
        var quoteId = Guid.NewGuid();

        var originalValidUntil = DateTime.UtcNow.AddDays(5);

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-015",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Accepted,
            ValidUntil = originalValidUntil,
            IsDeleted = false
        };

        var updateDto = new UpdateQuoteDto
        {
            ValidUntil = DateTime.UtcNow.AddDays(20)
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.UpdateAsync(
                quoteId,
                updateDto));

        
        Assert.Contains(
            "Kabul edilmiş teklif güncellenemez",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            originalValidUntil,
            quote.ValidUntil);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task UpdateAsync_ShouldThrowBadRequest_WhenQuoteIsCancelled()
    {
       
        var quoteId = Guid.NewGuid();

        var originalValidUntil = DateTime.UtcNow.AddDays(5);

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-016",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Cancelled,
            ValidUntil = originalValidUntil,
            IsDeleted = false
        };

        var updateDto = new UpdateQuoteDto
        {
            ValidUntil = DateTime.UtcNow.AddDays(20)
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

        
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.UpdateAsync(
                quoteId,
                updateDto));

        
        Assert.Contains(
            "İptal edilmiş teklif güncellenemez",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            originalValidUntil,
            quote.ValidUntil);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task UpdateAsync_ShouldUpdateValidUntil_WhenQuoteIsValid()
    {
        
        var quoteId = Guid.NewGuid();

        var oldValidUntil = DateTime.UtcNow.AddDays(5);
        var newValidUntil = DateTime.UtcNow.AddDays(20);

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-017",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Offered,
            ValidUntil = oldValidUntil,
            IsDeleted = false
        };

        var updateDto = new UpdateQuoteDto
        {
            ValidUntil = newValidUntil
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

        _quoteRepositoryMock
            .Setup(x => x.UpdateAsync(quote))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        
        await _service.UpdateAsync(
            quoteId,
            updateDto);

        
        Assert.Equal(
            newValidUntil,
            quote.ValidUntil);

        Assert.NotNull(quote.UpdatedDate);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(quote),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task CreateAsync_ShouldThrowNotFound_WhenCustomerDoesNotExist()
    {
        
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var dto = new CreateQuoteDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            ValidUntil = DateTime.UtcNow.AddDays(10)
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync((Customer?)null);

        
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateAsync(dto));

        
        Assert.Contains(
            "Müşteri bulunamadı",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        _customerRepositoryMock.Verify(
            x => x.GetByIdAsync(customerId),
            Times.Once);

        _quoteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task CreateAsync_ShouldThrowNotFound_WhenVehicleDoesNotExist()
    {
       
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var dto = new CreateQuoteDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            ValidUntil = DateTime.UtcNow.AddDays(10)
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

        
        Assert.Contains(
            "Araç bulunamadı",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        _customerRepositoryMock.Verify(
            x => x.GetByIdAsync(customerId),
            Times.Once);

        _vehicleRepositoryMock.Verify(
            x => x.GetByIdAsync(vehicleId),
            Times.Once);

        _quoteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task CreateAsync_ShouldThrowBadRequest_WhenVehicleIsDeleted()
    {
        
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var dto = new CreateQuoteDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            ValidUntil = DateTime.UtcNow.AddDays(10)
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
            IsDeleted = false,
            IsActive = true,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year - 4
        };

        _customerRepositoryMock
     .Setup(x => x.GetByIdAsync(customerId))
     .ReturnsAsync(customer);

        vehicle.IsDeleted = true;   

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _pricingServiceMock
     .Setup(x => x.CalculateAsync(
         It.IsAny<PricingRequest>(),
         It.IsAny<CancellationToken>()))
      .ReturnsAsync(
         new PricingCalculation
         {
             MarketValue = 1_000_000m,
             BaseRate = 0.02m,
             AgeFactor = 1.10m,
             BasePremium = 20_000m,
             RiskAdjustedPremium = 22_000m,
             Coverages = Array.Empty<PricingCoverageResult>(),
             CoveragePremium = 0m,
             TotalPremium = 22_000m
         });


        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateAsync(dto));

        
        Assert.Contains(
            "Silinmiş bir araç için teklif oluşturulamaz",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.True(vehicle.IsDeleted);

        _quoteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task CreateAsync_ShouldThrowBadRequest_WhenVehicleIsInactive()
    {
        
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var dto = new CreateQuoteDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            ValidUntil = DateTime.UtcNow.AddDays(10)
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
            IsDeleted = false,
            IsActive = true,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year - 4
        };

        _customerRepositoryMock
    .Setup(x => x.GetByIdAsync(customerId))
    .ReturnsAsync(customer);

        vehicle.IsActive = false;   

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _pricingServiceMock
      .Setup(x => x.CalculateAsync(
          It.IsAny<PricingRequest>(),
          It.IsAny<CancellationToken>()))
        .ReturnsAsync(
          new PricingCalculation
          {
              MarketValue = 1_000_000m,
              BaseRate = 0.02m,
              AgeFactor = 1.10m,
              BasePremium = 20_000m,
              RiskAdjustedPremium = 22_000m,
              Coverages = Array.Empty<PricingCoverageResult>(),
              CoveragePremium = 0m,
              TotalPremium = 22_000m
          });


        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateAsync(dto));

        
        Assert.Contains(
            "Pasif bir araç için teklif oluşturulamaz",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.False(vehicle.IsActive);

        _quoteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task CreateAsync_ShouldThrowBadRequest_WhenVehicleBelongsToAnotherCustomer()
    {
        
        var customerId = Guid.NewGuid();
        var anotherCustomerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var dto = new CreateQuoteDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            ValidUntil = DateTime.UtcNow.AddDays(10)
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
            IsDeleted = false,
            IsActive = true,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year - 4
        };

        _customerRepositoryMock
    .Setup(x => x.GetByIdAsync(customerId))
    .ReturnsAsync(customer);

        vehicle.CustomerId = anotherCustomerId;   

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _pricingServiceMock
      .Setup(x => x.CalculateAsync(
          It.IsAny<PricingRequest>(),
          It.IsAny<CancellationToken>()))
      .ReturnsAsync(
        new PricingCalculation
        {
            MarketValue = 1_000_000m,
            BaseRate = 0.02m,
            AgeFactor = 1.10m,
            BasePremium = 20_000m,
            RiskAdjustedPremium = 22_000m,
            Coverages = Array.Empty<PricingCoverageResult>(),
            CoveragePremium = 0m,
            TotalPremium = 22_000m
        });

        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _service.CreateAsync(dto));

        
        Assert.Contains(
            "Seçilen araç bu müşteriye ait değildir",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.NotEqual(
            customerId,
            vehicle.CustomerId);

        _quoteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task CreateAsync_ShouldCreateQuote_WhenDataIsValid()
    {
        
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var validUntil = DateTime.UtcNow.AddDays(10);

        var dto = new CreateQuoteDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            ValidUntil = validUntil
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
            IsDeleted = false,
            IsActive = true,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year - 4
        };

        _customerRepositoryMock
     .Setup(x => x.GetByIdAsync(customerId))
     .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _pricingServiceMock
     .Setup(x => x.CalculateAsync(
         It.IsAny<PricingRequest>(),
         It.IsAny<CancellationToken>()))
             .ReturnsAsync(
                new PricingCalculation
                {
                    MarketValue = 1_000_000m,
                    BaseRate = 0.02m,
                    AgeFactor = 1.10m,
                    BasePremium = 20_000m,
                    RiskAdjustedPremium = 22_000m,
                    Coverages = Array.Empty<PricingCoverageResult>(),
                    CoveragePremium = 0m,
                    TotalPremium = 22_000m
                });
        _quoteRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Quote>()))
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
            validUntil,
            result.ValidUntil);

        Assert.Equal(
            QuoteStatus.Draft,
            result.Status);

        Assert.Equal(
               22000m,
               result.PremiumAmount);

        Assert.False(
            string.IsNullOrWhiteSpace(result.QuoteNumber));

        Assert.StartsWith(
            "KLF-",
            result.QuoteNumber);

        _quoteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Quote>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task CreateAsync_ShouldCreateQuoteWithCoverages_WhenCoveragesAreSelected()
    {
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var coverage1Id = Guid.NewGuid();
        var coverage2Id = Guid.NewGuid();

        var validUntil =
            DateTime.UtcNow.AddDays(10);

        var dto = new CreateQuoteDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            ValidUntil = validUntil,
            CoverageIds = new[]
            {
            coverage1Id,
            coverage2Id
        }
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
            IsDeleted = false,
            IsActive = true,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year - 4
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _pricingServiceMock
                 .Setup(x => x.CalculateAsync(
                 It.IsAny<PricingRequest>(),
                 It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PricingCalculation
                {
                    MarketValue = 1_000_000m,
                    BaseRate = 0.02m,
                    AgeFactor = 1.10m,
                    BasePremium = 20_000m,
                    RiskAdjustedPremium = 22_000m,

                    Coverages = new[]
                    {
                    new PricingCoverageResult
                    {
                        CoverageId = coverage1Id,
                        CoverageName = "Cam Kırılması",
                        CalculatedPrice = 900m,
                        Limit = 50_000m
                    },

                    new PricingCoverageResult
                    {
                        CoverageId = coverage2Id,
                        CoverageName = "Hırsızlık",
                        CalculatedPrice = 2_000m,
                        Limit = null
                    }
                    },

                    CoveragePremium = 2_900m,
                    TotalPremium = 24_900m
                });

        _quoteRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Quote>()))
            .Returns(Task.CompletedTask);

        _quoteCoverageRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<QuoteCoverage>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(3);

        var result =
            await _service.CreateAsync(dto);

        Assert.NotNull(result);

        Assert.Equal(
            24_900m,
            result.PremiumAmount);

        _quoteCoverageRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<QuoteCoverage>(qc =>
                    qc.QuoteId == result.Id &&
                    qc.CoverageId == coverage1Id &&
                    qc.CalculatedPrice == 900m &&
                    qc.Limit == 50_000m)),
            Times.Once);

        _quoteCoverageRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<QuoteCoverage>(qc =>
                    qc.QuoteId == result.Id &&
                    qc.CoverageId == coverage2Id &&
                    qc.CalculatedPrice == 2_000m &&
                    qc.Limit == null)),
            Times.Once);

        _quoteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Quote>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task CreateAsync_ShouldUseClaimsCountFromPreviousPolicy()
    {
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var previousPolicyId = Guid.NewGuid();

        var customer = new Customer
        {
            Id = customerId,
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            City = "Istanbul",
            IsDeleted = false,
            IsActive = true
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year,
            IsDeleted = false,
            IsActive = true
        };

        var previousPolicy = new PreviousPolicy
        {
            Id = previousPolicyId,
            CustomerId = customerId,
            PreviousInsurer = "ABC Sigorta",
            PolicyNumber = "POL-001",
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow,
            ClaimsCount = 3,
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _unitOfWorkMock
            .Setup(x => x.PreviousPolicies.GetByIdAsync(previousPolicyId))
            .ReturnsAsync(previousPolicy);

        _pricingServiceMock
            .Setup(x => x.CalculateAsync(
                It.IsAny<PricingRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingCalculation
            {
                MarketValue = 1_000_000m,
                TotalPremium = 26_000m,
                Coverages = Array.Empty<PricingCoverageResult>()
            });

        _quoteRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Quote>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        await _service.CreateAsync(
            new CreateQuoteDto
            {
                CustomerId = customerId,
                VehicleId = vehicleId,
                PreviousPolicyId = previousPolicyId,
                ClaimsCount = 0,
                Usage = "PRIVATE",
                PackageId = null,
                Deductible = 0,
                CoverageIds = Array.Empty<Guid>(),
                ValidUntil = DateTime.UtcNow.AddDays(10)
            });

        _pricingServiceMock.Verify(
            x => x.CalculateAsync(
                It.Is<PricingRequest>(r =>
                    r.ClaimsCount == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowNotFound_WhenPreviousPolicyDoesNotExist()
    {
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var previousPolicyId = Guid.NewGuid();

        var customer = new Customer
        {
            Id = customerId,
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            City = "Istanbul",
            IsDeleted = false,
            IsActive = true
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year,
            IsDeleted = false,
            IsActive = true
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _unitOfWorkMock
            .Setup(x => x.PreviousPolicies.GetByIdAsync(previousPolicyId))
            .ReturnsAsync((PreviousPolicy?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _service.CreateAsync(
                    new CreateQuoteDto
                    {
                        CustomerId = customerId,
                        VehicleId = vehicleId,
                        PreviousPolicyId = previousPolicyId,
                        ClaimsCount = 0,
                        Usage = "PRIVATE",
                        Deductible = 0,
                        CoverageIds = Array.Empty<Guid>(),
                        ValidUntil = DateTime.UtcNow.AddDays(10)
                    }));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowBadRequest_WhenPreviousPolicyBelongsToAnotherCustomer()
    {
        var customerId = Guid.NewGuid();
        var anotherCustomerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var previousPolicyId = Guid.NewGuid();

        var customer = new Customer
        {
            Id = customerId,
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            City = "Istanbul",
            IsDeleted = false,
            IsActive = true
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year,
            IsDeleted = false,
            IsActive = true
        };

        var previousPolicy = new PreviousPolicy
        {
            Id = previousPolicyId,
            CustomerId = anotherCustomerId,
            PreviousInsurer = "ABC Sigorta",
            PolicyNumber = "POL-002",
            StartDate = DateTime.UtcNow.AddYears(-1),
            EndDate = DateTime.UtcNow,
            ClaimsCount = 2,
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _unitOfWorkMock
            .Setup(x => x.PreviousPolicies.GetByIdAsync(previousPolicyId))
            .ReturnsAsync(previousPolicy);

        await Assert.ThrowsAsync<BadRequestException>(
            () =>
                _service.CreateAsync(
                    new CreateQuoteDto
                    {
                        CustomerId = customerId,
                        VehicleId = vehicleId,
                        PreviousPolicyId = previousPolicyId,
                        ClaimsCount = 0,
                        Usage = "PRIVATE",
                        Deductible = 0,
                        CoverageIds = Array.Empty<Guid>(),
                        ValidUntil = DateTime.UtcNow.AddDays(10)
                    }));
    }
    [Fact]
    public async Task DeleteAsync_ShouldThrowNotFound_WhenQuoteDoesNotExist()
    {
        
        var quoteId = Guid.NewGuid();

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync((Quote?)null);

       
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.DeleteAsync(quoteId));

        
        Assert.Contains(
            "Teklif bulunamadı",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task DeleteAsync_ShouldThrowNotFound_WhenQuoteIsAlreadyDeleted()
    {
        
        var quoteId = Guid.NewGuid();

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-025",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Offered,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsDeleted = true
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

        
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.DeleteAsync(quoteId));

        
        Assert.Contains(
            "Teklif bulunamadı",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.True(quote.IsDeleted);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Quote>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }
    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteQuote_WhenQuoteExists()
    {
        
        var quoteId = Guid.NewGuid();

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-026",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Offered,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsDeleted = false
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdAsync(quoteId))
            .ReturnsAsync(quote);

        _quoteRepositoryMock
            .Setup(x => x.UpdateAsync(quote))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        
        await _service.DeleteAsync(quoteId);

        
        Assert.True(quote.IsDeleted);

        Assert.NotNull(quote.DeletedDate);

        _quoteRepositoryMock.Verify(
            x => x.UpdateAsync(quote),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenQuoteDoesNotExist()
    {
        
        var quoteId = Guid.NewGuid();

        _quoteRepositoryMock
            .Setup(x => x.GetByIdIncludingDetailsAsync(quoteId))
            .ReturnsAsync((Quote?)null);

        
        var result = await _service.GetByIdAsync(quoteId);

       
        Assert.Null(result);

        _quoteRepositoryMock.Verify(
            x => x.GetByIdIncludingDetailsAsync(quoteId),
            Times.Once);
    }
    [Fact]
    public async Task GetByIdAsync_ShouldReturnQuote_WhenQuoteExists()
    {
        
        var quoteId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var validUntil = DateTime.UtcNow.AddDays(10);

        var quote = new Quote
        {
            Id = quoteId,
            CustomerId = customerId,
            VehicleId = vehicleId,
            QuoteNumber = "KLF-TEST-028",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Offered,
            ValidUntil = validUntil,
            IsDeleted = false
        };

        _quoteRepositoryMock
            .Setup(x => x.GetByIdIncludingDetailsAsync(quoteId))
            .ReturnsAsync(quote);

        
        var result = await _service.GetByIdAsync(quoteId);

        
        Assert.NotNull(result);

        Assert.Equal(
            quoteId,
            result.Id);

        Assert.Equal(
            customerId,
            result.CustomerId);

        Assert.Equal(
            vehicleId,
            result.VehicleId);

        Assert.Equal(
            "KLF-TEST-028",
            result.QuoteNumber);

        Assert.Equal(
               25000m,
               result.PremiumAmount);

        Assert.Equal(
            QuoteStatus.Offered,
            result.Status);

        Assert.Equal(
            validUntil,
            result.ValidUntil);

        _quoteRepositoryMock.Verify(
            x => x.GetByIdIncludingDetailsAsync(quoteId),
            Times.Once);
    }
    [Fact]
    public async Task GetAllAsync_ShouldExcludeDeletedQuotes()
    {
        
        var activeQuote = new Quote
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-029-A",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Offered,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsDeleted = false
        };

        var deletedQuote = new Quote
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-029-D",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Cancelled,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsDeleted = true
        };

        _quoteRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Quote>
            {
            activeQuote,
            deletedQuote
            });

        
        var result = (await _service.GetAllAsync()).ToList();

        
        Assert.Single(result);

        Assert.Equal(
            activeQuote.Id,
            result[0].Id);

        Assert.Equal(
            "KLF-TEST-029-A",
            result[0].QuoteNumber);

        Assert.DoesNotContain(
            result,
            x => x.Id == deletedQuote.Id);

        _quoteRepositoryMock.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }
    [Fact]
    public async Task GetAllAsync_ShouldReturnActiveQuotes()
    {
       
        var quote1 = new Quote
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-030-1",
            PremiumAmount = 25000m,
            Status = QuoteStatus.Draft,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            IsDeleted = false
        };

        var quote2 = new Quote
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            QuoteNumber = "KLF-TEST-030-2",
            PremiumAmount = 30000m,
            Status = QuoteStatus.Offered,
            ValidUntil = DateTime.UtcNow.AddDays(20),
            IsDeleted = false
        };

        _quoteRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Quote>
            {
            quote1,
            quote2
            });

        
        var result = (await _service.GetAllAsync()).ToList();

        
        Assert.Equal(2, result.Count);

        Assert.Equal(
            quote1.Id,
            result[0].Id);

        Assert.Equal(
            quote1.QuoteNumber,
            result[0].QuoteNumber);

        Assert.Equal(
            quote1.CustomerId,
            result[0].CustomerId);

        Assert.Equal(
            quote1.VehicleId,
            result[0].VehicleId);

        Assert.Equal(
            quote1.PremiumAmount,
            result[0].PremiumAmount);

        Assert.Equal(
            quote1.Status,
            result[0].Status);

        Assert.Equal(
            quote1.ValidUntil,
            result[0].ValidUntil);

        Assert.Equal(
            quote2.Id,
            result[1].Id);

        Assert.Equal(
            quote2.QuoteNumber,
            result[1].QuoteNumber);

        _quoteRepositoryMock.Verify(
            x => x.GetAllAsync(),
            Times.Once);
    }
    [Fact]
    public async Task CalculateAsync_ShouldReturnPricingCalculationWithoutCreatingQuote()
    {
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var customer = new Customer
        {
            Id = customerId,
            DateOfBirth = DateTime.UtcNow.AddYears(-31),
            City = "Istanbul",
            IsDeleted = false,
            IsActive = true
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year,
            IsDeleted = false,
            IsActive = true
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _pricingServiceMock
            .Setup(x => x.CalculateAsync(
                It.Is<PricingRequest>(r =>
                    r.MarketValue == 1_000_000m &&
                    r.ModelYear == DateTime.UtcNow.Year &&
                    r.DriverAge == 31 &&
                    r.Usage == "PRIVATE" &&
                    r.ClaimsCount == 0 &&
                    r.Region == "NORMAL" &&
                    r.Deductible == 0 &&
                    r.CoverageIds.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingCalculation
            {
                MarketValue = 1_000_000m,
                BasePremium = 20_000m,
                RiskAdjustedPremium = 20_000m,
                CoveragePremium = 0m,
                TotalPremium = 20_000m,
                Coverages = Array.Empty<PricingCoverageResult>()
            });

        var result =
            await _service.CalculateAsync(
                new CreateQuoteDto
                {
                    CustomerId = customerId,
                    VehicleId = vehicleId,
                    Usage = "PRIVATE",
                    ClaimsCount = 0,
                    Deductible = 0,
                    CoverageIds = Array.Empty<Guid>(),
                    ValidUntil = DateTime.UtcNow.AddDays(10)
                });

        Assert.Equal(1_000_000m, result.MarketValue);
        Assert.Equal(20_000m, result.TotalPremium);

        _quoteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Quote>()),
            Times.Never);

        _pricingServiceMock.Verify(
            x => x.CalculateAsync(
                It.IsAny<PricingRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    [Fact]
    public async Task CalculateAndCreateAsync_ShouldUseSamePremium()
    {
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var customer = new Customer
        {
            Id = customerId,
            DateOfBirth = DateTime.UtcNow.AddYears(-31),
            City = "Istanbul",
            IsActive = true,
            IsDeleted = false
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year,
            IsActive = true,
            IsDeleted = false
        };

        var dto = new CreateQuoteDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            ValidUntil = DateTime.UtcNow.AddDays(10),
            CoverageIds = Array.Empty<Guid>(),
            Usage = "PRIVATE",
            ClaimsCount = 0,
            PackageId = null,
            Deductible = 0,
            PreviousPolicyId = null
        };

        var pricingCalculation = new PricingCalculation
        {
            MarketValue = 1_000_000m,
            BaseRate = 0.02m,
            AgeFactor = 1.00m,
            UsageFactor = 1.00m,
            DriverFactor = 1.00m,
            ClaimsFactor = 1.00m,
            RegionFactor = 1.00m,
            PackageFactor = 1.00m,
            DeductibleFactor = 1.00m,
            BasePremium = 20_000m,
            RiskAdjustedPremium = 20_000m,
            CoveragePremium = 2_900m,
            TotalPremium = 22_900m,
            Coverages = Array.Empty<PricingCoverageResult>()
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _pricingServiceMock
    .Setup(x => x.CalculateAsync(
        It.IsAny<PricingRequest>(),
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(pricingCalculation);

        _quoteRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Quote>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var calculated =
            await _service.CalculateAsync(dto);

        var created =
            await _service.CreateAsync(dto);

        Assert.Equal(
            calculated.TotalPremium,
            created.PremiumAmount);

        Assert.Equal(
            22_900m,
            calculated.TotalPremium);

        Assert.Equal(
            22_900m,
            created.PremiumAmount);
    }
    [Fact]
    public async Task CalculateAsync_ShouldThrowBadRequest_WhenValidUntilIsInThePast()
    {
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var customer = new Customer
        {
            Id = customerId,
            DateOfBirth = DateTime.UtcNow.AddYears(-31),
            City = "Istanbul",
            IsActive = true,
            IsDeleted = false
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year,
            IsActive = true,
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        var dto = new CreateQuoteDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            ValidUntil = DateTime.UtcNow.AddDays(-1),
            CoverageIds = Array.Empty<Guid>(),
            Usage = "PRIVATE",
            ClaimsCount = 0,
            Deductible = 0
        };

        var exception =
            await Assert.ThrowsAsync<BadRequestException>(
                () => _service.CalculateAsync(dto));

        Assert.Contains(
            "Teklif geçerlilik tarihi gelecekte olmalıdır.",
            exception.Message);
    }
    [Fact]
    public async Task CreateAsync_ShouldThrowBadRequest_WhenValidUntilIsInThePast()
    {
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var customer = new Customer
        {
            Id = customerId,
            DateOfBirth = DateTime.UtcNow.AddYears(-31),
            City = "Istanbul",
            IsActive = true,
            IsDeleted = false
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year,
            IsActive = true,
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        var dto = new CreateQuoteDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            ValidUntil = DateTime.UtcNow.AddDays(-1),
            CoverageIds = Array.Empty<Guid>(),
            Usage = "PRIVATE",
            ClaimsCount = 0,
            Deductible = 0
        };

        var exception =
            await Assert.ThrowsAsync<BadRequestException>(
                () => _service.CreateAsync(dto));

        Assert.Contains(
            "Teklif geçerlilik tarihi gelecekte olmalıdır.",
            exception.Message);

        _pricingServiceMock.Verify(
            x => x.CalculateAsync(
                It.IsAny<PricingRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
    [Fact]
    public async Task CalculateAsync_ShouldUsePreviousPolicyClaimsCount()
    {
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var previousPolicyId = Guid.NewGuid();

        var customer = new Customer
        {
            Id = customerId,
            DateOfBirth = DateTime.UtcNow.AddYears(-31),
            City = "Istanbul",
            IsActive = true,
            IsDeleted = false
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year,
            IsActive = true,
            IsDeleted = false
        };

        var previousPolicy = new PreviousPolicy
        {
            Id = previousPolicyId,
            CustomerId = customerId,
            ClaimsCount = 2,
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _unitOfWorkMock
            .Setup(x => x.PreviousPolicies
                .GetByIdAsync(previousPolicyId))
            .ReturnsAsync(previousPolicy);

        _pricingServiceMock
            .Setup(x => x.CalculateAsync(
                It.Is<PricingRequest>(r =>
                    r.ClaimsCount == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingCalculation
            {
                MarketValue = 1_000_000m,
                BasePremium = 20_000m,
                RiskAdjustedPremium = 24_000m,
                CoveragePremium = 0m,
                TotalPremium = 24_000m
            });

        var dto = new CreateQuoteDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            PreviousPolicyId = previousPolicyId,
            ClaimsCount = 0,
            Usage = "PRIVATE",
            Deductible = 0,
            CoverageIds = Array.Empty<Guid>(),
            ValidUntil = DateTime.UtcNow.AddDays(10)
        };

        var result =
            await _service.CalculateAsync(dto);

        Assert.Equal(
            24_000m,
            result.TotalPremium);

        _pricingServiceMock.Verify(
            x => x.CalculateAsync(
                It.Is<PricingRequest>(r =>
                    r.ClaimsCount == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    [Fact]
    public async Task CalculateAsync_ShouldUseDtoClaimsCount_WhenPreviousPolicyIsNotProvided()
    {
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var customer = new Customer
        {
            Id = customerId,
            DateOfBirth = DateTime.UtcNow.AddYears(-31),
            City = "Istanbul",
            IsActive = true,
            IsDeleted = false
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year,
            IsActive = true,
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _pricingServiceMock
            .Setup(x => x.CalculateAsync(
                It.Is<PricingRequest>(r =>
                    r.ClaimsCount == 3 &&
                    r.Usage == "COMMERCIAL"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PricingCalculation
            {
                MarketValue = 1_000_000m,
                BasePremium = 20_000m,
                RiskAdjustedPremium = 26_000m,
                CoveragePremium = 0m,
                TotalPremium = 26_000m
            });

        var dto = new CreateQuoteDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            ClaimsCount = 3,
            Usage = "COMMERCIAL",
            Deductible = 0,
            CoverageIds = Array.Empty<Guid>(),
            ValidUntil = DateTime.UtcNow.AddDays(10)
        };

        var result =
            await _service.CalculateAsync(dto);

        Assert.Equal(
            26_000m,
            result.TotalPremium);

        _pricingServiceMock.Verify(
            x => x.CalculateAsync(
                It.Is<PricingRequest>(r =>
                    r.ClaimsCount == 3 &&
                    r.Usage == "COMMERCIAL"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    [Fact]
    public async Task CalculateAsync_ShouldNotDuplicatePackageDefaultCoverage()
    {
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var coverageId = Guid.NewGuid();

        var customer = new Customer
        {
            Id = customerId,
            DateOfBirth = DateTime.UtcNow.AddYears(-31),
            City = "Istanbul",
            IsActive = true,
            IsDeleted = false
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year,
            IsActive = true,
            IsDeleted = false
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        _packageCoverageRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<PackageCoverage, bool>>>()))
            .ReturnsAsync(
                new[]
                {
                new PackageCoverage
                {
                    Id = Guid.NewGuid(),
                    InsurancePackageId = packageId,
                    CoverageId = coverageId,
                    IsDefault = true,
                    IsDeleted = false
                }
                });

        _pricingServiceMock
            .Setup(x => x.CalculateAsync(
                It.Is<PricingRequest>(r =>
                    r.CoverageIds.Count == 1 &&
                    r.CoverageIds.Count(x => x == coverageId) == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new PricingCalculation
                {
                    MarketValue = 1_000_000m,
                    BasePremium = 20_000m,
                    RiskAdjustedPremium = 20_000m,
                    CoveragePremium = 900m,
                    TotalPremium = 20_900m
                });

        var result =
            await _service.CalculateAsync(
                new CreateQuoteDto
                {
                    CustomerId = customerId,
                    VehicleId = vehicleId,
                    PackageId = packageId,
                    CoverageIds = new[] { coverageId },
                    Usage = "PRIVATE",
                    ClaimsCount = 0,
                    Deductible = 0,
                    ValidUntil = DateTime.UtcNow.AddDays(10)
                });

        Assert.Equal(
            20_900m,
            result.TotalPremium);
    }
    [Fact]
    public async Task CreateAsync_ShouldCreatePricingSnapshot()
    {
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var customer = new Customer
        {
            Id = customerId,
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            City = "Istanbul",
            IsActive = true,
            IsDeleted = false
        };

        var vehicle = new Vehicle
        {
            Id = vehicleId,
            CustomerId = customerId,
            MarketValue = 1_000_000m,
            ModelYear = DateTime.UtcNow.Year - 4,
            IsActive = true,
            IsDeleted = false
        };

        var dto = new CreateQuoteDto
        {
            CustomerId = customerId,
            VehicleId = vehicleId,
            ValidUntil = DateTime.UtcNow.AddDays(7),
            Usage = "PRIVATE",
            ClaimsCount = 0,
            Deductible = 0
        };

        _customerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(customer);

        _vehicleRepositoryMock
            .Setup(x => x.GetByIdAsync(vehicleId))
            .ReturnsAsync(vehicle);

        var pricing = new PricingCalculation
        {
            MarketValue = 1_000_000m,
            BaseRate = 0.02m,
            AgeFactor = 1.10m,
            UsageFactor = 1.00m,
            DriverFactor = 1.00m,
            ClaimsFactor = 0.90m,
            RegionFactor = 1.00m,
            PackageFactor = 1.00m,
            DeductibleFactor = 1.00m,
            BasePremium = 20_000m,
            RiskAdjustedPremium = 19_800m,
            CoveragePremium = 0m,
            Discount = 0m,
            TotalPremium = 19_800m,
            Coverages = Array.Empty<PricingCoverageResult>()
        };

        _pricingServiceMock
    .Setup(x => x.CalculateAsync(
        It.IsAny<PricingRequest>(),
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(pricing);

        _quoteRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Quote>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result =
            await _service.CreateAsync(dto);

        Assert.NotNull(result);

        _unitOfWorkMock.Verify(
            x => x.QuotePricingSnapshots.AddAsync(
                It.Is<QuotePricingSnapshot>(snapshot =>
                    snapshot.MarketValue == pricing.MarketValue &&
                    snapshot.BaseRate == pricing.BaseRate &&
                    snapshot.AgeFactor == pricing.AgeFactor &&
                    snapshot.UsageFactor == pricing.UsageFactor &&
                    snapshot.DriverFactor == pricing.DriverFactor &&
                    snapshot.ClaimsFactor == pricing.ClaimsFactor &&
                    snapshot.RegionFactor == pricing.RegionFactor &&
                    snapshot.PackageFactor == pricing.PackageFactor &&
                    snapshot.DeductibleFactor == pricing.DeductibleFactor &&
                    snapshot.CoveragePremium == pricing.CoveragePremium &&
                    snapshot.Discount == pricing.Discount &&
                    snapshot.FinalPremium == pricing.TotalPremium
                )),
            Times.Once);
    }
}