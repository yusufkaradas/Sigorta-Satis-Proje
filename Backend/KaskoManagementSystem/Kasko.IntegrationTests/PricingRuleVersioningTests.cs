using Kasko.DataAccess;
using Kasko.DataAccess.Repositories.Abstract;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace Kasko.IntegrationTests;

public class PricingRuleVersioningTests
{
    [Fact]
    public async Task GetApplicableRuleAsync_ShouldSelectCorrectVersionByEffectiveDate()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IPricingRuleRepository>();

        // 15.08.2026 -> V1
        var v1BeforeEnd =
            await repository.GetApplicableRuleAsync(
                "BASE_KASKO_RATE",
                new DateTime(2026, 8, 15));

        Assert.NotNull(v1BeforeEnd);
        Assert.Equal(1, v1BeforeEnd!.Version);
        Assert.Equal(0.0200m, v1BeforeEnd.Value);

        // 31.08.2026 -> V1
        var v1LastDay =
            await repository.GetApplicableRuleAsync(
                "BASE_KASKO_RATE",
                new DateTime(2026, 8, 31));

        Assert.NotNull(v1LastDay);
        Assert.Equal(1, v1LastDay!.Version);
        Assert.Equal(0.0200m, v1LastDay.Value);

        // 01.09.2026 -> V2
        var v2FirstDay =
            await repository.GetApplicableRuleAsync(
                "BASE_KASKO_RATE",
                new DateTime(2026, 9, 1));

        Assert.NotNull(v2FirstDay);
        Assert.Equal(2, v2FirstDay!.Version);
        Assert.Equal(0.0215m, v2FirstDay.Value);

        // 15.09.2026 -> V2
        var v2AfterStart =
            await repository.GetApplicableRuleAsync(
                "BASE_KASKO_RATE",
                new DateTime(2026, 9, 15));

        Assert.NotNull(v2AfterStart);
        Assert.Equal(2, v2AfterStart!.Version);
        Assert.Equal(0.0215m, v2AfterStart.Value);

        // 31.10.2026 -> V2
        var v2LastDay =
            await repository.GetApplicableRuleAsync(
                "BASE_KASKO_RATE",
                new DateTime(2026, 10, 31));

        Assert.NotNull(v2LastDay);
        Assert.Equal(2, v2LastDay!.Version);
        Assert.Equal(0.0215m, v2LastDay.Value);

        // 01.11.2026 -> V3
        var v3FirstDay =
            await repository.GetApplicableRuleAsync(
                "BASE_KASKO_RATE",
                new DateTime(2026, 11, 1));

        Assert.NotNull(v3FirstDay);
        Assert.Equal(3, v3FirstDay!.Version);
        Assert.Equal(0.0230m, v3FirstDay.Value);

        // 15.11.2026 -> V3
        var v3AfterStart =
            await repository.GetApplicableRuleAsync(
                "BASE_KASKO_RATE",
                new DateTime(2026, 11, 15));

        Assert.NotNull(v3AfterStart);
        Assert.Equal(3, v3AfterStart!.Version);
        Assert.Equal(0.0230m, v3AfterStart.Value);
    }
}