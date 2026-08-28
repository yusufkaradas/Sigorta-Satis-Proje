using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kasko.IntegrationTests;

public class SoftDeleteTests
{
    [Fact]
    public async Task DeletedCustomer_ShouldNotBeReturned()
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

        var deleteResponse =
            await client.DeleteAsync(
                $"/api/Customer/{customerId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var getResponse =
            await client.GetAsync(
                $"/api/Customer/{customerId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }
}