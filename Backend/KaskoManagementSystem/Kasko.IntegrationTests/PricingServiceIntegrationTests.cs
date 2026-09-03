using Kasko.Business.Pricing;
using Kasko.DataAccess;
using Kasko.Entities.Concrete;
using Kasko.Entities.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kasko.IntegrationTests;

public class PricingServiceIntegrationTests
{
    [Fact]
    public async Task PricingService_FixedCoverage_Should_CalculateCoveragePremium()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var coverageId =
            Guid.NewGuid();

        await using (var setupScope =
            factory.Services.CreateAsyncScope())
        {
            var context =
                setupScope.ServiceProvider
                    .GetRequiredService<KaskoContext>();

            var coverage =
                new Coverage
                {
                    Id = coverageId,
                    Name = $"Integration Fixed Coverage {Guid.NewGuid()}",
                    Description = "Created for pricing integration test",
                    PricingType = CoveragePricingType.Fixed,
                    BasePrice = 1000m,
                    Rate = null,
                    DefaultLimit = 50000m,
                    IsRequired = false,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedDate = DateTime.UtcNow
                };

            context.Coverages.Add(coverage);

            await context.SaveChangesAsync();
        }

        await using var serviceScope =
            factory.Services.CreateAsyncScope();

        var pricingService =
            serviceScope.ServiceProvider
                .GetRequiredService<IPricingService>();

        var currentYear =
            DateTime.UtcNow.Year;

        var result =
            await pricingService.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = currentYear,
                    DriverAge = 25,
                    Usage = "PRIVATE",
                    ClaimsCount = 1,
                    Region = "NORMAL",
                    PackageId = null,
                    Deductible = 0m,
                    CoverageIds =
                        new[]
                        {
                            coverageId
                        },
                    EffectiveDate = DateTime.UtcNow
                });

        Assert.NotNull(result);

        Assert.Equal(
            0.0215m,
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
            1.00m,
            result.RegionFactor);

        Assert.Equal(
            1.00m,
            result.PackageFactor);

        Assert.Equal(
            1.00m,
            result.DeductibleFactor);

        Assert.Equal(
            21_500m,
            result.BasePremium);

        Assert.Equal(
            21_500m,
            result.RiskAdjustedPremium);

        Assert.Single(
            result.Coverages);

        var coverageResult =
            result.Coverages.Single();

        Assert.Equal(
            coverageId,
            coverageResult.CoverageId);

        Assert.Equal(
            1000m,
            coverageResult.CalculatedPrice);

        Assert.Equal(
            1000m,
            result.CoveragePremium);

        Assert.Equal(
            22_500m,
            result.TotalPremium);
    }
    [Fact]
    public async Task PricingService_PercentageCoverage_Should_CalculateCoveragePremium()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var coverageId =
            Guid.NewGuid();

        await using (var setupScope =
            factory.Services.CreateAsyncScope())
        {
            var context =
                setupScope.ServiceProvider
                    .GetRequiredService<KaskoContext>();

            var coverage =
                new Coverage
                {
                    Id = coverageId,
                    Name = $"Integration Percentage Coverage {Guid.NewGuid()}",
                    Description = "Created for pricing integration test",
                    PricingType =
                        CoveragePricingType.PercentageOfVehicleValue,
                    BasePrice = 0m,
                    Rate = 0.5m,
                    DefaultLimit = 100000m,
                    IsRequired = false,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedDate = DateTime.UtcNow
                };

            context.Coverages.Add(coverage);

            await context.SaveChangesAsync();
        }

        await using var serviceScope =
            factory.Services.CreateAsyncScope();

        var pricingService =
            serviceScope.ServiceProvider
                .GetRequiredService<IPricingService>();

        var result =
            await pricingService.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    DriverAge = 25,
                    Usage = "PRIVATE",
                    ClaimsCount = 1,
                    Region = "NORMAL",
                    PackageId = null,
                    Deductible = 0m,
                    CoverageIds =
                        new[]
                        {
                        coverageId
                        },
                    EffectiveDate = DateTime.UtcNow
                });

        Assert.NotNull(result);

        Assert.Single(
            result.Coverages);

        var coverageResult =
            result.Coverages.Single();

        Assert.Equal(
            coverageId,
            coverageResult.CoverageId);

        Assert.Equal(
            5000m,
            coverageResult.CalculatedPrice);

        Assert.Equal(
            5000m,
            result.CoveragePremium);

        Assert.Equal(
            21_500m,
            result.BasePremium);

        Assert.Equal(
            26_500m,
            result.TotalPremium);
    }
    [Fact]
    public async Task PricingService_WithPackage_Should_ApplyPackageFactor()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var package =
            await context.InsurancePackages
                .AsNoTracking()
                .Where(x =>
                    x.Code == "STANDART" &&
                    !x.IsDeleted &&
                    x.IsActive)
                .FirstOrDefaultAsync();

        Assert.NotNull(package);

        var pricingService =
            scope.ServiceProvider
                .GetRequiredService<IPricingService>();

        var result =
            await pricingService.CalculateAsync(
                new PricingRequest
                {
                    MarketValue = 1_000_000m,
                    ModelYear = DateTime.UtcNow.Year,
                    DriverAge = 25,
                    Usage = "PRIVATE",
                    ClaimsCount = 1,
                    Region = "NORMAL",
                    PackageId = package!.Id,
                    Deductible = 0m,
                    CoverageIds = Array.Empty<Guid>(),
                    EffectiveDate = DateTime.UtcNow
                });

        Assert.NotNull(result);

        Assert.Equal(
            package.Factor,
            result.PackageFactor);

        var expectedBasePremium =
            1_000_000m
            * result.BaseRate
            * result.AgeFactor
            * result.UsageFactor
            * result.DriverFactor
            * result.ClaimsFactor
            * result.RegionFactor
            * package.Factor;

        Assert.Equal(
            expectedBasePremium,
            result.RiskAdjustedPremium);

        Assert.Equal(
            expectedBasePremium,
            result.TotalPremium);
    }
}