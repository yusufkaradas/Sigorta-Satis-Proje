using System.Net;
using System.Net.Http.Json;
using Kasko.Entities.Enums;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kasko.IntegrationTests;

public class ConcurrencyTests
{
    [Fact]
    public async Task UpdatingPolicyWithOldRowVersion_ShouldReturnConflict()
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
                client,
                customerId);

        var quote =
            await IntegrationTestHelper.CreateQuoteAsync(
                client,
                customerId,
                vehicle.Id);

        await IntegrationTestHelper.ChangeQuoteStatusAsync(
            client,
            quote.Id,
            QuoteStatus.Offered);

        await IntegrationTestHelper.ChangeQuoteStatusAsync(
            client,
            quote.Id,
            QuoteStatus.Accepted);

        var policy =
            await IntegrationTestHelper.CreatePolicyAsync(
                client,
                customerId,
                vehicle.Id,
                quote.Id);

        // Get the current RowVersion
        var currentPolicy =
            await client.GetFromJsonAsync<
                Kasko.Business.DTOs.Policy.PolicyDto>(
                $"/api/Policy/{policy.Id}");

        Assert.NotNull(currentPolicy);

        var oldRowVersion =
            currentPolicy!.RowVersion;

        // First update
        var firstUpdate =
            await client.PutAsJsonAsync(
                $"/api/Policy/{policy.Id}",
                new
                {
                    EndDate =
                        DateTime.UtcNow.AddYears(2),

                    RowVersion =
                        oldRowVersion
                });

        Assert.Equal(
            HttpStatusCode.NoContent,
            firstUpdate.StatusCode);

        // Second update uses stale RowVersion
        var secondUpdate =
            await client.PutAsJsonAsync(
                $"/api/Policy/{policy.Id}",
                new
                {
                    EndDate =
                        DateTime.UtcNow.AddYears(3),

                    RowVersion =
                        oldRowVersion
                });

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondUpdate.StatusCode);
    }
}