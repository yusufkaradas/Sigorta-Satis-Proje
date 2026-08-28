using Microsoft.AspNetCore.Mvc.Testing;


namespace Kasko.IntegrationTests;

public class ApiSmokeTests
{
    [Fact]
    public async Task Api_Should_Start_Successfully()
    {
        await using var factory = new WebApplicationFactory<Program>();

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.NotNull(response);
    }
}