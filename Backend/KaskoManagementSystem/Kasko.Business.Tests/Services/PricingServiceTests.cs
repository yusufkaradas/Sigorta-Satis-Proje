using Kasko.Business.Pricing;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Kasko.Entities.Enums;
using Moq;

namespace Kasko.Business.Tests.Services;

public class PricingServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPricingRuleRepository> _pricingRuleRepositoryMock;

    private readonly PricingService _service;

    public PricingServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _pricingRuleRepositoryMock =
            new Mock<IPricingRuleRepository>();

        _unitOfWorkMock
            .Setup(x => x.PricingRules)
            .Returns(_pricingRuleRepositoryMock.Object);

        _service = new PricingService(
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CalculateAsync_ShouldCalculateCorrectly_ForVehicleAge0To2()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        var result =
            await _service.CalculateAsync(
                1_000_000m,
                DateTime.UtcNow.Year, Array.Empty<Guid>());

        Assert.Equal(
            0.02m,
            result.BaseRate);

        Assert.Equal(
            1.00m,
            result.AgeFactor);

        Assert.Equal(
            20_000m,
            result.BasePremium);

        Assert.Equal(
            20_000m,
            result.RiskAdjustedPremium);

        Assert.Equal(
            20_000m,
            result.TotalPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldApplyAgeFactor_ForVehicleAge3To5()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_3_5",
            ageFactor: 1.10m);

        var modelYear =
            DateTime.UtcNow.Year - 4;

        var result =
            await _service.CalculateAsync(
                1_000_000m,
                modelYear, Array.Empty<Guid>());

        Assert.Equal(
            1.10m,
            result.AgeFactor);

        Assert.Equal(
            20_000m,
            result.BasePremium);

        Assert.Equal(
            22_000m,
            result.RiskAdjustedPremium);

        Assert.Equal(
            22_000m,
            result.TotalPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldApplyAgeFactor_ForVehicleAge6To8()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_6_8",
            ageFactor: 1.20m);

        var modelYear =
            DateTime.UtcNow.Year - 7;

        var result =
            await _service.CalculateAsync(
                1_000_000m,
                modelYear, Array.Empty<Guid>());

        Assert.Equal(
            1.20m,
            result.AgeFactor);

        Assert.Equal(
            24_000m,
            result.TotalPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldApplyAgeFactor_ForVehicleAge9To12()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_9_12",
            ageFactor: 1.35m);

        var modelYear =
            DateTime.UtcNow.Year - 10;

        var result =
            await _service.CalculateAsync(
                1_000_000m,
                modelYear, Array.Empty<Guid>());

        Assert.Equal(
            1.35m,
            result.AgeFactor);

        Assert.Equal(
            27_000m,
            result.TotalPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldApplyAgeFactor_ForVehicleAge13Plus()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_13_15",
            ageFactor: 1.50m);

        var modelYear =
            DateTime.UtcNow.Year - 14;

        var result =
            await _service.CalculateAsync(
                1_000_000m,
                modelYear, Array.Empty<Guid>());

        Assert.Equal(
            1.50m,
            result.AgeFactor);

        Assert.Equal(
            30_000m,
            result.TotalPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldThrow_WhenMarketValueIsZero()
    {
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CalculateAsync(
                    0m,
                    DateTime.UtcNow.Year, Array.Empty<Guid>()));

        Assert.Contains(
            "Araç değeri 0'dan büyük olmalıdır.",
            exception.Message);
    }

    [Fact]
    public async Task CalculateAsync_ShouldThrow_WhenMarketValueIsNegative()
    {
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CalculateAsync(
                    -100m,
                    DateTime.UtcNow.Year, Array.Empty<Guid>()));

        Assert.Contains(
            "Araç değeri 0'dan büyük olmalıdır.",
            exception.Message);
    }

    [Fact]
    public async Task CalculateAsync_ShouldThrow_WhenBaseRateRuleDoesNotExist()
    {
        _pricingRuleRepositoryMock
            .Setup(x => x.GetByCodeAsync(
                "BASE_KASKO_RATE"))
            .ReturnsAsync((PricingRule?)null);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CalculateAsync(
                    1_000_000m,
                    DateTime.UtcNow.Year, Array.Empty<Guid>()));

        Assert.Contains(
            "BASE_KASKO_RATE",
            exception.Message);
    }

    private void SetupRules(
        decimal baseRate,
        string ageFactorCode,
        decimal ageFactor)
    {
        _pricingRuleRepositoryMock
            .Setup(x => x.GetByCodeAsync(
                "BASE_KASKO_RATE"))
            .ReturnsAsync(
                CreateRule(
                    "BASE_KASKO_RATE",
                    baseRate));

        _pricingRuleRepositoryMock
            .Setup(x => x.GetByCodeAsync(
                ageFactorCode))
            .ReturnsAsync(
                CreateRule(
                    ageFactorCode,
                    ageFactor));
    }

    private static PricingRule CreateRule(
        string code,
        decimal value)
    {
        return new PricingRule
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code,
            Value = value,
            IsActive = true,
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow
        };
    }
    [Fact]
    public async Task CalculateAsync_ShouldCalculateFixedCoverageCorrectly()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        var coverageId =
            Guid.NewGuid();

        _unitOfWorkMock
            .Setup(x => x.Coverages.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Coverage, bool>>>()))
            .ReturnsAsync(
                new[]
                {
                new Coverage
                {
                    Id = coverageId,
                    Name = "Cam Kırılması",
                    PricingType = CoveragePricingType.Fixed,
                    BasePrice = 900m,
                    IsActive = true,
                    IsDeleted = false
                }
                });

        var result =
            await _service.CalculateAsync(
                1_000_000m,
                DateTime.UtcNow.Year,
                new[] { coverageId });

        Assert.Single(result.Coverages);

        Assert.Equal(
            900m,
            result.Coverages[0].CalculatedPrice);

        Assert.Equal(
            900m,
            result.CoveragePremium);

        Assert.Equal(
            20_900m,
            result.TotalPremium);
    }
    [Fact]
    public async Task CalculateAsync_ShouldCalculatePercentageCoverageCorrectly()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        var coverageId =
            Guid.NewGuid();

        _unitOfWorkMock
            .Setup(x => x.Coverages.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Coverage, bool>>>()))
            .ReturnsAsync(
                new[]
                {
                new Coverage
                {
                    Id = coverageId,
                    Name = "Hırsızlık",
                    PricingType =
                        CoveragePricingType.PercentageOfVehicleValue,
                    BasePrice = 0m,
                    Rate = 0.20m,
                    IsActive = true,
                    IsDeleted = false
                }
                });

        var result =
            await _service.CalculateAsync(
                1_000_000m,
                DateTime.UtcNow.Year,
                new[] { coverageId });

        Assert.Single(result.Coverages);

        Assert.Equal(
            2_000m,
            result.Coverages[0].CalculatedPrice);

        Assert.Equal(
            2_000m,
            result.CoveragePremium);

        Assert.Equal(
            22_000m,
            result.TotalPremium);
    }
    [Fact]
    public async Task CalculateAsync_ShouldSumMultipleCoveragesCorrectly()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        var fixedCoverageId =
            Guid.NewGuid();

        var percentageCoverageId =
            Guid.NewGuid();

        _unitOfWorkMock
            .Setup(x => x.Coverages.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Coverage, bool>>>()))
            .ReturnsAsync(
                new[]
                {
                new Coverage
                {
                    Id = fixedCoverageId,
                    Name = "Cam Kırılması",
                    PricingType = CoveragePricingType.Fixed,
                    BasePrice = 900m,
                    IsActive = true,
                    IsDeleted = false
                },

                new Coverage
                {
                    Id = percentageCoverageId,
                    Name = "Hırsızlık",
                    PricingType =
                        CoveragePricingType.PercentageOfVehicleValue,
                    BasePrice = 0m,
                    Rate = 0.20m,
                    IsActive = true,
                    IsDeleted = false
                }
                });

        var result =
            await _service.CalculateAsync(
                1_000_000m,
                DateTime.UtcNow.Year,
                new[]
                {
                fixedCoverageId,
                percentageCoverageId
                });

        Assert.Equal(
            2,
            result.Coverages.Count);

        Assert.Equal(
            2_900m,
            result.CoveragePremium);

        Assert.Equal(
            22_900m,
            result.TotalPremium);
    }
    [Fact]
    public async Task CalculateAsync_ShouldHaveZeroCoveragePremium_WhenNoCoverageSelected()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        var result =
            await _service.CalculateAsync(
                1_000_000m,
                DateTime.UtcNow.Year,
                Array.Empty<Guid>());

        Assert.Empty(result.Coverages);

        Assert.Equal(
            0m,
            result.CoveragePremium);

        Assert.Equal(
            20_000m,
            result.TotalPremium);
    }
}