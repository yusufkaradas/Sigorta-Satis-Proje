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
        _unitOfWorkMock =
            new Mock<IUnitOfWork>();

        _pricingRuleRepositoryMock =
            new Mock<IPricingRuleRepository>();

        _unitOfWorkMock
            .Setup(x => x.PricingRules)
            .Returns(_pricingRuleRepositoryMock.Object);

        var coverageOptionRepositoryMock =
            new Mock<IGenericRepository<CoverageOption>>();

        coverageOptionRepositoryMock
            .Setup(x => x.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<CoverageOption, bool>>>()))
            .ReturnsAsync(new List<CoverageOption>());

        _service = new PricingService(
            _unitOfWorkMock.Object,
            coverageOptionRepositoryMock.Object);
    }
    [Fact]
    public async Task CalculateAsync_ShouldUseEffectiveDate()
    {
        var effectiveDate =
            new DateTime(2026, 8, 15);

        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        _pricingRuleRepositoryMock
            .Setup(x => x.GetApplicableRuleAsync(
                "BASE_KASKO_RATE",
                effectiveDate))
            .ReturnsAsync(
                CreateRule(
                    "BASE_KASKO_RATE",
                    0.02m));

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    Usage = "PRIVATE",
                    ClaimsCount = 0,
                    EffectiveDate = effectiveDate,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            0.02m,
            result.BaseRate);

        _pricingRuleRepositoryMock.Verify(
            x => x.GetApplicableRuleAsync(
                "BASE_KASKO_RATE",
                effectiveDate),
            Times.Once);
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
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    ClaimsCount = 1,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            0.02m,
            result.BaseRate);

        Assert.Equal(
            1.00m,
            result.AgeFactor);

        Assert.Equal(
            1.00m,
            result.UsageFactor);

        Assert.Equal(
            1.00m,
            result.DriverFactor);

        Assert.Equal(
            1.00m,
            result.ClaimsFactor);

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
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = modelYear,
                    ClaimsCount = 1,
                    CoverageIds = Array.Empty<Guid>()
                });

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
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = modelYear,
                    ClaimsCount = 1,
                    CoverageIds = Array.Empty<Guid>()
                });

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
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = modelYear,
                    ClaimsCount = 1,
                    CoverageIds = Array.Empty<Guid>()
                });

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
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = modelYear,
                    ClaimsCount = 1,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            1.50m,
            result.AgeFactor);

        Assert.Equal(
            30_000m,
            result.TotalPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldApplyPrivateUsageFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    Usage = "PRIVATE",
                    ClaimsCount = 1,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            1.00m,
            result.UsageFactor);

        Assert.Equal(
            20_000m,
            result.RiskAdjustedPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldApplyCommercialUsageFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        _pricingRuleRepositoryMock
    .Setup(x => x.GetApplicableRuleAsync(
        "USAGE_COMMERCIAL",
        It.IsAny<DateTime>()))
            .ReturnsAsync(
                CreateRule(
                    "USAGE_COMMERCIAL",
                    1.25m));

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    Usage = "COMMERCIAL",
                    ClaimsCount = 1,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            1.25m,
            result.UsageFactor);

        Assert.Equal(
            25_000m,
            result.RiskAdjustedPremium);

        Assert.Equal(
            25_000m,
            result.TotalPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldApplyRentalUsageFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        _pricingRuleRepositoryMock
     .Setup(x => x.GetApplicableRuleAsync(
         "USAGE_RENTAL",
         It.IsAny<DateTime>()))
             .ReturnsAsync(
                CreateRule(
                    "USAGE_RENTAL",
                    1.40m));

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    Usage = "RENTAL",
                    ClaimsCount = 1,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            1.40m,
            result.UsageFactor);

        Assert.Equal(
            28_000m,
            result.RiskAdjustedPremium);

        Assert.Equal(
            28_000m,
            result.TotalPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldApply25PlusDriverFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    DriverAge = 25,
                    Usage = "PRIVATE",
                    ClaimsCount = 1,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            1.00m,
            result.DriverFactor);

        Assert.Equal(
            20_000m,
            result.RiskAdjustedPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldApply21To24DriverFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        _pricingRuleRepositoryMock
    .Setup(x => x.GetApplicableRuleAsync(
        "DRIVER_21_24",
        It.IsAny<DateTime>()))
            .ReturnsAsync(
                CreateRule(
                    "DRIVER_21_24",
                    1.15m));

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    DriverAge = 22,
                    Usage = "PRIVATE",
                    ClaimsCount = 1,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            1.15m,
            result.DriverFactor);

        Assert.Equal(
            23_000m,
            result.RiskAdjustedPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldApply18To20DriverFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        _pricingRuleRepositoryMock
    .Setup(x => x.GetApplicableRuleAsync(
        "DRIVER_18_20",
        It.IsAny<DateTime>()))
            .ReturnsAsync(
                CreateRule(
                    "DRIVER_18_20",
                    1.30m));

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    DriverAge = 19,
                    Usage = "PRIVATE",
                    ClaimsCount = 1,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            1.30m,
            result.DriverFactor);

        Assert.Equal(
            26_000m,
            result.RiskAdjustedPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldThrow_WhenMarketValueIsZero()
    {
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CalculateAsync(
                    new PricingRequest
                    {
                        MarketValue = 0m,
                        ModelYear = DateTime.UtcNow.Year,
                        CoverageIds = Array.Empty<Guid>()
                    }));

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
                    new PricingRequest
                    {
                        MarketValue = -100m,
                        ModelYear = DateTime.UtcNow.Year,
                        CoverageIds = Array.Empty<Guid>()
                    }));

        Assert.Contains(
            "Araç değeri 0'dan büyük olmalıdır.",
            exception.Message);
    }

    [Fact]
    public async Task CalculateAsync_ShouldThrow_WhenBaseRateRuleDoesNotExist()
    {
        _pricingRuleRepositoryMock
    .Setup(x => x.GetApplicableRuleAsync(
        "BASE_KASKO_RATE",
        It.IsAny<DateTime>()))
    .ReturnsAsync(
        (PricingRule?)null);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CalculateAsync(
                    new PricingRequest
                    {
                        MarketValue = 1_000_000m,
                        ModelYear = DateTime.UtcNow.Year,
                        CoverageIds = Array.Empty<Guid>()
                    }));

        Assert.Contains(
            "BASE_KASKO_RATE",
            exception.Message);
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
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    ClaimsCount = 1,
                    CoverageIds = new[] { coverageId }
                });

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
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    ClaimsCount = 1,
                    CoverageIds = new[] { coverageId }
                });

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
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    ClaimsCount = 1,
                    CoverageIds = new[]
                    {
                        fixedCoverageId,
                        percentageCoverageId
                    }
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
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    ClaimsCount = 1,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Empty(result.Coverages);

        Assert.Equal(
            0m,
            result.CoveragePremium);

        Assert.Equal(
            20_000m,
            result.TotalPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldApplyZeroClaimsFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    DriverAge = 25,
                    Usage = "PRIVATE",
                    ClaimsCount = 0,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            0.90m,
            result.ClaimsFactor);

        Assert.Equal(
            18_000m,
            result.RiskAdjustedPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldApplyOneClaimFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        _pricingRuleRepositoryMock
    .Setup(x => x.GetApplicableRuleAsync(
        "CLAIMS_1",
        It.IsAny<DateTime>()))
            .ReturnsAsync(
                CreateRule(
                    "CLAIMS_1",
                    1.00m));

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    DriverAge = 25,
                    Usage = "PRIVATE",
                    ClaimsCount = 1,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            1.00m,
            result.ClaimsFactor);

        Assert.Equal(
            20_000m,
            result.RiskAdjustedPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldApplyTwoClaimsFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        _pricingRuleRepositoryMock
    .Setup(x => x.GetApplicableRuleAsync(
        "CLAIMS_2",
        It.IsAny<DateTime>()))
            .ReturnsAsync(
                CreateRule(
                    "CLAIMS_2",
                    1.15m));

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    DriverAge = 25,
                    Usage = "PRIVATE",
                    ClaimsCount = 2,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            1.15m,
            result.ClaimsFactor);

        Assert.Equal(
            23_000m,
            result.RiskAdjustedPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldApplyThreeOrMoreClaimsFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        _pricingRuleRepositoryMock
     .Setup(x => x.GetApplicableRuleAsync(
         "CLAIMS_3_PLUS",
         It.IsAny<DateTime>()))
             .ReturnsAsync(
                CreateRule(
                    "CLAIMS_3_PLUS",
                    1.30m));

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    DriverAge = 25,
                    Usage = "PRIVATE",
                    ClaimsCount = 3,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            1.30m,
            result.ClaimsFactor);

        Assert.Equal(
            26_000m,
            result.RiskAdjustedPremium);
    }

    private void SetupRules(
        decimal baseRate,
        string ageFactorCode,
        decimal ageFactor)
    {
        _unitOfWorkMock
            .Setup(x => x.PackageCoverages.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<PackageCoverage, bool>>>()))
            .ReturnsAsync(new List<PackageCoverage>());

        _pricingRuleRepositoryMock
            .Setup(x => x.GetApplicableRuleAsync(
                        "BASE_KASKO_RATE",
                        It.IsAny<DateTime>()))
            .ReturnsAsync(
                CreateRule(
                    "BASE_KASKO_RATE",
                    baseRate));

        _pricingRuleRepositoryMock
            .Setup(x => x.GetApplicableRuleAsync(
                ageFactorCode, It.IsAny<DateTime>()))
            .ReturnsAsync(
                CreateRule(
                    ageFactorCode,
                    ageFactor));

        _pricingRuleRepositoryMock
            .Setup(x => x.GetApplicableRuleAsync(
    "USAGE_PRIVATE",
    It.IsAny<DateTime>()))
            .ReturnsAsync(
                CreateRule(
                    "USAGE_PRIVATE",
                    1.00m));

        _pricingRuleRepositoryMock
            .Setup(x => x.GetApplicableRuleAsync(
    "DRIVER_25_PLUS",
    It.IsAny<DateTime>()))
            .ReturnsAsync(
                CreateRule(
                    "DRIVER_25_PLUS",
                    1.00m));

        _pricingRuleRepositoryMock
            .Setup(x => x.GetApplicableRuleAsync(
    "CLAIMS_0",
    It.IsAny<DateTime>()))
            .ReturnsAsync(
                CreateRule(
                    "CLAIMS_0",
                    0.90m));

        _pricingRuleRepositoryMock
            .Setup(x => x.GetApplicableRuleAsync(
             "CLAIMS_1",
             It.IsAny<DateTime>()))
            .ReturnsAsync(
                CreateRule(
                    "CLAIMS_1",
                    1.00m));
        _pricingRuleRepositoryMock
    .Setup(x => x.GetApplicableRuleAsync(
             "REGION_NORMAL",
             It.IsAny<DateTime>()))
    .ReturnsAsync(
        CreateRule(
            "REGION_NORMAL",
            1.00m));
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
    public async Task CalculateAsync_ShouldApplyLowRegionFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        _pricingRuleRepositoryMock
    .Setup(x => x.GetApplicableRuleAsync(
        "REGION_LOW",
        It.IsAny<DateTime>()))
            .ReturnsAsync(
                CreateRule(
                    "REGION_LOW",
                    0.95m));

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    DriverAge = 25,
                    Usage = "PRIVATE",
                    ClaimsCount = 1,
                    Region = "LOW",
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            0.95m,
            result.RegionFactor);

        Assert.Equal(
            19_000m,
            result.RiskAdjustedPremium);
    }
    [Fact]
    public async Task CalculateAsync_ShouldApplyNormalRegionFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    DriverAge = 25,
                    Usage = "PRIVATE",
                    ClaimsCount = 1,
                    Region = "NORMAL",
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            1.00m,
            result.RegionFactor);

        Assert.Equal(
            20_000m,
            result.RiskAdjustedPremium);
    }
    [Fact]
    public async Task CalculateAsync_ShouldApplyHighRegionFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

       _pricingRuleRepositoryMock
    .Setup(x => x.GetApplicableRuleAsync(
        "REGION_HIGH",
        It.IsAny<DateTime>()))
            .ReturnsAsync(
                CreateRule(
                    "REGION_HIGH",
                    1.10m));

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    DriverAge = 25,
                    Usage = "PRIVATE",
                    ClaimsCount = 1,
                    Region = "HIGH",
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            1.10m,
            result.RegionFactor);

        Assert.Equal(
            22_000m,
            result.RiskAdjustedPremium);
    }
    [Fact]
    public async Task CalculateAsync_ShouldApplyEconomicPackageFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        var packageId =
            Guid.NewGuid();

        _unitOfWorkMock
            .Setup(x => x.InsurancePackages.GetByIdAsync(packageId))
            .ReturnsAsync(
                new InsurancePackage
                {
                    Id = packageId,
                    Code = "EKONOMIK",
                    Name = "Ekonomik Paket",
                    Factor = 1.00m,
                    IsActive = true,
                    IsDeleted = false
                });

        var result =
            await _service.CalculateAsync(
               new PricingRequest
               {
                   MarketValue = 1_000_000m,
                   ModelYear = DateTime.UtcNow.Year,
                   DriverAge = 25,
                   Usage = "PRIVATE",
                   ClaimsCount = 1,
                   Region = "NORMAL",
                   PackageId = packageId,
                   CoverageIds = Array.Empty<Guid>()
               });

        Assert.Equal(
            1.00m,
            result.PackageFactor);

        Assert.Equal(
            20_000m,
            result.RiskAdjustedPremium);
    }

    [Fact]
    public async Task CalculateAsync_ShouldApplyStandardPackageFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        var packageId =
            Guid.NewGuid();

        _unitOfWorkMock
            .Setup(x => x.InsurancePackages.GetByIdAsync(packageId))
            .ReturnsAsync(
                new InsurancePackage
                {
                    Id = packageId,
                    Code = "STANDART",
                    Name = "Standart Paket",
                    Factor = 1.10m,
                    IsActive = true,
                    IsDeleted = false
                });

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    DriverAge = 25,
                    Usage = "PRIVATE",
                    ClaimsCount = 1,
                    Region = "NORMAL",
                    PackageId = packageId,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            1.10m,
            result.PackageFactor);

        Assert.Equal(
            22_000m,
            result.RiskAdjustedPremium);
    }
    [Fact]
    public async Task CalculateAsync_ShouldApplyComprehensivePackageFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        var packageId =
            Guid.NewGuid();

        _unitOfWorkMock
            .Setup(x => x.InsurancePackages.GetByIdAsync(packageId))
            .ReturnsAsync(
                new InsurancePackage
                {
                    Id = packageId,
                    Code = "KAPSAMLI",
                    Name = "Kapsamlı Paket",
                    Factor = 1.20m,
                    IsActive = true,
                    IsDeleted = false
                });

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    DriverAge = 25,
                    Usage = "PRIVATE",
                    ClaimsCount = 1,
                    Region = "NORMAL",
                    PackageId = packageId,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            1.20m,
            result.PackageFactor);

        Assert.Equal(
            24_000m,
            result.RiskAdjustedPremium);
    }
    [Fact]
    public async Task CalculateAsync_ShouldApplyDeductibleFactor()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        var result =
            await _service.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    DriverAge = 25,
                    Usage = "PRIVATE",
                    ClaimsCount = 1,
                    Region = "NORMAL",
                    Deductible = 10_000m,
                    CoverageIds = Array.Empty<Guid>()
                });

        Assert.Equal(
            1.00m,
            result.DeductibleFactor);

        Assert.Equal(
            20_000m,
            result.RiskAdjustedPremium);

        Assert.Equal(
            20_000m,
            result.TotalPremium);
    }
    [Fact]
    public async Task CalculateAsync_ShouldThrow_WhenDeductibleIsNegative()
    {
        SetupRules(
            baseRate: 0.02m,
            ageFactorCode: "AGE_0_2",
            ageFactor: 1.00m);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () =>
                    _service.CalculateAsync(
                        new PricingRequest
                        {
                            MarketValue = 1_000_000m,
                            ModelYear = DateTime.UtcNow.Year,
                            DriverAge = 25,
                            Usage = "PRIVATE",
                            ClaimsCount = 1,
                            Region = "NORMAL",
                            Deductible = -1m,
                            CoverageIds = Array.Empty<Guid>()
                        }));

        Assert.Contains(
            "Muafiyet tutarı negatif olamaz.",
            exception.Message);
    }
}