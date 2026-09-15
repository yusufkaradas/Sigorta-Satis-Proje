using Kasko.Business.DTOs.Package;
using Kasko.Business.DTOs.QuickQuote;
using Kasko.Business.DTOs.VehicleValue;
using Kasko.Business.Exceptions;
using Kasko.Business.Interfaces;
using Kasko.Business.Pricing;
using Kasko.Business.Services;
using Moq;

namespace Kasko.Business.Tests.Services;

public class QuickQuoteEstimateServiceTests
{
    private readonly Mock<IVehicleValueCatalogService> _catalogMock = new();
    private readonly Mock<IInsurancePackageService> _packageMock = new();
    private readonly Mock<IPricingService> _pricingMock = new();

    private readonly QuickQuoteEstimateService _service;

    private static readonly Guid EconomyId = Guid.NewGuid();
    private static readonly Guid ComprehensiveId = Guid.NewGuid();
    private static readonly Guid DefaultCoverageId = Guid.NewGuid();
    private static readonly Guid OptionalCoverageId = Guid.NewGuid();

    public QuickQuoteEstimateServiceTests()
    {
        _service = new QuickQuoteEstimateService(
            _catalogMock.Object,
            _packageMock.Object,
            _pricingMock.Object);

        _catalogMock
            .Setup(x => x.LookupAsync("123", "456", 2022, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VehicleValueLookupDto
            {
                BrandCode = "123",
                TypeCode = "456",
                BrandName = "FORD",
                TypeName = "FOCUS 1.5 TDCI",
                ModelYear = 2022,
                Value = 1_000_000m
            });

        _packageMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<InsurancePackageDto>
            {
                new()
                {
                    Id = ComprehensiveId,
                    Code = "FULL",
                    Name = "Kapsamlı",
                    Factor = 1.3m,
                    IsActive = true,
                    Coverages = new()
                    {
                        new() { CoverageId = DefaultCoverageId, IsDefault = true },
                        new() { CoverageId = OptionalCoverageId, IsDefault = false }
                    }
                },
                new()
                {
                    Id = EconomyId,
                    Code = "ECO",
                    Name = "Ekonomik",
                    Factor = 0.9m,
                    IsActive = true,
                    Coverages = new() { new() { CoverageId = DefaultCoverageId, IsDefault = true } }
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Code = "OLD",
                    Name = "Pasif Paket",
                    Factor = 1m,
                    IsActive = false
                }
            });

        _pricingMock
            .Setup(x => x.CalculateAsync(It.IsAny<PricingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PricingRequest request, CancellationToken _) => new PricingCalculation
            {
                TotalPremium = request.PackageId == EconomyId ? 10_000m : 15_000m,
                CoveragePremium = 1_000m,
                Coverages = new List<PricingCoverageResult>()
            });
    }

    private static QuickQuoteEstimateRequestDto ValidRequest() => new()
    {
        BrandCode = "123",
        TypeCode = "456",
        ModelYear = 2022,
        BirthYear = DateTime.UtcNow.Year - 35,
        Usage = "private",
        ClaimsCount = 1,
        Deductible = 5000m
    };

    [Fact]
    public async Task EstimateAsync_ShouldPriceEveryActivePackage_WithoutCustomer()
    {
        var result = await _service.EstimateAsync(ValidRequest());

        Assert.Equal(1_000_000m, result.MarketValue);
        Assert.Equal("FORD", result.BrandName);
        Assert.Equal(2, result.Packages.Count);
        Assert.Equal(new[] { "ECO", "FULL" }, result.Packages.Select(x => x.PackageCode));
        Assert.Equal(10_000m, result.Packages[0].TotalPremium);
    }

    [Fact]
    public async Task EstimateAsync_ShouldBuildPricingRequest_FromAnonymousInputs()
    {
        await _service.EstimateAsync(ValidRequest());

        _pricingMock.Verify(x => x.CalculateAsync(
            It.Is<PricingRequest>(r =>
                r.PackageId == ComprehensiveId &&
                r.MarketValue == 1_000_000m &&
                r.ModelYear == 2022 &&
                r.DriverAge == 35 &&
                r.Usage == "PRIVATE" &&
                r.ClaimsCount == 1 &&
                r.Deductible == 5000m &&
                r.CoverageIds.SequenceEqual(new[] { DefaultCoverageId })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EstimateAsync_ShouldPriceOnlyRequestedPackage()
    {
        var request = ValidRequest();
        request.PackageId = EconomyId;

        var result = await _service.EstimateAsync(request);

        Assert.Single(result.Packages);
        Assert.Equal(EconomyId, result.Packages[0].PackageId);
    }

    [Fact]
    public async Task EstimateAsync_ShouldThrowNotFound_WhenVehicleValueMissing()
    {
        var request = ValidRequest();
        request.TypeCode = "999";

        await Assert.ThrowsAsync<NotFoundException>(() => _service.EstimateAsync(request));

        _pricingMock.Verify(x => x.CalculateAsync(It.IsAny<PricingRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(120)]
    public async Task EstimateAsync_ShouldReject_InvalidDriverAge(int age)
    {
        var request = ValidRequest();
        request.BirthYear = DateTime.UtcNow.Year - age;

        await Assert.ThrowsAsync<BadRequestException>(() => _service.EstimateAsync(request));
    }

    [Fact]
    public async Task EstimateAsync_ShouldReject_UnknownUsage()
    {
        var request = ValidRequest();
        request.Usage = "RACING";

        await Assert.ThrowsAsync<BadRequestException>(() => _service.EstimateAsync(request));
    }

    [Fact]
    public async Task EstimateAsync_ShouldReject_MissingVehicleSelection()
    {
        var request = ValidRequest();
        request.BrandCode = " ";

        await Assert.ThrowsAsync<BadRequestException>(() => _service.EstimateAsync(request));
    }
}
