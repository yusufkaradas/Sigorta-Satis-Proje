using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kasko.IntegrationTests;

public class CorsTests
{
    [Fact]
    public async Task LocalAngularOrigin_ShouldBeAllowed()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        using var request =
            new HttpRequestMessage(
                HttpMethod.Options,
                "/api/Customer");

        request.Headers.Add(
            "Origin",
            "http://localhost:4200");

        request.Headers.Add(
            "Access-Control-Request-Method",
            "GET");

        var response =
            await client.SendAsync(request);

        Assert.True(
            response.StatusCode == HttpStatusCode.NoContent ||
            response.StatusCode == HttpStatusCode.OK);

        Assert.True(
            response.Headers.TryGetValues(
                "Access-Control-Allow-Origin",
                out var values));

        Assert.Contains(
            "http://localhost:4200",
            values);
    }
}