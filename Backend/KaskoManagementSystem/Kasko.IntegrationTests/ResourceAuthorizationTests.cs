using Kasko.Business.DTOs.Quote;
using Kasko.Business.DTOs.Policy;
using Kasko.Entities.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;



namespace Kasko.IntegrationTests;

public class ResourceAuthorizationTests
{
    [Fact]
    public async Task Customer_Cannot_Get_AnotherCustomersVehicle()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        // Admin kullanıcı
        var adminUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var adminClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                adminUser);

        // Customer A
        var customerAId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        // Customer B
        var customerBId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        // Customer B'ye ait araç
        var customerBVehicle =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                adminClient,
                customerBId);

        Assert.Equal(
            customerBId,
            customerBVehicle.CustomerId);

        // Customer A kullanıcısı
        var customerAUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Customer",
                customerAId);

        using var customerAClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                customerAUser);

        // Customer A, Customer B'nin aracına erişmeye çalışıyor
        var response =
            await customerAClient.GetAsync(
                $"/api/Vehicle/{customerBVehicle.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
    [Fact]
    public async Task Customer_Cannot_Get_AnotherCustomersQuote()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        // Admin
        var adminUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var adminClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                adminUser);

        // Customer A
        var customerAId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        // Customer B
        var customerBId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        // Customer B'ye ait Vehicle
        var vehicleB =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                adminClient,
                customerBId);

        // Customer B'ye ait Quote
        var quoteB =
            await IntegrationTestHelper.CreateQuoteAsync(
                adminClient,
                customerBId,
                vehicleB.Id);

        // Customer A kullanıcısı
        var customerAUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Customer",
                customerAId);

        using var customerAClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                customerAUser);

        // Customer A, Customer B'nin quote'una erişmeye çalışıyor
        var response =
            await customerAClient.GetAsync(
                $"/api/Quote/{quoteB.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
    [Fact]
    public async Task Customer_Cannot_Get_AnotherCustomersPolicy()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        // Admin
        var adminUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var adminClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                adminUser);

        // Customer A
        var customerAId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        // Customer B
        var customerBId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        // Customer B Vehicle
        var vehicleB =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                adminClient,
                customerBId);

        // Customer B Quote
        var quoteB =
            await IntegrationTestHelper.CreateQuoteAsync(
                adminClient,
                customerBId,
                vehicleB.Id);

        // Quote'u Accepted durumuna getir
        await IntegrationTestHelper.ChangeQuoteStatusAsync(
            adminClient,
            quoteB.Id,
            QuoteStatus.Offered);

        await IntegrationTestHelper.ChangeQuoteStatusAsync(
            adminClient,
            quoteB.Id,
            QuoteStatus.Accepted);

        // Customer B Policy
        var policyB =
            await IntegrationTestHelper.CreatePolicyAsync(
                adminClient,
                customerBId,
                vehicleB.Id,
                quoteB.Id);

        // Customer A User
        var customerAUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Customer",
                customerAId);

        using var customerAClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                customerAUser);

        // Customer A, Customer B'nin policy'sine erişmeye çalışıyor
        var response =
            await customerAClient.GetAsync(
                $"/api/Policy/{policyB.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
    [Fact]
    public async Task Customer_Can_Get_Only_Own_Vehicles_From_List()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        // Admin
        var adminUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var adminClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                adminUser);

        // Customer A
        var customerAId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        // Customer B
        var customerBId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        // Customer A Vehicle
        var customerAVehicle =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                adminClient,
                customerAId);

        // Customer B Vehicle
        var customerBVehicle =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                adminClient,
                customerBId);

        // Customer A kullanıcısı
        var customerAUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Customer",
                customerAId);

        using var customerAClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                customerAUser);

        // Customer A kendi listesine erişiyor
        var response =
            await customerAClient.GetAsync(
                "/api/Vehicle");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var vehicles =
            await response.Content
                .ReadFromJsonAsync<
                    IEnumerable<Kasko.Business.DTOs.Vehicle.VehicleListDto>>();

        Assert.NotNull(vehicles);

        Assert.Contains(
            vehicles!,
            x => x.Id == customerAVehicle.Id);

        Assert.DoesNotContain(
            vehicles!,
            x => x.Id == customerBVehicle.Id);

    }
    [Fact]
    public async Task Customer_Can_Get_Only_Own_Quotes_From_List()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        // Admin kullanıcı
        var adminUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var adminClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                adminUser);

        // Customer A
        var customerAId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        // Customer B
        var customerBId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        // Customer A Vehicle
        var vehicleA =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                adminClient,
                customerAId);

        // Customer B Vehicle
        var vehicleB =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                adminClient,
                customerBId);

        // Customer A Quote
        var quoteA =
            await IntegrationTestHelper.CreateQuoteAsync(
                adminClient,
                customerAId,
                vehicleA.Id);

        // Customer B Quote
        var quoteB =
            await IntegrationTestHelper.CreateQuoteAsync(
                adminClient,
                customerBId,
                vehicleB.Id);

        Assert.Equal(
            customerAId,
            quoteA.CustomerId);

        Assert.Equal(
            customerBId,
            quoteB.CustomerId);

        // Customer A kullanıcısı
        var customerAUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Customer",
                customerAId);

        using var customerAClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                customerAUser);

        // Customer A kendi quote listesini getiriyor
        var response =
            await customerAClient.GetAsync(
                "/api/Quote");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var quotes =
            await response.Content
                .ReadFromJsonAsync<IEnumerable<QuoteListDto>>();

        Assert.NotNull(quotes);

        Assert.Contains(
            quotes!,
            x => x.Id == quoteA.Id);

        Assert.DoesNotContain(
            quotes!,
            x => x.Id == quoteB.Id);
    }
    [Fact]
    public async Task Customer_Can_Get_Only_Own_Policies_From_List()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        // Admin
        var adminUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Admin");

        using var adminClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                adminUser);

        // Customer A
        var customerAId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        // Customer B
        var customerBId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        // Customer A Vehicle + Quote
        var vehicleA =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                adminClient,
                customerAId);

        var quoteA =
            await IntegrationTestHelper.CreateQuoteAsync(
                adminClient,
                customerAId,
                vehicleA.Id);

        await IntegrationTestHelper.ChangeQuoteStatusAsync(
            adminClient,
            quoteA.Id,
            QuoteStatus.Offered);

        await IntegrationTestHelper.ChangeQuoteStatusAsync(
            adminClient,
            quoteA.Id,
            QuoteStatus.Accepted);

        var policyA =
            await IntegrationTestHelper.CreatePolicyAsync(
                adminClient,
                customerAId,
                vehicleA.Id,
                quoteA.Id);

        // Customer B Vehicle + Quote
        var vehicleB =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                adminClient,
                customerBId);

        var quoteB =
            await IntegrationTestHelper.CreateQuoteAsync(
                adminClient,
                customerBId,
                vehicleB.Id);

        await IntegrationTestHelper.ChangeQuoteStatusAsync(
            adminClient,
            quoteB.Id,
            QuoteStatus.Offered);

        await IntegrationTestHelper.ChangeQuoteStatusAsync(
            adminClient,
            quoteB.Id,
            QuoteStatus.Accepted);

        var policyB =
            await IntegrationTestHelper.CreatePolicyAsync(
                adminClient,
                customerBId,
                vehicleB.Id,
                quoteB.Id);

        // Customer A kullanıcısı
        var customerAUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Customer",
                customerAId);

        using var customerAClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                customerAUser);

        // Customer A kendi policy listesini getiriyor
        var response =
            await customerAClient.GetAsync(
                "/api/Policy");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var policies =
            await response.Content
                .ReadFromJsonAsync<
                    IEnumerable<
                        Kasko.Business.DTOs.Policy.PolicyListDto>>();

        Assert.NotNull(policies);

        Assert.Contains(
            policies!,
            x => x.Id == policyA.Id);

        Assert.DoesNotContain(
            policies!,
            x => x.Id == policyB.Id);
    }
    [Fact]
    public async Task Customer_Cannot_Update_AnotherCustomersQuote()
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

        var customerAId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        var customerBId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        var vehicleB =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                adminClient,
                customerBId);

        var quoteB =
            await IntegrationTestHelper.CreateQuoteAsync(
                adminClient,
                customerBId,
                vehicleB.Id);

        var customerAUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Customer",
                customerAId);

        using var customerAClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                customerAUser);

        var updateDto = new UpdateQuoteDto
        {
            ValidUntil = quoteB.ValidUntil.AddDays(1)
        };

        var response =
            await customerAClient.PutAsJsonAsync(
                $"/api/Quote/{quoteB.Id}",
                updateDto);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
    [Fact]
    public async Task Customer_Cannot_Delete_AnotherCustomersQuote()
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

        // Customer A
        var customerAId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        // Customer B
        var customerBId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        // Customer B Vehicle
        var vehicleB =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                adminClient,
                customerBId);

        // Customer B Quote
        var quoteB =
            await IntegrationTestHelper.CreateQuoteAsync(
                adminClient,
                customerBId,
                vehicleB.Id);

        // Customer A kullanıcı
        var customerAUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Customer",
                customerAId);

        using var customerAClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                customerAUser);

        var response =
            await customerAClient.DeleteAsync(
                $"/api/Quote/{quoteB.Id}");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }
    [Fact]
    public async Task Customer_Cannot_ChangeStatus_Of_AnotherCustomersQuote()
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

        var customerAId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        var customerBId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        var vehicleB =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                adminClient,
                customerBId);

        var quoteB =
            await IntegrationTestHelper.CreateQuoteAsync(
                adminClient,
                customerBId,
                vehicleB.Id);

        await IntegrationTestHelper.ChangeQuoteStatusAsync(
              adminClient,
              quoteB.Id,
              QuoteStatus.Offered);

        var customerAUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Customer",
                customerAId);

        using var customerAClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                customerAUser);

        var response =
            await customerAClient.PatchAsync(
                $"/api/Quote/{quoteB.Id}/status?status=Accepted",
                null);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
    [Fact]
    public async Task Customer_Cannot_Update_AnotherCustomersPolicy()
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

        var customerAId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        var customerBId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        var vehicleB =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                adminClient,
                customerBId);

        var quoteB =
            await IntegrationTestHelper.CreateQuoteAsync(
                adminClient,
                customerBId,
                vehicleB.Id);

        await IntegrationTestHelper.ChangeQuoteStatusAsync(
            adminClient,
            quoteB.Id,
            QuoteStatus.Offered);

        await IntegrationTestHelper.ChangeQuoteStatusAsync(
            adminClient,
            quoteB.Id,
            QuoteStatus.Accepted);

        var policyB =
            await IntegrationTestHelper.CreatePolicyAsync(
                adminClient,
                customerBId,
                vehicleB.Id,
                quoteB.Id);

        var customerAUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Customer",
                customerAId);

        using var customerAClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                customerAUser);

        var updateDto = new PolicyUpdateDto
        {
            EndDate = policyB.EndDate.AddDays(1),
            RowVersion = policyB.RowVersion
        };

        var response =
            await customerAClient.PutAsJsonAsync(
                $"/api/Policy/{policyB.Id}",
                updateDto);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
    [Fact]
    public async Task Customer_Cannot_Delete_AnotherCustomersPolicy()
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

        var customerAId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        var customerBId =
            await IntegrationTestHelper.CreateCustomerAsync(
                adminClient);

        var vehicleB =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                adminClient,
                customerBId);

        var quoteB =
            await IntegrationTestHelper.CreateQuoteAsync(
                adminClient,
                customerBId,
                vehicleB.Id);

        await IntegrationTestHelper.ChangeQuoteStatusAsync(
            adminClient,
            quoteB.Id,
            QuoteStatus.Offered);

        await IntegrationTestHelper.ChangeQuoteStatusAsync(
            adminClient,
            quoteB.Id,
            QuoteStatus.Accepted);

        var policyB =
            await IntegrationTestHelper.CreatePolicyAsync(
                adminClient,
                customerBId,
                vehicleB.Id,
                quoteB.Id);

        var customerAUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Customer",
                customerAId);

        using var customerAClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                customerAUser);

        var response =
            await customerAClient.DeleteAsync(
                $"/api/Policy/{policyB.Id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task Manager_Can_Read_But_Cannot_Write_Operational_Data()
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

        var vehicle =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                adminClient,
                customerId);

        var quote =
            await IntegrationTestHelper.CreateQuoteAsync(
                adminClient,
                customerId,
                vehicle.Id);

        var managerUser =
            await IntegrationTestHelper.SeedUserAsync(
                factory,
                "Manager");

        using var managerClient =
            await IntegrationTestHelper.LoginAsync(
                factory,
                managerUser);

        var readResponse =
            await managerClient.GetAsync(
                $"/api/Quote/{quote.Id}");

        Assert.Equal(
            HttpStatusCode.OK,
            readResponse.StatusCode);

        var createResponse =
            await managerClient.PostAsJsonAsync(
                "/api/Quote",
                new
                {
                    CustomerId = customerId,
                    VehicleId = vehicle.Id,
                    ValidUntil = DateTime.UtcNow.AddDays(30)
                });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            createResponse.StatusCode);

        var deleteResponse =
            await managerClient.DeleteAsync(
                $"/api/Quote/{quote.Id}");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            deleteResponse.StatusCode);

        var policyResponse =
            await managerClient.PostAsJsonAsync(
                "/api/Policy",
                new
                {
                    CustomerId = customerId,
                    VehicleId = vehicle.Id,
                    QuoteId = quote.Id
                });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            policyResponse.StatusCode);
    }
}