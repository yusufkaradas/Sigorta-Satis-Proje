using System.Net;
using System.Net.Http.Json;
using Kasko.Entities.Enums;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kasko.IntegrationTests;

public class E2ELifecycleTests
{
    [Fact]
    public async Task FullPolicyLifecycle_ShouldCompleteSuccessfully()
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

        Assert.NotEqual(
            Guid.Empty,
            customerId);

        
        var vehicle =
            await IntegrationTestHelper.CreateVehicleAsync(
                factory,
                client,
                customerId);

        Assert.Equal(
            customerId,
            vehicle.CustomerId);

        
        var quote =
            await IntegrationTestHelper.CreateQuoteAsync(
                client,
                customerId,
                vehicle.Id);

        Assert.Equal(
            QuoteStatus.Draft,
            quote.Status);

        Assert.Equal(
               24187.50m,
    quote.PremiumAmount);
        var snapshot =
    await IntegrationTestHelper
        .GetQuotePricingSnapshotAsync(
            factory,
            quote.Id);

        Assert.NotNull(snapshot);

        Assert.Equal(
            quote.Id,
            snapshot!.QuoteId);

        Assert.Equal(
            quote.PremiumAmount,
            snapshot.FinalPremium);

        Assert.True(
            snapshot.MarketValue > 0);

        Assert.True(
            snapshot.BaseRate > 0);


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

        Assert.Equal(
            PolicyStatus.Draft,
            policy.Status);

        
        var paymentResponse =
            await client.PostAsJsonAsync(
                "/api/Payment",
                new
                {
                    PolicyId = policy.Id,
                    SimulateFailure = false
                });

        Assert.Equal(
            HttpStatusCode.Created,
            paymentResponse.StatusCode);

        var payment =
            await paymentResponse.Content
                .ReadFromJsonAsync<
                    Kasko.Business.DTOs.Payment.PaymentDto>();

        Assert.NotNull(payment);

        Assert.Equal(
            PaymentStatus.Successful,
            payment!.Status);

        
        var activePolicy =
            await client.GetFromJsonAsync<
                Kasko.Business.DTOs.Policy.PolicyDto>(
                $"/api/Policy/{policy.Id}");

        Assert.NotNull(activePolicy);

        Assert.Equal(
            PolicyStatus.Active,
            activePolicy!.Status);

        Assert.True(
            activePolicy.IsActive);

        
        var cancelResponse =
            await client.PostAsync(
                $"/api/Policy/{policy.Id}/cancel",
                null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            cancelResponse.StatusCode);

        var cancelledPolicy =
            await client.GetFromJsonAsync<
                Kasko.Business.DTOs.Policy.PolicyDto>(
                $"/api/Policy/{policy.Id}");

        Assert.NotNull(cancelledPolicy);

        Assert.Equal(
            PolicyStatus.Cancelled,
            cancelledPolicy!.Status);
    }
    [Fact]
    public async Task FailedPayment_ShouldKeepPolicyInDraft()
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
                factory,
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

        var paymentResponse =
            await client.PostAsJsonAsync(
                "/api/Payment",
                new
                {
                    PolicyId = policy.Id,
                    SimulateFailure = true
                });

        Assert.Equal(
            HttpStatusCode.Created,
            paymentResponse.StatusCode);

        var payment =
            await paymentResponse.Content
                .ReadFromJsonAsync<
                    Kasko.Business.DTOs.Payment.PaymentDto>();

        Assert.NotNull(payment);

        Assert.Equal(
            PaymentStatus.Failed,
            payment!.Status);

        Assert.Equal(
            "Simüle edilen ödeme hatası.",
            payment.FailureReason);

        var currentPolicy =
            await client.GetFromJsonAsync<
                Kasko.Business.DTOs.Policy.PolicyDto>(
                $"/api/Policy/{policy.Id}");

        Assert.NotNull(currentPolicy);

        Assert.Equal(
            PolicyStatus.Draft,
            currentPolicy!.Status);
    }
}