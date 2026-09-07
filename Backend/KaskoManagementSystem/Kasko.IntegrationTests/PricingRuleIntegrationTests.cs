using Kasko.Business.DTOs.PricingRule;
using Kasko.DataAccess;
using Kasko.DataAccess.Repositories.Abstract;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;


namespace Kasko.IntegrationTests;

public class PricingRuleIntegrationTests
{
    [Fact]
    public async Task Manager_GetPricingRules_Should_ReturnSuccess()
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
                "/api/PricingRule");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var rules =
            await response.Content
                .ReadFromJsonAsync<
                    IEnumerable<PricingRuleDto>>();

        Assert.NotNull(rules);

        Assert.Contains(
            rules!,
            x => x.Code == "BASE_KASKO_RATE");

        Assert.All(
            rules!,
            rule =>
            {
                Assert.NotEqual(
                    Guid.Empty,
                    rule.Id);

                Assert.False(
                    string.IsNullOrWhiteSpace(
                        rule.Code));
            });
    }
    [Fact]
    public async Task Customer_GetPricingRules_Should_ReturnForbidden()
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
                "/api/PricingRule");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
    [Fact]
    public async Task Admin_CreatePricingRule_Should_CreateRule()
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
            new CreatePricingRuleDto
            {
                Code = $"INTEGRATION_TEST_RULE_{Guid.NewGuid():N}",
                Name = $"Integration Test Rule {Guid.NewGuid():N}",
                Description = "Created by integration test",
                Value = 0.1234m,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow,
                EffectiveUntil = null
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/PricingRule",
                request);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<PricingRuleDto>();

        Assert.NotNull(result);

        Assert.NotEqual(
            Guid.Empty,
            result!.Id);

        Assert.Equal(
            request.Code,
            result.Code);

        Assert.Equal(
            request.Name,
            result.Name);

        Assert.Equal(
            request.Description,
            result.Description);

        Assert.Equal(
            request.Value,
            result.Value);

        Assert.Equal(
            request.IsActive,
            result.IsActive);

        Assert.Equal(
            request.EffectiveFrom,
            result.EffectiveFrom);

        Assert.Equal(
            request.EffectiveUntil,
            result.EffectiveUntil);

        Assert.Equal(
            1,
            result.Version);

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
    scope.ServiceProvider
        .GetRequiredService<KaskoContext>();

        var pricingRuleRepository =
            scope.ServiceProvider
                .GetRequiredService<IPricingRuleRepository>();

        var persistedRule =
            await pricingRuleRepository
                .GetByIdAsync(result.Id);

        Assert.NotNull(persistedRule);

        Assert.Equal(
            request.Code,
            persistedRule!.Code);

        Assert.Equal(
            request.Value,
            persistedRule.Value);

        Assert.Equal(
            1,
            persistedRule.Version);
    }
    [Fact]
    public async Task Manager_CreatePricingRule_Should_ReturnForbidden()
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
            new CreatePricingRuleDto
            {
                Code = "MANAGER_FORBIDDEN_TEST_RULE",
                Name = "Manager Forbidden Test Rule",
                Description = "Should not be created by Manager",
                Value = 0.1234m,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow,
                EffectiveUntil = null
            };

        var response =
            await client.PostAsJsonAsync(
                "/api/PricingRule",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
    [Fact]
    public async Task Admin_UpdatePricingRule_Should_UpdateRule()
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

        var pricingRuleRepository =
            scope.ServiceProvider
                .GetRequiredService<IPricingRuleRepository>();

        var rule =
            (await pricingRuleRepository.GetAllAsync())
                .Where(x =>
                    x.Code == "INTEGRATION_TEST_RULE" &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();

        Assert.NotNull(rule);

        var request =
            new UpdatePricingRuleDto
            {
                Id = rule!.Id,
                Name = "Updated Integration Test Rule",
                Description = "Updated by integration test",
                Value = 0.5678m,
                IsActive = false
            };

        var response =
            await client.PutAsJsonAsync(
                "/api/PricingRule",
                request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var verificationScope =
    factory.Services.CreateAsyncScope();

        var verificationRepository =
            verificationScope.ServiceProvider
                .GetRequiredService<IPricingRuleRepository>();

        var updatedRule =
            await verificationRepository.GetByIdAsync(
                rule.Id);

        Assert.NotNull(updatedRule);

        Assert.Equal(
            request.Name,
            updatedRule!.Name);

        Assert.Equal(
            request.Description,
            updatedRule.Description);

        Assert.Equal(
            request.Value,
            updatedRule.Value);

        Assert.Equal(
            request.IsActive,
            updatedRule.IsActive);

        Assert.Equal(
            rule.Version,
            updatedRule.Version);
    }
    [Fact]
    public async Task Manager_UpdatePricingRule_Should_ReturnForbidden()
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

        var pricingRuleRepository =
            scope.ServiceProvider
                .GetRequiredService<IPricingRuleRepository>();

        var rule =
            (await pricingRuleRepository.GetAllAsync())
                .Where(x =>
                    x.Code == "INTEGRATION_TEST_RULE" &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();

        Assert.NotNull(rule);

        var request =
            new UpdatePricingRuleDto
            {
                Id = rule!.Id,
                Name = "Manager Should Not Update",
                Description = "Authorization test",
                Value = rule.Value + 0.001m,
                IsActive = rule.IsActive
            };

        var response =
            await client.PutAsJsonAsync(
                "/api/PricingRule",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
    [Fact]
    public async Task Admin_DeletePricingRule_Should_SoftDeleteRule()
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

        var createRequest =
    new CreatePricingRuleDto
    {
        Code = "INTEGRATION_DELETE_TEST_RULE",
        Name = "Integration Delete Test Rule",
        Description = "Created for delete integration test",
        Value = 0.2222m,
        IsActive = true,
        EffectiveFrom = DateTime.UtcNow,
        EffectiveUntil = null
    };

        var createResponse =
            await client.PostAsJsonAsync(
                "/api/PricingRule",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var rule =
            await createResponse.Content
                .ReadFromJsonAsync<PricingRuleDto>();

        Assert.NotNull(rule);

        var response =
            await client.DeleteAsync(
                $"/api/PricingRule/{rule!.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        await using var verificationScope =
            factory.Services.CreateAsyncScope();

        var verificationRepository =
            verificationScope.ServiceProvider
                .GetRequiredService<IPricingRuleRepository>();

        var verificationContext =
    verificationScope.ServiceProvider
        .GetRequiredService<KaskoContext>();

        var rowCount =
            await verificationContext.Database
                .SqlQueryRaw<int>(
                    "SELECT COUNT(*) AS [Value] FROM PricingRules WHERE Id = {0}",
                    rule.Id)
                .SingleAsync();

        Assert.Equal(
            1,
            rowCount);

        var isDeleted =
            await verificationContext.Database
                .SqlQueryRaw<bool>(
                    "SELECT IsDeleted AS [Value] FROM PricingRules WHERE Id = {0}",
                    rule.Id)
                .SingleAsync();

        Assert.True(
            isDeleted);
    }
    [Fact]
    public async Task Manager_DeletePricingRule_Should_ReturnForbidden()
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

        var admin =
    await IntegrationTestHelper.SeedUserAsync(
        factory,
        "Admin");

        using var adminClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                admin);

        var createRequest =
            new CreatePricingRuleDto
            {
                Code = "MANAGER_DELETE_FORBIDDEN_TEST_RULE",
                Name = "Manager Delete Forbidden Test Rule",
                Description = "Created for authorization integration test",
                Value = 0.3333m,
                IsActive = true,
                EffectiveFrom = DateTime.UtcNow,
                EffectiveUntil = null
            };

        var createResponse =
            await adminClient.PostAsJsonAsync(
                "/api/PricingRule",
                createRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var pricingRule =
            await createResponse.Content
                .ReadFromJsonAsync<PricingRuleDto>();

        Assert.NotNull(pricingRule);

        var deleteResponse =
            await client.DeleteAsync(
                $"/api/PricingRule/{pricingRule!.Id}");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            deleteResponse.StatusCode);

        var response =
            await client.DeleteAsync(
                $"/api/PricingRule/{pricingRule!.Id}");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
}