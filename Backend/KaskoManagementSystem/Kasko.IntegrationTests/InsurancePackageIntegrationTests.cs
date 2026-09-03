using Kasko.Business.DTOs.Package;
using Kasko.DataAccess;
using Kasko.Entities.Concrete;
using Kasko.Entities.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Microsoft.EntityFrameworkCore;

namespace Kasko.IntegrationTests;

public class InsurancePackageIntegrationTests
{
    [Fact]
    public async Task Manager_GetInsurancePackages_Should_ReturnPackagesWithCoverages()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var manager =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Manager");

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                manager);

        var response =
            await client.GetAsync(
                "/api/InsurancePackage");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var packages =
            await response.Content
                .ReadFromJsonAsync<
                    IEnumerable<InsurancePackageDto>>();

        Assert.NotNull(packages);

        var packageList =
            packages!.ToList();

        Assert.Contains(
            packageList,
            x => x.Code == "EKONOMIK");

        Assert.Contains(
            packageList,
            x => x.Code == "STANDART");

        Assert.Contains(
            packageList,
            x => x.Code == "KAPSAMLI");

        Assert.All(
            packageList,
            package =>
            {
                Assert.NotEqual(
                    Guid.Empty,
                    package.Id);

                Assert.False(
                    string.IsNullOrWhiteSpace(
                        package.Code));

                Assert.True(
                    package.IsActive);

                Assert.NotNull(
                    package.Coverages);

                Assert.NotNull(
                    package.Coverages);
            });

        var ekonomik =
            packageList.Single(
                x => x.Code == "EKONOMIK");

        Assert.NotEmpty(
            ekonomik.Coverages);

        Assert.All(
            ekonomik.Coverages,
            coverage =>
            {
                Assert.NotEqual(
                    Guid.Empty,
                    coverage.CoverageId);

                Assert.False(
                    string.IsNullOrWhiteSpace(
                        coverage.CoverageName));

                Assert.True(
                    coverage.CalculatedPrice >= 0);

            });
    }
    [Fact]
    public async Task Customer_GetInsurancePackages_Should_ReturnSuccess()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var customer =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Customer");

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                customer);

        var response =
            await client.GetAsync(
                "/api/InsurancePackage");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var packages =
            await response.Content
                .ReadFromJsonAsync<
                    IEnumerable<InsurancePackageDto>>();

        Assert.NotNull(packages);

        Assert.NotEmpty(
            packages!);

        Assert.Contains(
            packages!,
            x => x.Code == "EKONOMIK");

        Assert.Contains(
            packages!,
            x => x.Code == "STANDART");

        Assert.Contains(
            packages!,
            x => x.Code == "KAPSAMLI");
    }
    [Fact]
    public async Task Manager_GetInsurancePackages_Should_ExcludeInactiveCoverage()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var manager =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Manager");

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                manager);

        await using var setupScope =
            factory.Services.CreateAsyncScope();

        var setupContext =
            setupScope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var package =
            await setupContext.InsurancePackages
                .Where(x =>
                    x.Code == "EKONOMIK" &&
                    !x.IsDeleted)
                .FirstOrDefaultAsync();

        Assert.NotNull(package);

        var inactiveCoverage =
            new Coverage
            {
                Id = Guid.NewGuid(),
                Name = "Integration Inactive Coverage",
                Description = "Should not appear in package response",
                PricingType = CoveragePricingType.Fixed,
                BasePrice = 500m,
                Rate = null,
                DefaultLimit = 10000m,
                IsRequired = false,
                IsActive = false,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            };

        var packageCoverage =
            new PackageCoverage
            {
                Id = Guid.NewGuid(),
                InsurancePackageId = package!.Id,
                CoverageId = inactiveCoverage.Id,
                IsDefault = false,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            };

        setupContext.Coverages.Add(inactiveCoverage);
        setupContext.PackageCoverages.Add(packageCoverage);

        await setupContext.SaveChangesAsync();

        var response =
            await client.GetAsync(
                "/api/InsurancePackage");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var packages =
            await response.Content
                .ReadFromJsonAsync<
                    IEnumerable<InsurancePackageDto>>();

        Assert.NotNull(packages);

        var ekonomik =
            packages!
                .Single(x => x.Code == "EKONOMIK");

        Assert.DoesNotContain(
            ekonomik.Coverages,
            x => x.CoverageId == inactiveCoverage.Id);
    }
    [Fact]
    public async Task Manager_GetInsurancePackages_Should_ExcludeDeletedPackageCoverage()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var manager =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Manager");

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                manager);

        await using var setupScope =
            factory.Services.CreateAsyncScope();

        var setupContext =
            setupScope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var package =
            await setupContext.InsurancePackages
                .Where(x =>
                    x.Code == "EKONOMIK" &&
                    !x.IsDeleted)
                .FirstOrDefaultAsync();

        Assert.NotNull(package);

        var activeCoverage =
            new Coverage
            {
                Id = Guid.NewGuid(),
                Name = "Integration Deleted Relation Coverage",
                Description = "Coverage remains active",
                PricingType = CoveragePricingType.Fixed,
                BasePrice = 750m,
                Rate = null,
                DefaultLimit = 15000m,
                IsRequired = false,
                IsActive = true,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            };

        var deletedPackageCoverage =
            new PackageCoverage
            {
                Id = Guid.NewGuid(),
                InsurancePackageId = package!.Id,
                CoverageId = activeCoverage.Id,
                IsDefault = false,
                IsDeleted = true,
                CreatedDate = DateTime.UtcNow
            };

        setupContext.Coverages.Add(activeCoverage);
        setupContext.PackageCoverages.Add(deletedPackageCoverage);

        await setupContext.SaveChangesAsync();

        var response =
            await client.GetAsync(
                "/api/InsurancePackage");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var packages =
            await response.Content
                .ReadFromJsonAsync<
                    IEnumerable<InsurancePackageDto>>();

        Assert.NotNull(packages);

        var ekonomik =
            packages!
                .Single(x => x.Code == "EKONOMIK");

        Assert.DoesNotContain(
            ekonomik.Coverages,
            x => x.CoverageId == activeCoverage.Id);
    }
}