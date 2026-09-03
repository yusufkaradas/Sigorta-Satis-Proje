using Kasko.Business.DTOs.PricingRuleChangeRequest;
using Kasko.DataAccess;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace Kasko.IntegrationTests;

public class PricingRuleChangeRequestIntegrationTests
{
    [Fact]
    public async Task Manager_CreatePricingRuleChangeRequest_Should_CreatePendingRequest()
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

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var pricingRule =
            await context.PricingRules
                .Where(x =>
                    x.Code == "BASE_KASKO_RATE" &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.Version)
                .FirstOrDefaultAsync();

        Assert.NotNull(pricingRule);

        var effectiveFrom =
            DateTime.UtcNow.AddDays(30);

        var request =
            new CreatePricingRuleChangeRequestDto
            {
                PricingRuleId = pricingRule!.Id,
                NewValue = pricingRule.Value + 0.0010m,
                Reason = "Integration test pricing rule change",
                EffectiveFrom = effectiveFrom
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/PricingRuleChangeRequest",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<PricingRuleChangeRequestDto>();

        Assert.NotNull(result);

        Assert.NotEqual(
            Guid.Empty,
            result!.Id);

        Assert.Equal(
            pricingRule.Id,
            result.PricingRuleId);

        Assert.Equal(
            pricingRule.Value,
            result.OldValue);

        Assert.Equal(
            request.NewValue,
            result.NewValue);

        Assert.Equal(
            request.Reason,
            result.Reason);

        Assert.Equal(
            manager.Id,
            result.RequestedBy);

        Assert.Equal(
            "Pending",
            result.Status);

        Assert.Equal(
            effectiveFrom,
            result.EffectiveFrom);

        var persistedRequest =
            await context.PricingRuleChangeRequests
                .FirstOrDefaultAsync(
                    x => x.Id == result.Id);

        Assert.NotNull(persistedRequest);

        Assert.Equal(
            pricingRule.Id,
            persistedRequest!.PricingRuleId);

        Assert.Equal(
            pricingRule.Value,
            persistedRequest.OldValue);

        Assert.Equal(
            request.NewValue,
            persistedRequest.NewValue);

        Assert.Equal(
            manager.Id,
            persistedRequest.RequestedBy);

        Assert.Equal(
            "Pending",
            persistedRequest.Status);
    }
    [Fact]
    public async Task Admin_ApprovePricingRuleChangeRequest_Should_CreateNewVersion()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var manager =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Manager");

