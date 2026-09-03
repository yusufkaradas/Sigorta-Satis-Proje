using Kasko.Business.DTOs.Policy;
using Kasko.Business.DTOs.Quote;
using Kasko.DataAccess;
using Kasko.Entities.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace Kasko.IntegrationTests;

public class QuoteIntegrationTests
{
    [Fact]
    public async Task Customer_CreateQuote_Should_CreateQuoteCoverageAndSnapshot()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var user =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                user);

        var customerId =
            await IntegrationTestHelper.CreateCustomerAsync(
                client);

        Assert.NotEqual(
            Guid.Empty,
            customerId);

        var vehicle =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                client,
                customerId);

        Assert.Equal(
            customerId,
            vehicle.CustomerId);

        await using var setupScope =
            factory.Services.CreateAsyncScope();

        var setupContext =
            setupScope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var package =
            await setupContext.InsurancePackages
                .AsNoTracking()
                .Where(x =>
                    x.Code == "STANDART" &&
                    x.IsActive &&
                    !x.IsDeleted)
                .FirstOrDefaultAsync();

        Assert.NotNull(package);

        var packageCoverage =
            await setupContext.PackageCoverages
                .AsNoTracking()
                .Where(x =>
                    x.InsurancePackageId == package!.Id &&
                    !x.IsDeleted)
                .FirstOrDefaultAsync();

        Assert.NotNull(packageCoverage);

        var validUntil =
            DateTime.UtcNow.AddDays(30);

        var request =
            new CreateQuoteDto
            {
                CustomerId = customerId,
                VehicleId = vehicle.Id,
                ValidUntil = validUntil,

                PackageId = package!.Id,

                CoverageIds =
                    new[]
                    {
                        packageCoverage!.CoverageId
                    },

                Usage = "PRIVATE",
                ClaimsCount = 1,
                Deductible = 0m,
                PreviousPolicyId = null
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/Quote",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var quote =
            await response.Content
                .ReadFromJsonAsync<QuoteDto>();

        Assert.NotNull(quote);

        Assert.NotEqual(
            Guid.Empty,
            quote!.Id);

        Assert.Equal(
            customerId,
            quote.CustomerId);

        Assert.Equal(
            vehicle.Id,
            quote.VehicleId);

        Assert.False(
            string.IsNullOrWhiteSpace(
                quote.QuoteNumber));

        Assert.True(
            quote.PremiumAmount > 0);

        Assert.Equal(
            validUntil,
            quote.ValidUntil);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationContext =
            verificationScope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var persistedQuote =
            await verificationContext.Quotes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == quote.Id &&
                        !x.IsDeleted);

        Assert.NotNull(
            persistedQuote);

        Assert.Equal(
            quote.PremiumAmount,
            persistedQuote!.PremiumAmount);

        var quoteCoverages =
            await verificationContext.QuoteCoverages
                .AsNoTracking()
                .Where(x =>
                    x.QuoteId == quote.Id &&
                    !x.IsDeleted)
                .ToListAsync();

        Assert.NotEmpty(
            quoteCoverages);

        Assert.Contains(
            quoteCoverages,
            x =>
                x.CoverageId ==
                packageCoverage.CoverageId);

        Assert.All(
            quoteCoverages,
            x =>
            {
                Assert.NotEqual(
                    Guid.Empty,
                    x.CoverageId);

                Assert.True(
                    x.CalculatedPrice >= 0);
            });

        var snapshot =
            await verificationContext.QuotePricingSnapshots
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.QuoteId == quote.Id &&
                        !x.IsDeleted);

        Assert.NotNull(
            snapshot);

        Assert.Equal(
            quote.Id,
            snapshot!.QuoteId);

        Assert.Equal(
            quote.PremiumAmount,
            snapshot.FinalPremium);

        Assert.True(
            snapshot.MarketValue > 0);

        Assert.True(
            snapshot.BaseRate > 0);

        Assert.True(
            snapshot.PackageFactor > 0);
    }
    [Fact]
    public async Task Customer_CalculateQuote_Should_ReturnCalculatedPremium()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var user =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                user);

        var customerId =
            await IntegrationTestHelper.CreateCustomerAsync(
                client);

        Assert.NotEqual(
            Guid.Empty,
            customerId);

        var vehicle =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                client,
                customerId);

        Assert.Equal(
            customerId,
            vehicle.CustomerId);

        await using var setupScope =
            factory.Services.CreateAsyncScope();

        var setupContext =
            setupScope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var package =
            await setupContext.InsurancePackages
                .AsNoTracking()
                .Where(x =>
                    x.Code == "STANDART" &&
                    x.IsActive &&
                    !x.IsDeleted)
                .FirstOrDefaultAsync();

        Assert.NotNull(package);

        var packageCoverage =
            await setupContext.PackageCoverages
                .AsNoTracking()
                .Where(x =>
                    x.InsurancePackageId == package!.Id &&
                    !x.IsDeleted)
                .FirstOrDefaultAsync();

        Assert.NotNull(packageCoverage);

        var request = new
        {
            CustomerId = customerId,
            VehicleId = vehicle.Id,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            PackageId = package!.Id,
            CoverageIds = new[]
     {
        packageCoverage!.CoverageId
    },
            Usage = "PRIVATE",
            ClaimsCount = 1,
            Deductible = 0m
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/Quote/calculate",
                request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var calculation =
            await response.Content
                .ReadFromJsonAsync<JsonElement>();

        Assert.True(
            calculation.ValueKind ==
            JsonValueKind.Object);

        Assert.True(
            calculation.TryGetProperty(
                "totalPremium",
                out var totalPremium));

        Assert.True(
            totalPremium.GetDecimal() > 0);
    }
    [Fact]
    public async Task Customer_GetQuoteById_Should_ReturnCreatedQuote()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var user =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                user);

        var customerId =
            await IntegrationTestHelper.CreateCustomerAsync(
                client);

        var vehicle =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                client,
                customerId);

        var createdQuote =
    await IntegrationTestHelper.CreateQuoteAsync(
        client,
        customerId,
        vehicle.Id);

        Assert.NotNull(createdQuote);

        Assert.NotNull(createdQuote);

        var response =
            await client.GetAsync(
                $"/api/Quote/{createdQuote!.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var quote =
            await response.Content
                .ReadFromJsonAsync<QuoteDto>();

        Assert.NotNull(quote);

        Assert.Equal(
            createdQuote.Id,
            quote!.Id);

        Assert.Equal(
            createdQuote.CustomerId,
            quote.CustomerId);

        Assert.Equal(
            createdQuote.VehicleId,
            quote.VehicleId);

        Assert.Equal(
            createdQuote.QuoteNumber,
            quote.QuoteNumber);

        Assert.Equal(
            createdQuote.PremiumAmount,
            quote.PremiumAmount);

        Assert.Equal(
            createdQuote.Status,
            quote.Status);

        Assert.Equal(
            createdQuote.ValidUntil,
            quote.ValidUntil);
    }
    [Fact]
    public async Task DeleteQuote_Should_SoftDeleteQuote()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var user =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                user);

        var customerId =
            await IntegrationTestHelper.CreateCustomerAsync(
                client);

        var vehicle =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                client,
                customerId);

        var quote =
            await IntegrationTestHelper.CreateQuoteAsync(
                client,
                customerId,
                vehicle.Id);

        Assert.NotEqual(
            Guid.Empty,
            quote.Id);

        var deleteResponse =
            await client.DeleteAsync(
                $"/api/Quote/{quote.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationContext =
            verificationScope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var deletedQuote =
            await verificationContext.Quotes
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == quote.Id);

        Assert.NotNull(deletedQuote);

        Assert.True(
            deletedQuote!.IsDeleted);

        var getResponse =
            await client.GetAsync(
                $"/api/Quote/{quote.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }
    [Fact]
    public async Task UpdateQuoteStatus_ToOffered_Should_UpdateStatus()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var user =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                user);

        var customerId =
            await IntegrationTestHelper.CreateCustomerAsync(
                client);

        var vehicle =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                client,
                customerId);

        var quote =
            await IntegrationTestHelper.CreateQuoteAsync(
                client,
                customerId,
                vehicle.Id);

        Assert.Equal(
               QuoteStatus.Draft,
               quote.Status);

        var response =
            await client.PatchAsync(
                $"/api/Quote/{quote.Id}/status?status=Offered",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationContext =
            verificationScope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var updatedQuote =
            await verificationContext.Quotes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == quote.Id &&
                        !x.IsDeleted);

        Assert.NotNull(updatedQuote);

        Assert.Equal(
     QuoteStatus.Offered,
     updatedQuote!.Status);
    }
    [Fact]
    public async Task UpdateQuoteStatus_ToAccepted_Should_UpdateStatus()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var user =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                user);

        var customerId =
            await IntegrationTestHelper.CreateCustomerAsync(
                client);

        var vehicle =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                client,
                customerId);

        var quote =
            await IntegrationTestHelper.CreateQuoteAsync(
                client,
                customerId,
                vehicle.Id);

        Assert.NotEqual(
            Guid.Empty,
            quote.Id);

        var offeredResponse =
            await client.PatchAsync(
                $"/api/Quote/{quote.Id}/status?status={QuoteStatus.Offered}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            offeredResponse.StatusCode);

        var acceptedResponse =
            await client.PatchAsync(
                $"/api/Quote/{quote.Id}/status?status={QuoteStatus.Accepted}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            acceptedResponse.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationContext =
            verificationScope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var updatedQuote =
            await verificationContext.Quotes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == quote.Id &&
                        !x.IsDeleted);

        Assert.NotNull(updatedQuote);

        Assert.Equal(
            QuoteStatus.Accepted,
            updatedQuote!.Status);
    }
    [Fact]
    public async Task CreatePolicy_FromAcceptedQuote_Should_CreateDraftPolicy()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var user =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                user);

        var customerId =
            await IntegrationTestHelper.CreateCustomerAsync(
                client);

        var vehicle =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                client,
                customerId);

        var quote =
            await IntegrationTestHelper.CreateQuoteAsync(
                client,
                customerId,
                vehicle.Id);

        Assert.Equal(
            QuoteStatus.Draft,
            quote.Status);

        var offeredResponse =
            await client.PatchAsync(
                $"/api/Quote/{quote.Id}/status?status={QuoteStatus.Offered}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            offeredResponse.StatusCode);

        var acceptedResponse =
            await client.PatchAsync(
                $"/api/Quote/{quote.Id}/status?status={QuoteStatus.Accepted}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            acceptedResponse.StatusCode);

        var policyResponse =
            await client.PostAsJsonAsync(
                "/api/Policy",
                new
                {
                    CustomerId = customerId,
                    VehicleId = vehicle.Id,
                    QuoteId = quote.Id,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddYears(1)
                });

        Assert.Equal(
            HttpStatusCode.Created,
            policyResponse.StatusCode);

        var policy =
            await policyResponse.Content
                .ReadFromJsonAsync<PolicyDto>();

        Assert.NotNull(policy);

        Assert.NotEqual(
            Guid.Empty,
            policy!.Id);

        Assert.Equal(
            customerId,
            policy.CustomerId);

        Assert.Equal(
            vehicle.Id,
            policy.VehicleId);

        Assert.Equal(
            quote.Id,
            policy.QuoteId);

        Assert.False(
            string.IsNullOrWhiteSpace(
                policy.PolicyNumber));

        Assert.Equal(
            quote.PremiumAmount,
            policy.PremiumAmount);

        Assert.Equal(
            PolicyStatus.Draft,
            policy.Status);

        Assert.Equal(
            false,
            policy.IsActive);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationContext =
            verificationScope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var persistedPolicy =
            await verificationContext.Policies
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == policy.Id &&
                        !x.IsDeleted);

        Assert.NotNull(persistedPolicy);

        Assert.Equal(
            quote.Id,
            persistedPolicy!.QuoteId);

        Assert.Equal(
            quote.PremiumAmount,
            persistedPolicy.PremiumAmount);

        Assert.Equal(
            PolicyStatus.Draft,
            persistedPolicy.Status);
    }
    [Fact]
    public async Task UpdateQuoteStatus_FromDraftToAccepted_Should_ReturnBadRequest()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var user =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                user);

        var customerId =
            await IntegrationTestHelper.CreateCustomerAsync(
                client);

        var vehicle =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                client,
                customerId);

        var quote =
            await IntegrationTestHelper.CreateQuoteAsync(
                client,
                customerId,
                vehicle.Id);

        Assert.Equal(
            QuoteStatus.Draft,
            quote.Status);

        var response =
            await client.PatchAsync(
                $"/api/Quote/{quote.Id}/status?status={QuoteStatus.Accepted}",
                null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationContext =
            verificationScope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var currentQuote =
            await verificationContext.Quotes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == quote.Id &&
                        !x.IsDeleted);

        Assert.NotNull(currentQuote);

        Assert.Equal(
            QuoteStatus.Draft,
            currentQuote!.Status);
    }
    [Fact]
    public async Task UpdateQuoteStatus_FromDraftToAccepted_Should_ReturnBadRequest()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var user =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                user);

        var customerId =
            await IntegrationTestHelper.CreateCustomerAsync(
                client);

        var vehicle =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                client,
                customerId);

        var quote =
            await IntegrationTestHelper.CreateQuoteAsync(
                client,
                customerId,
                vehicle.Id);

        Assert.Equal(
            QuoteStatus.Draft,
            quote.Status);

        var response =
            await client.PatchAsync(
                $"/api/Quote/{quote.Id}/status?status={QuoteStatus.Accepted}",
                null);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationContext =
            verificationScope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var currentQuote =
            await verificationContext.Quotes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == quote.Id &&
                        !x.IsDeleted);

        Assert.NotNull(currentQuote);

        Assert.Equal(
            QuoteStatus.Draft,
            currentQuote!.Status);
    }
}