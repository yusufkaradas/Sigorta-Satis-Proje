using Kasko.Business.DTOs.Auth;
using Kasko.Business.DTOs.Customer;
using Kasko.Business.DTOs.Policy;
using Kasko.Business.DTOs.Quote;
using Kasko.Business.DTOs.Vehicle;
using Kasko.DataAccess;
using Kasko.Entities.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Kasko.IntegrationTests;

public record TestUser(
    Guid Id,
    string Email,
    string Password,
    string Role);

public static class IntegrationTestHelper
{
    public static async Task<TestUser> SeedUserAsync(
        WebApplicationFactory<Program> factory,
        string role)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var passwordHasher =
            new PasswordHasher<User>();

        var roleEntity =
            await context.Roles
                .FirstOrDefaultAsync(x => x.Name == role);

        if (roleEntity == null)
        {
            roleEntity = new Role
            {
                Id = Guid.NewGuid(),
                Name = role,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            };

            context.Roles.Add(roleEntity);

            await context.SaveChangesAsync();
        }

        var userId = Guid.NewGuid();

        var email =
            $"integration.{role.ToLower()}.{userId:N}@test.local";

        const string password = "IntegrationTest123!";

        var user = new User
        {
            Id = userId,

            FirstName = "Integration",
            LastName = "Test",

            Email = email,

            PhoneNumber = "+905551234567",

            IsActive = true,
            IsDeleted = false,

            RoleId = roleEntity.Id,

            CreatedDate = DateTime.UtcNow
        };

        user.PasswordHash =
            passwordHasher.HashPassword(
                user,
                password);

        context.Users.Add(user);

        await context.SaveChangesAsync();

        return new TestUser(
            user.Id,
            email,
            password,
            role);
    }

    public static async Task<HttpClient> LoginAsync(
        WebApplicationFactory<Program> factory,
        TestUser user)
    {
        var client = factory.CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "/api/Auth/login",
                new
                {
                    Email = user.Email,
                    Password = user.Password
                });

        response.EnsureSuccessStatusCode();

        var login =
            await response.Content
                .ReadFromJsonAsync<LoginResponseDto>();

        Assert.NotNull(login);
        Assert.False(
            string.IsNullOrWhiteSpace(login!.Token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                login.Token);

        return client;
    }

    public static async Task<Guid> CreateCustomerAsync(
     HttpClient client)
    {
        var uniqueId = Guid.NewGuid();

        var identityNumber =
            Random.Shared.NextInt64(
                10000000000,
                99999999999)
            .ToString();

        var dto = new CreateCustomerDto
        {
            FirstName = "Integration",
            LastName = "Customer",

            IdentityNumber =
                identityNumber,

            DateOfBirth =
                new DateTime(1995, 1, 1),

            Email =
                $"customer.{uniqueId:N}@test.local",

            PhoneNumber =
                "+90555" +
                Random.Shared.Next(
                    1000000,
                    9999999),

            Address =
                "Integration Test Address",

            City =
                "Istanbul",

            District =
                "Kadikoy"
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/Customer",
                dto);

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var customers =
            await client.GetFromJsonAsync<
                IEnumerable<CustomerListDto>>(
                "/api/Customer");

        Assert.NotNull(customers);

        var customer =
            customers!.FirstOrDefault(
                x => x.Email == dto.Email);

        Assert.NotNull(customer);

        return customer!.Id;
    }
    public static async Task<VehicleDto> CreateVehicleAsync(
        HttpClient client,
        Guid customerId)
    {
        var random = Guid.NewGuid()
            .ToString("N")
            .Substring(0, 6)
            .ToUpper();

        var dto = new CreateVehicleDto
        {
            CustomerId = customerId,

            PlateNumber =
                $"34TEST{random.Substring(0, 3)}",

            VIN =
                "1HGCM82633A" +
                random,

            Brand = "Toyota",
            Model = "Corolla",

            ModelYear = 2024,

            VehicleType =
                Kasko.Entities.Enums.VehicleType.Sedan,

            FuelType =
                Kasko.Entities.Enums.FuelType.Gasoline,

            TransmissionType =
                Kasko.Entities.Enums.TransmissionType.Automatic,

            EngineVolume = 1.6m,
            EnginePower = 132,

            Color = "White",

            MarketValue = 1_250_000m
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/Vehicle",
                dto);

        response.EnsureSuccessStatusCode();

        var vehicle =
            await response.Content
                .ReadFromJsonAsync<VehicleDto>();

        Assert.NotNull(vehicle);

        return vehicle!;
    }

    public static async Task<QuoteDto> CreateQuoteAsync(
        HttpClient client,
        Guid customerId,
        Guid vehicleId)
    {
        var dto = new
        {
            CustomerId = customerId,
            VehicleId = vehicleId,

            ValidUntil =
                DateTime.UtcNow.AddDays(30)
        };

        var response =
            await client.PostAsJsonAsync(
                "/api/Quote",
                dto);

        response.EnsureSuccessStatusCode();

        var quote =
            await response.Content
                .ReadFromJsonAsync<QuoteDto>();

        Assert.NotNull(quote);

        return quote!;
    }

    public static async Task ChangeQuoteStatusAsync(
        HttpClient client,
        Guid quoteId,
        Kasko.Entities.Enums.QuoteStatus status)
    {
        var response =
            await client.PatchAsync(
                $"/api/Quote/{quoteId}/status?status={(int)status}",
                null);

        response.EnsureSuccessStatusCode();
    }

    public static async Task<PolicyDto> CreatePolicyAsync(
        HttpClient client,
        Guid customerId,
        Guid vehicleId,
        Guid quoteId)
    {
        var response =
            await client.PostAsJsonAsync(
                "/api/Policy",
                new
                {
                    CustomerId = customerId,
                    VehicleId = vehicleId,
                    QuoteId = quoteId,

                    StartDate =
                        DateTime.UtcNow,

                    EndDate =
                        DateTime.UtcNow.AddYears(1)
                });

        response.EnsureSuccessStatusCode();

        var policy =
            await response.Content
                .ReadFromJsonAsync<PolicyDto>();

        Assert.NotNull(policy);

        return policy!;
    }
    public static async Task<QuotePricingSnapshot?> GetQuotePricingSnapshotAsync(
    WebApplicationFactory<Program> factory,
    Guid quoteId)
    {
        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        return await context.QuotePricingSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.QuoteId == quoteId &&
                    !x.IsDeleted);
    }
}