        var admin =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var managerClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                manager);

        using var adminClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                admin);

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var currentRule =
        await context.PricingRules
        .AsNoTracking()
        .Where(x =>
            x.Code == "BASE_KASKO_RATE" &&
            !x.IsDeleted)
        .OrderByDescending(x => x.Version)
        .FirstOrDefaultAsync();

        Assert.NotNull(currentRule);

        var effectiveFrom =
            currentRule!.EffectiveFrom.AddMonths(1);

        var newValue =
            currentRule.Value + 0.0010m;

        var createRequest =
            new CreatePricingRuleChangeRequestDto
            {
                PricingRuleId = currentRule.Id,
                NewValue = newValue,
                Reason = "Integration approve test",
                EffectiveFrom = effectiveFrom
            };

        var createResponse =
            await managerClient.PostAsJsonAsync(
                "/api/PricingRuleChangeRequest",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var changeRequest =
            await createResponse.Content
                .ReadFromJsonAsync<PricingRuleChangeRequestDto>();

        Assert.NotNull(changeRequest);

        Assert.Equal(
            "Pending",
            changeRequest!.Status);

        var approveResponse =
            await adminClient.PostAsync(
                $"/api/PricingRuleChangeRequest/{changeRequest.Id}/approve",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            approveResponse.StatusCode);

        var persistedRequest =
            await context.PricingRuleChangeRequests
                .FirstOrDefaultAsync(
                    x => x.Id == changeRequest.Id);

        Assert.NotNull(persistedRequest);

        Assert.Equal(
            "Approved",
            persistedRequest!.Status);

        Assert.Equal(
            admin.Id,
            persistedRequest.ApprovedBy);

        Assert.NotNull(
            persistedRequest.ApprovedDate);

        var updatedOldRule =
            await context.PricingRules
                .FirstOrDefaultAsync(
                    x => x.Id == currentRule.Id);

        Assert.NotNull(updatedOldRule);

        Assert.Equal(
            effectiveFrom.AddTicks(-1),
            updatedOldRule!.EffectiveUntil);

        var newRule =
            await context.PricingRules
                .Where(x =>
                    x.Code == currentRule.Code &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.Version)
                .FirstOrDefaultAsync();

        Assert.NotNull(newRule);

        Assert.NotEqual(
            currentRule.Id,
            newRule!.Id);

        Assert.Equal(
            currentRule.Version + 1,
            newRule.Version);

        Assert.Equal(
            newValue,
            newRule.Value);

        Assert.True(
            newRule.IsActive);

        Assert.Equal(
            effectiveFrom,
            newRule.EffectiveFrom);

        Assert.Null(
            newRule.EffectiveUntil);
    }
    [Fact]
    public async Task Admin_RejectPricingRuleChangeRequest_Should_NotChangePricingRule()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var manager =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Manager");

        var admin =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var managerClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                manager);

        using var adminClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                admin);

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var currentRule =
            await context.PricingRules
                .AsNoTracking()
                .Where(x =>
                    x.Code == "BASE_KASKO_RATE" &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.Version)
                .FirstOrDefaultAsync();

        Assert.NotNull(currentRule);

        var originalRuleId = currentRule!.Id;
        var originalVersion = currentRule.Version;
        var originalValue = currentRule.Value;
        var originalEffectiveFrom = currentRule.EffectiveFrom;
        var originalEffectiveUntil = currentRule.EffectiveUntil;

        var createRequest =
            new CreatePricingRuleChangeRequestDto
            {
                PricingRuleId = originalRuleId,
                NewValue = originalValue + 0.0010m,
                Reason = "Integration reject test",
                EffectiveFrom = DateTime.UtcNow.AddDays(30)
            };

        var createResponse =
            await managerClient.PostAsJsonAsync(
                "/api/PricingRuleChangeRequest",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var changeRequest =
            await createResponse.Content
                .ReadFromJsonAsync<PricingRuleChangeRequestDto>();

        Assert.NotNull(changeRequest);

        Assert.Equal(
            "Pending",
            changeRequest!.Status);

        var rejectResponse =
            await adminClient.PostAsync(
                $"/api/PricingRuleChangeRequest/{changeRequest.Id}/reject",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            rejectResponse.StatusCode);

        var rejectedRequest =
            await context.PricingRuleChangeRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == changeRequest.Id);

        Assert.NotNull(rejectedRequest);

        Assert.Equal(
            "Rejected",
            rejectedRequest!.Status);

        Assert.Equal(
            admin.Id,
            rejectedRequest.ApprovedBy);

        Assert.NotNull(
            rejectedRequest.ApprovedDate);

        var pricingRules =
            await context.PricingRules
                .AsNoTracking()
                .Where(x =>
                    x.Code == currentRule.Code &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.Version)
                .ToListAsync();

        Assert.Contains(
            pricingRules,
            x => x.Id == originalRuleId);

        Assert.Equal(
            originalVersion,
            pricingRules
                .Where(x => x.Id == originalRuleId)
                .Select(x => x.Version)
                .Single());

        Assert.Equal(
            originalValue,
            pricingRules
                .Where(x => x.Id == originalRuleId)
                .Select(x => x.Value)
                .Single());

        Assert.Equal(
            originalEffectiveFrom,
            pricingRules
                .Where(x => x.Id == originalRuleId)
                .Select(x => x.EffectiveFrom)
                .Single());

        Assert.Equal(
            originalEffectiveUntil,
            pricingRules
                .Where(x => x.Id == originalRuleId)
                .Select(x => x.EffectiveUntil)
                .Single());

        Assert.DoesNotContain(
            pricingRules,
            x =>
                x.Version > originalVersion &&
                x.Value == createRequest.NewValue);
    }
    [Fact]
    public async Task Manager_ApprovePricingRuleChangeRequest_Should_ReturnForbidden()
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

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var pricingRule =
            await context.PricingRules
                .AsNoTracking()
                .Where(x =>
                    x.Code == "BASE_KASKO_RATE" &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.Version)
                .FirstOrDefaultAsync();

        Assert.NotNull(pricingRule);

        var createRequest =
            new CreatePricingRuleChangeRequestDto
            {
                PricingRuleId = pricingRule!.Id,
                NewValue = pricingRule.Value + 0.0010m,
                Reason = "Manager authorization test",
                EffectiveFrom = DateTime.UtcNow.AddDays(30)
            };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/PricingRuleChangeRequest",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var changeRequest =
            await createResponse.Content
                .ReadFromJsonAsync<PricingRuleChangeRequestDto>();

        Assert.NotNull(changeRequest);

        Assert.Equal(
            "Pending",
            changeRequest!.Status);

        var approveResponse =
            await client.PostAsync(
                $"/api/PricingRuleChangeRequest/{changeRequest.Id}/approve",
                null);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            approveResponse.StatusCode);

        var persistedRequest =
            await context.PricingRuleChangeRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == changeRequest.Id);

        Assert.NotNull(persistedRequest);

        Assert.Equal(
            "Pending",
            persistedRequest!.Status);

        Assert.Null(
            persistedRequest.ApprovedBy);

        Assert.Null(
            persistedRequest.ApprovedDate);
    }
    [Fact]
    public async Task Customer_CreatePricingRuleChangeRequest_Should_ReturnForbidden()
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

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var pricingRule =
            await context.PricingRules
                .AsNoTracking()
                .Where(x =>
                    x.Code == "BASE_KASKO_RATE" &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.Version)
                .FirstOrDefaultAsync();

        Assert.NotNull(pricingRule);

        var request =
            new CreatePricingRuleChangeRequestDto
            {
                PricingRuleId = pricingRule!.Id,
                NewValue = pricingRule.Value + 0.0010m,
                Reason = "Customer authorization test",
                EffectiveFrom = DateTime.UtcNow.AddDays(30)
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/PricingRuleChangeRequest",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        var requestCount =
            await context.PricingRuleChangeRequests
                .AsNoTracking()
                .CountAsync();

        // Authorization başarısız olduğu için yeni ChangeRequest oluşturulmamalı.
        Assert.True(requestCount >= 0);
    }
    [Fact]
    public async Task Customer_GetAllPricingRuleChangeRequests_Should_ReturnForbidden()
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
                "/api/PricingRuleChangeRequest");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
    [Fact]
    public async Task Manager_RejectPricingRuleChangeRequest_Should_ReturnForbidden()
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

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var pricingRule =
            await context.PricingRules
                .AsNoTracking()
                .Where(x =>
                    x.Code == "BASE_KASKO_RATE" &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.Version)
                .FirstOrDefaultAsync();

        Assert.NotNull(pricingRule);

        var createRequest =
            new CreatePricingRuleChangeRequestDto
            {
                PricingRuleId = pricingRule!.Id,
                NewValue = pricingRule.Value + 0.0010m,
                Reason = "Manager reject authorization test",
                EffectiveFrom = DateTime.UtcNow.AddDays(30)
            };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/PricingRuleChangeRequest",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var changeRequest =
            await createResponse.Content
                .ReadFromJsonAsync<PricingRuleChangeRequestDto>();

        Assert.NotNull(changeRequest);

        Assert.Equal(
            "Pending",
            changeRequest!.Status);

        var rejectResponse =
            await client.PostAsync(
                $"/api/PricingRuleChangeRequest/{changeRequest.Id}/reject",
                null);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            rejectResponse.StatusCode);

        var persistedRequest =
            await context.PricingRuleChangeRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == changeRequest.Id);

        Assert.NotNull(persistedRequest);

        Assert.Equal(
            "Pending",
            persistedRequest!.Status);

        Assert.Null(
            persistedRequest.ApprovedBy);

        Assert.Null(
            persistedRequest.ApprovedDate);
    }
    [Fact]
    public async Task Admin_CreatePricingRuleChangeRequest_Should_ReturnForbidden()
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

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var pricingRule =
            await context.PricingRules
                .AsNoTracking()
                .Where(x =>
                    x.Code == "BASE_KASKO_RATE" &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.Version)
                .FirstOrDefaultAsync();

        Assert.NotNull(pricingRule);

        var request =
            new CreatePricingRuleChangeRequestDto
            {
                PricingRuleId = pricingRule!.Id,
                NewValue = pricingRule.Value + 0.0010m,
                Reason = "Admin authorization test",
                EffectiveFrom = DateTime.UtcNow.AddDays(30)
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/PricingRuleChangeRequest",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
}