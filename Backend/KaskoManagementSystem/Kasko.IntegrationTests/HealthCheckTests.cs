using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kasko.IntegrationTests;

public class HealthCheckTests
{
    [Fact]
    public async Task Health_Should_ReturnOk()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var response =
            await client.GetAsync("/health");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }
}