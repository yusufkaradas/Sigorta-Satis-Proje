using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Net;


namespace Kasko.IntegrationTests;

public class AuthenticationTests
{
    [Fact]
    public async Task Customer_GetAll_WithoutToken_Should_Return_Unauthorized()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/Customer");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    [Fact]
    public async Task Customer_Create_WithoutToken_Should_Return_Unauthorized()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/Customer",
            new
            {
                FirstName = "Integration",
                LastName = "Test",
                IdentityNumber = "12345678901",
                DateOfBirth = new DateTime(1995, 1, 1),
                Email = "integration@test.com",
                Address = "Test Address",
                City = "Istanbul",
                PhoneNumber = "5551112233",
                District = "Kadikoy"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    [Fact]
    public async Task Login_WithoutToken_Should_Not_Return_Unauthorized()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/Auth/login",
            new
            {
                Email = "integration-nonexistent@test.com",
                Password = "Test123!"
            });

        Assert.NotEqual(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    [Fact]
    public async Task Login_WithValidCredentials_Should_ReturnToken()
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

        Assert.NotNull(
            client.DefaultRequestHeaders.Authorization);

        var response =
            await client.GetAsync("/api/Customer");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Should_NotReturnToken()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        var user =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var client =
            factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    Email = user.Email,
                    Password = "WrongPassword123!"
                });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Should_ReturnUnauthorized()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        using var client =
            factory.CreateClient();

        var response =
            await client.GetAsync("/api/Customer");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

}