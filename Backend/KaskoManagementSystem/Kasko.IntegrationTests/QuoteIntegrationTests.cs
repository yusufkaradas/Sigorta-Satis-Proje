using Kasko.Business.DTOs.Policy;
using Kasko.Business.DTOs.Quote;
using Kasko.DataAccess;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
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

        Assert.False(
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
    public async Task CreateQuote_WithPreviousPolicy_Should_UsePreviousPolicyClaimsCount()
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

        var previousPolicyResponse =
            await client.PostAsJsonAsync(
                "/api/PreviousPolicy",
                new
                {
                    CustomerId = customerId,
                    PreviousInsurer = "Test Sigorta",
                    PolicyNumber = $"TEST-{Guid.NewGuid():N}",
                    StartDate = DateTime.UtcNow.AddYears(-1),
                    EndDate = DateTime.UtcNow.AddDays(-1),
                    ClaimsCount = 3
                });

        Assert.Equal(
            HttpStatusCode.OK,
            previousPolicyResponse.StatusCode);

        var previousPolicy =
            await previousPolicyResponse.Content
                .ReadFromJsonAsync<JsonElement>();

        Assert.True(
            previousPolicy.ValueKind ==
            JsonValueKind.Object);

        Assert.True(
            previousPolicy.TryGetProperty(
                "id",
                out var previousPolicyId));

        var quoteRequest =
            new
            {
                CustomerId = customerId,
                VehicleId = vehicle.Id,
                ValidUntil = DateTime.UtcNow.AddDays(30),
                Usage = "PRIVATE",

                // Bilerek 0 gönderiyoruz.
                // QuoteService PreviousPolicy.ClaimsCount = 3 kullanmalı.
                ClaimsCount = 0,

                Deductible = 0m,
                PreviousPolicyId = previousPolicyId.GetGuid()
            };

        var quoteResponse =
            await client.PostAsJsonAsync(
                "/api/Quote",
                quoteRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            quoteResponse.StatusCode);

        var quote =
            await quoteResponse.Content
                .ReadFromJsonAsync<QuoteDto>();

        Assert.NotNull(quote);

        var snapshot =
            await IntegrationTestHelper
                .GetQuotePricingSnapshotAsync(
                    factory,
                    quote!.Id);

        Assert.NotNull(snapshot);

        // CLAIMS_3_PLUS seed değeri 1.30 olmalı.
        Assert.Equal(
            1.30m,
            snapshot!.ClaimsFactor);
    }
    [Fact]
    public async Task CreateQuote_WithPreviousPolicyOfAnotherCustomer_Should_ReturnBadRequest()
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

        var customer1Id =
            await IntegrationTestHelper.CreateCustomerAsync(
                client);

        var customer2Id =
            await IntegrationTestHelper.CreateCustomerAsync(
                client);

        var vehicle =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                client,
                customer2Id);

        var previousPolicyResponse =
            await client.PostAsJsonAsync(
                "/api/PreviousPolicy",
                new
                {
                    CustomerId = customer1Id,
                    PreviousInsurer = "Test Sigorta",
                    PolicyNumber = $"TEST-{Guid.NewGuid():N}",
                    StartDate = DateTime.UtcNow.AddYears(-1),
                    EndDate = DateTime.UtcNow.AddDays(-1),
                    ClaimsCount = 2
                });

        Assert.Equal(
            HttpStatusCode.OK,
            previousPolicyResponse.StatusCode);

        var previousPolicy =
            await previousPolicyResponse.Content
                .ReadFromJsonAsync<JsonElement>();

        Assert.True(
            previousPolicy.TryGetProperty(
                "id",
                out var previousPolicyId));

        var quoteRequest =
            new
            {
                CustomerId = customer2Id,
                VehicleId = vehicle.Id,
                ValidUntil = DateTime.UtcNow.AddDays(30),
                Usage = "PRIVATE",
                ClaimsCount = 0,
                Deductible = 0m,
                PreviousPolicyId = previousPolicyId.GetGuid()
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/Quote",
                quoteRequest);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
    [Fact]
    public async Task CreateQuote_WithCoverageOutsidePackage_Should_ReturnBadRequest()
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

        var coverageResponse =
            await client.PostAsJsonAsync(
                "/api/Coverage",
                new
                {
                    Name = $"Integration Test Coverage {Guid.NewGuid():N}",
                    Description = "Package dışı coverage test verisi",
                    PricingType = 1,
                    BasePrice = 500m,
                    Rate = 0m,
                    DefaultLimit = 10000m,
                    IsRequired = false
                });

        Assert.Equal(
            HttpStatusCode.Created,
            coverageResponse.StatusCode);

        var createdCoverage =
            await coverageResponse.Content
                .ReadFromJsonAsync<JsonElement>();

        Assert.True(
            createdCoverage.TryGetProperty(
                "id",
                out var coverageId));

        var quoteRequest =
            new
            {
                CustomerId = customerId,
                VehicleId = vehicle.Id,
                ValidUntil = DateTime.UtcNow.AddDays(30),
                PackageId = package!.Id,

                CoverageIds = new[]
                {
                coverageId.GetGuid()
                },

                Usage = "PRIVATE",
                ClaimsCount = 0,
                Deductible = 0m,
                PreviousPolicyId = (Guid?)null
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/Quote",
                quoteRequest);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
    [Fact]
    public async Task CreateQuote_WithPastValidUntil_Should_ReturnBadRequest()
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

        var request =
            new
            {
                CustomerId = customerId,
                VehicleId = vehicle.Id,
                ValidUntil = DateTime.UtcNow.AddDays(-1),
                Usage = "PRIVATE",
                ClaimsCount = 0,
                Deductible = 0m,
                PreviousPolicyId = (Guid?)null
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/Quote",
                request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
    [Fact]
    public async Task UpdateQuoteStatus_ToRejected_Should_UpdateStatus()
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

        var rejectedResponse =
            await client.PatchAsync(
                $"/api/Quote/{quote.Id}/status?status={QuoteStatus.Rejected}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            rejectedResponse.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationContext =
            verificationScope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var rejectedQuote =
            await verificationContext.Quotes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == quote.Id &&
                        !x.IsDeleted);

        Assert.NotNull(rejectedQuote);

        Assert.Equal(
            QuoteStatus.Rejected,
            rejectedQuote!.Status);
    }
    [Fact]
    public async Task UpdateQuoteStatus_ToCancelled_Should_UpdateStatus()
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

        var cancelledResponse =
            await client.PatchAsync(
                $"/api/Quote/{quote.Id}/status?status={QuoteStatus.Cancelled}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            cancelledResponse.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationContext =
            verificationScope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var cancelledQuote =
            await verificationContext.Quotes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == quote.Id &&
                        !x.IsDeleted);

        Assert.NotNull(cancelledQuote);

        Assert.Equal(
            QuoteStatus.Cancelled,
            cancelledQuote!.Status);
    }
    [Fact]
    public async Task UpdateQuoteStatus_ToExpired_Should_UpdateStatus()
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
        await using var setupScope =
    factory.Services.CreateAsyncScope();

        var setupContext =
            setupScope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var quoteEntity =
            await setupContext.Quotes
                .FirstOrDefaultAsync(
                    x => x.Id == quote.Id);

        Assert.NotNull(quoteEntity);

        quoteEntity!.ValidUntil =
            DateTime.UtcNow.AddMinutes(-1);

        await setupContext.SaveChangesAsync();
        Assert.Equal(
            HttpStatusCode.NoContent,
            offeredResponse.StatusCode);

        var expiredResponse =
            await client.PatchAsync(
                $"/api/Quote/{quote.Id}/status?status={QuoteStatus.Expired}",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            expiredResponse.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationContext =
            verificationScope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var expiredQuote =
            await verificationContext.Quotes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == quote.Id &&
                        !x.IsDeleted);

        Assert.NotNull(expiredQuote);

        Assert.Equal(
            QuoteStatus.Expired,
            expiredQuote!.Status);
    }
    [Fact]
    public async Task UpdateQuoteStatus_FromAcceptedToOffered_Should_ReturnBadRequest()
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

        var response =
            await client.PatchAsync(
                $"/api/Quote/{quote.Id}/status?status={QuoteStatus.Offered}",
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
            QuoteStatus.Accepted,
            currentQuote!.Status);
    }
    [Fact]
    public async Task OldQuote_Should_Keep_OldPricingSnapshot_When_NewPricingRuleVersion_IsAdded()
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

        // 1. Mevcut aktif pricing rule'u bul
        await using var setupScope =
            factory.Services.CreateAsyncScope();

        var pricingRuleRepository =
            setupScope.ServiceProvider
                .GetRequiredService<IPricingRuleRepository>();

        var currentRule =
            await pricingRuleRepository
                .GetApplicableRuleAsync(
                    "BASE_KASKO_RATE",
                    DateTime.UtcNow);

        Assert.NotNull(currentRule);

        var oldBaseRate =
            currentRule!.Value;

        var oldVersion =
            currentRule.Version;

        // 2. İlk Quote
        var oldQuote =
            await IntegrationTestHelper.CreateQuoteAsync(
                client,
                customerId,
                vehicle.Id);

        var oldSnapshot =
            await IntegrationTestHelper
                .GetQuotePricingSnapshotAsync(
                    factory,
                    oldQuote.Id);

        Assert.NotNull(oldSnapshot);

        Assert.Equal(
            oldBaseRate,
            oldSnapshot!.BaseRate);
        var maxVersion =
        await setupScope.ServiceProvider
        .GetRequiredService<KaskoContext>()
        .PricingRules
        .Where(x => x.Code == "BASE_KASKO_RATE")
        .MaxAsync(x => x.Version);

        var newVersion = maxVersion + 1;
        // 3. Yeni pricing rule version
        var newBaseRate =
            oldBaseRate + 0.0100m;

        var newRule =
            new PricingRule
            {
                Id = Guid.NewGuid(),

                Code = "BASE_KASKO_RATE",
                Name = "Integration Test New Base Rate",
                Description = "Snapshot regression test",

                Value = newBaseRate,

                IsActive = true,

                Version = newVersion,

                EffectiveFrom =
                    DateTime.UtcNow.AddSeconds(-1),

                EffectiveUntil = null,

                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        await pricingRuleRepository.AddAsync(
            newRule);

        await setupScope.ServiceProvider
            .GetRequiredService<IUnitOfWork>()
            .SaveChangesAsync();

        try
        {
            // 4. Yeni Quote
            var newQuote =
                await IntegrationTestHelper.CreateQuoteAsync(
                    client,
                    customerId,
                    vehicle.Id);

            var newSnapshot =
                await IntegrationTestHelper
                    .GetQuotePricingSnapshotAsync(
                        factory,
                        newQuote.Id);

            Assert.NotNull(newSnapshot);

            Assert.Equal(
                newBaseRate,
                newSnapshot!.BaseRate);

            // 6. Eski Quote eski snapshot'ını korumalı
            var oldSnapshotAfterNewRule =
                await IntegrationTestHelper
                    .GetQuotePricingSnapshotAsync(
                        factory,
                        oldQuote.Id);

            Assert.NotNull(oldSnapshotAfterNewRule);

            Assert.Equal(
                oldBaseRate,
                oldSnapshotAfterNewRule!.BaseRate);

            Assert.NotEqual(
                newSnapshot.BaseRate,
                oldSnapshotAfterNewRule.BaseRate);
        }
        finally
        {
            await pricingRuleRepository.DeleteAsync(newRule);

            await setupScope.ServiceProvider
                .GetRequiredService<IUnitOfWork>()
                .SaveChangesAsync();
        }
    }
    [Fact]
    public async Task Customer_UpdateQuote_Should_UpdateValidUntil()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var adminUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var adminClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                adminUser);

        var customerId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        var customerUser =
            await IntegrationTestHelper.SeedUserAsync(
            factory,
            "Customer",
            customerId);

        using var client =
            await IntegrationTestHelper.LoginAsync(
                factory,
                customerUser);

        var vehicle =
    await IntegrationTestHelper.CreateVehicleAsync(
        factory,
        adminClient,
        customerId);

        var quote =
            await IntegrationTestHelper.CreateQuoteAsync(
                client,
                customerId,
                vehicle.Id);

        var newValidUntil =
            DateTime.UtcNow.AddDays(30);

        var response =
            await client.PutAsJsonAsync(
                $"/api/Quote/{quote.Id}",
                new
                {
                    ValidUntil = newValidUntil
                });

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var updatedQuote =
            await client.GetFromJsonAsync<
                Kasko.Business.DTOs.Quote.QuoteDto>(
                $"/api/Quote/{quote.Id}");

        Assert.NotNull(updatedQuote);

        Assert.Equal(
            newValidUntil,
            updatedQuote!.ValidUntil);
    }
}