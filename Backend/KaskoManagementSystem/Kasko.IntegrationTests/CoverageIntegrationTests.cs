using Kasko.Business.DTOs.Coverage;
using Kasko.Business.DTOs.Package;
using Kasko.DataAccess;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Kasko.IntegrationTests;

public class CoverageIntegrationTests
{
    [Fact]
    public async Task Manager_GetCoverages_Should_ReturnSuccess()
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
                "/api/Coverage");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var coverages =
            await response.Content
                .ReadFromJsonAsync<
                    IEnumerable<CoverageDto>>();

        Assert.NotNull(coverages);

        var coverageList =
            coverages!.ToList();

        Assert.NotEmpty(
            coverageList);

        Assert.All(
            coverageList,
            coverage =>
            {
                Assert.NotEqual(
                    Guid.Empty,
                    coverage.Id);

                Assert.False(
                    string.IsNullOrWhiteSpace(
                        coverage.Name));

                Assert.True(
                    coverage.BasePrice >= 0);

                Assert.True(
                    coverage.DefaultLimit == null ||
                    coverage.DefaultLimit >= 0);

                Assert.True(
                    coverage.Rate == null ||
                    coverage.Rate >= 0);
            });
    }
    [Fact]
    public async Task Admin_CreateCoverage_Should_CreateCoverage()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var admin =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                admin);

        var request =
            new CreateCoverageDto
            {
                Name = $"Integration Test Coverage {Guid.NewGuid()}",
                Description = "Created by integration test",
                PricingType = CoveragePricingType.Fixed,
                BasePrice = 1250m,
                Rate = null,
                DefaultLimit = 50000m,
                IsRequired = false
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/Coverage",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<CoverageDto>();

        Assert.NotNull(result);

        Assert.NotEqual(
            Guid.Empty,
            result!.Id);

        Assert.Equal(
            request.Name,
            result.Name);

        Assert.Equal(
            request.Description,
            result.Description);

        Assert.Equal(
            request.PricingType,
            result.PricingType);

        Assert.Equal(
            request.BasePrice,
            result.BasePrice);

        Assert.Equal(
            request.Rate,
            result.Rate);

        Assert.Equal(
            request.DefaultLimit,
            result.DefaultLimit);

        Assert.Equal(
            request.IsRequired,
            result.IsRequired);

        Assert.True(
            result.IsActive);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationRepository =
            verificationScope.ServiceProvider
                .GetRequiredService<ICoverageRepository>();

        var persistedCoverage =
            await verificationRepository
                .GetByIdAsync(result.Id);

        Assert.NotNull(persistedCoverage);

        Assert.Equal(
            request.Name,
            persistedCoverage!.Name);

        Assert.Equal(
            request.BasePrice,
            persistedCoverage.BasePrice);

        Assert.Equal(
            request.PricingType,
            persistedCoverage.PricingType);

        Assert.True(
            persistedCoverage.IsActive);

        Assert.False(
            persistedCoverage.IsDeleted);
    }
    [Fact]
    public async Task Manager_CreateCoverage_Should_ReturnForbidden()
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

        var request =
            new CreateCoverageDto
            {
                Name = $"Manager Forbidden Coverage {Guid.NewGuid()}",
                Description = "Should not be created by Manager",
                PricingType = CoveragePricingType.Fixed,
                BasePrice = 1000m,
                Rate = null,
                DefaultLimit = 25000m,
                IsRequired = false
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/Coverage",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
    [Fact]
    public async Task Customer_CreateCoverage_Should_ReturnForbidden()
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

        var request =
            new CreateCoverageDto
            {
                Name = $"Customer Forbidden Coverage {Guid.NewGuid()}",
                Description = "Should not be created by Customer",
                PricingType = CoveragePricingType.Fixed,
                BasePrice = 1000m,
                Rate = null,
                DefaultLimit = 25000m,
                IsRequired = false
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/Coverage",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
    [Fact]
    public async Task Admin_UpdateCoverage_Should_UpdateCoverage()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var admin =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                admin);

        var createRequest =
            new CreateCoverageDto
            {
                Name = $"Integration Update Coverage {Guid.NewGuid()}",
                Description = "Created for update integration test",
                PricingType = CoveragePricingType.Fixed,
                BasePrice = 1500m,
                Rate = null,
                DefaultLimit = 40000m,
                IsRequired = false
            };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/Coverage",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var createdCoverage =
            await createResponse.Content
                .ReadFromJsonAsync<CoverageDto>();

        Assert.NotNull(createdCoverage);

        var updateRequest =
            new UpdateCoverageDto
            {
                Id = createdCoverage!.Id,
                Name = $"Updated Coverage {Guid.NewGuid()}",
                Description = "Updated by integration test",
                PricingType = CoveragePricingType.Fixed,
                BasePrice = 2750m,
                Rate = null,
                DefaultLimit = 75000m,
                IsRequired = true,
                IsActive = false
            };

        var updateResponse =
            await client.PutAsJsonAsync(
                "/api/Coverage",
                updateRequest);

        Assert.Equal(
            HttpStatusCode.NoContent,
            updateResponse.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationRepository =
            verificationScope.ServiceProvider
                .GetRequiredService<ICoverageRepository>();

        var updatedCoverage =
            await verificationRepository.GetByIdAsync(
                createdCoverage.Id);

        Assert.NotNull(updatedCoverage);

        Assert.Equal(
            updateRequest.Name,
            updatedCoverage!.Name);

        Assert.Equal(
            updateRequest.Description,
            updatedCoverage.Description);

        Assert.Equal(
            updateRequest.PricingType,
            updatedCoverage.PricingType);

        Assert.Equal(
            updateRequest.BasePrice,
            updatedCoverage.BasePrice);

        Assert.Equal(
            updateRequest.Rate,
            updatedCoverage.Rate);

        Assert.Equal(
            updateRequest.DefaultLimit,
            updatedCoverage.DefaultLimit);

        Assert.Equal(
            updateRequest.IsRequired,
            updatedCoverage.IsRequired);

        Assert.Equal(
            updateRequest.IsActive,
            updatedCoverage.IsActive);

        Assert.False(
            updatedCoverage.IsDeleted);
    }
    [Fact]
    public async Task Manager_UpdateCoverage_Should_ReturnForbidden()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var admin =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        var manager =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Manager");

        using var adminClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                admin);

        using var managerClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                manager);

        var createRequest =
            new CreateCoverageDto
            {
                Name = $"Manager Update Forbidden {Guid.NewGuid()}",
                Description = "Created for authorization test",
                PricingType = CoveragePricingType.Fixed,
                BasePrice = 1750m,
                Rate = null,
                DefaultLimit = 30000m,
                IsRequired = false
            };

        var createResponse =
            await adminClient.PostAsJsonAsync(
                "/api/Coverage",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var createdCoverage =
            await createResponse.Content
                .ReadFromJsonAsync<CoverageDto>();

        Assert.NotNull(createdCoverage);

        var updateRequest =
            new UpdateCoverageDto
            {
                Id = createdCoverage!.Id,
                Name = $"Manager Must Not Update {Guid.NewGuid()}",
                Description = "Should not be updated by Manager",
                PricingType = CoveragePricingType.Fixed,
                BasePrice = 9999m,
                Rate = null,
                DefaultLimit = 99999m,
                IsRequired = true,
                IsActive = false
            };

        var response =
            await managerClient.PutAsJsonAsync(
                "/api/Coverage",
                updateRequest);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
    [Fact]
    public async Task Customer_UpdateCoverage_Should_ReturnForbidden()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var admin =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        var customer =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Customer");

        using var adminClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                admin);

        using var customerClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                customer);

        var createRequest =
            new CreateCoverageDto
            {
                Name = $"Customer Update Forbidden {Guid.NewGuid()}",
                Description = "Created for authorization test",
                PricingType = CoveragePricingType.Fixed,
                BasePrice = 1800m,
                Rate = null,
                DefaultLimit = 35000m,
                IsRequired = false
            };

        var createResponse =
            await adminClient.PostAsJsonAsync(
                "/api/Coverage",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var createdCoverage =
            await createResponse.Content
                .ReadFromJsonAsync<CoverageDto>();

        Assert.NotNull(createdCoverage);

        var updateRequest =
            new UpdateCoverageDto
            {
                Id = createdCoverage!.Id,
                Name = $"Customer Must Not Update {Guid.NewGuid()}",
                Description = "Should not be updated by Customer",
                PricingType = CoveragePricingType.Fixed,
                BasePrice = 9999m,
                Rate = null,
                DefaultLimit = 99999m,
                IsRequired = true,
                IsActive = false
            };

        var response =
            await customerClient.PutAsJsonAsync(
                "/api/Coverage",
                updateRequest);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
    [Fact]
    public async Task Admin_DeleteCoverage_Should_SoftDeleteCoverage()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var admin =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                admin);

        var createRequest =
            new CreateCoverageDto
            {
                Name = $"Integration Delete Coverage {Guid.NewGuid()}",
                Description = "Created for delete integration test",
                PricingType = CoveragePricingType.Fixed,
                BasePrice = 2000m,
                Rate = null,
                DefaultLimit = 50000m,
                IsRequired = false
            };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/Coverage",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var createdCoverage =
            await createResponse.Content
                .ReadFromJsonAsync<CoverageDto>();

        Assert.NotNull(createdCoverage);

        var deleteResponse =
            await client.DeleteAsync(
                $"/api/Coverage/{createdCoverage!.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationContext =
            verificationScope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var rowCount =
            await verificationContext.Database
                .SqlQueryRaw<int>(
                    "SELECT COUNT(*) AS [Value] FROM Coverages WHERE Id = {0}",
                    createdCoverage.Id)
                .SingleAsync();

        Assert.Equal(
            1,
            rowCount);

        var isDeleted =
            await verificationContext.Database
                .SqlQueryRaw<bool>(
                    "SELECT IsDeleted AS [Value] FROM Coverages WHERE Id = {0}",
                    createdCoverage.Id)
                .SingleAsync();

        Assert.True(
            isDeleted);
    }
    [Fact]
    public async Task Manager_DeleteCoverage_Should_ReturnForbidden()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var admin =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        var manager =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Manager");

        using var adminClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                admin);

        using var managerClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                manager);

        var createRequest =
            new CreateCoverageDto
            {
                Name = $"Manager Delete Forbidden {Guid.NewGuid()}",
                Description = "Created for authorization integration test",
                PricingType = CoveragePricingType.Fixed,
                BasePrice = 2250m,
                Rate = null,
                DefaultLimit = 60000m,
                IsRequired = false
            };

        var createResponse =
            await adminClient.PostAsJsonAsync(
                "/api/Coverage",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var createdCoverage =
            await createResponse.Content
                .ReadFromJsonAsync<CoverageDto>();

        Assert.NotNull(createdCoverage);

        var deleteResponse =
            await managerClient.DeleteAsync(
                $"/api/Coverage/{createdCoverage!.Id}");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            deleteResponse.StatusCode);
    }
    [Fact]
    public async Task Customer_DeleteCoverage_Should_ReturnForbidden()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var admin =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        var customer =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Customer");

        using var adminClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                admin);

        using var customerClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                customer);

        var createRequest =
            new CreateCoverageDto
            {
                Name = $"Customer Delete Forbidden {Guid.NewGuid()}",
                Description = "Created for authorization integration test",
                PricingType = CoveragePricingType.Fixed,
                BasePrice = 2500m,
                Rate = null,
                DefaultLimit = 65000m,
                IsRequired = false
            };

        var createResponse =
            await adminClient.PostAsJsonAsync(
                "/api/Coverage",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var createdCoverage =
            await createResponse.Content
                .ReadFromJsonAsync<CoverageDto>();

        Assert.NotNull(createdCoverage);

        var deleteResponse =
            await customerClient.DeleteAsync(
                $"/api/Coverage/{createdCoverage!.Id}");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            deleteResponse.StatusCode);
    }
    [Fact]
    public async Task Manager_GetInsurancePackages_Should_MapDefaultCoverageCorrectly()
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

        var standard =
            packages!
                .Single(x => x.Code == "STANDART");

        Assert.NotEmpty(
            standard.Coverages);

        Assert.Contains(
            standard.Coverages,
            x => x.IsDefault);
    }
}