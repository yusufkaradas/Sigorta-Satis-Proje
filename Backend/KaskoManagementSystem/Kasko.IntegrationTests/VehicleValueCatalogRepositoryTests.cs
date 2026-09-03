using Kasko.DataAccess;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Kasko.IntegrationTests;

public class VehicleValueCatalogRepositoryTests
{
    [Fact]
    public async Task GetActiveByKeyAsync_ShouldReturnActiveMatchingRecord()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IVehicleValueCatalogRepository>();

        var brandCode =
            $"TEST-BRAND-{Guid.NewGuid():N}";

        var typeCode =
            $"TEST-TYPE-{Guid.NewGuid():N}";

        var catalog =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = typeCode,

                BrandName = "Test Brand",
                TypeName = "Test Type",

                ModelYear = 2024,
                Value = 1_250_000m,

                Source = "IntegrationTest",

                EffectiveDate =
                    DateTime.UtcNow,

                ImportedAt =
                    DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate =
                    DateTime.UtcNow
            };

        context.VehicleValueCatalogs.Add(catalog);

        await context.SaveChangesAsync();

        var result =
            await repository.GetActiveByKeyAsync(
                brandCode,
                typeCode,
                2024);

        Assert.NotNull(result);

        Assert.Equal(
            catalog.Id,
            result!.Id);

        Assert.Equal(
            brandCode,
            result.BrandCode);

        Assert.Equal(
            typeCode,
            result.TypeCode);

        Assert.Equal(
            2024,
            result.ModelYear);

        Assert.Equal(
            1_250_000m,
            result.Value);

        context.VehicleValueCatalogs.Remove(catalog);

        await context.SaveChangesAsync();
    }
    [Fact]
    public async Task GetActiveByKeyAsync_WhenRecordIsDeleted_ShouldReturnNull()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IVehicleValueCatalogRepository>();

        var brandCode =
            $"TEST-BRAND-{Guid.NewGuid():N}";

        var typeCode =
            $"TEST-TYPE-{Guid.NewGuid():N}";

        var catalog =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = typeCode,

                BrandName = "Test Brand",
                TypeName = "Test Type",

                ModelYear = 2024,
                Value = 1_250_000m,

                Source = "IntegrationTest",

                EffectiveDate =
                    DateTime.UtcNow,

                ImportedAt =
                    DateTime.UtcNow,

                IsActive = true,
                IsDeleted = true,

                CreatedDate =
                    DateTime.UtcNow
            };

        context.VehicleValueCatalogs.Add(catalog);

        await context.SaveChangesAsync();

        var result =
            await repository.GetActiveByKeyAsync(
                brandCode,
                typeCode,
                2024);

        Assert.Null(result);

        context.VehicleValueCatalogs.Remove(catalog);

        await context.SaveChangesAsync();
    }
    [Fact]
    public async Task GetActiveByKeyAsync_WhenRecordIsInactive_ShouldReturnNull()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IVehicleValueCatalogRepository>();

        var brandCode =
            $"TEST-BRAND-{Guid.NewGuid():N}";

        var typeCode =
            $"TEST-TYPE-{Guid.NewGuid():N}";

        var catalog =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = typeCode,

                BrandName = "Test Brand",
                TypeName = "Test Type",

                ModelYear = 2024,
                Value = 1_250_000m,

                Source = "IntegrationTest",

                EffectiveDate =
                    DateTime.UtcNow,

                ImportedAt =
                    DateTime.UtcNow,

                IsActive = false,
                IsDeleted = false,

                CreatedDate =
                    DateTime.UtcNow
            };

        context.VehicleValueCatalogs.Add(catalog);

        await context.SaveChangesAsync();

        var result =
            await repository.GetActiveByKeyAsync(
                brandCode,
                typeCode,
                2024);

        Assert.Null(result);

        context.VehicleValueCatalogs.Remove(catalog);

        await context.SaveChangesAsync();
    }
    [Fact]
    public async Task GetActiveByKeyAsync_WhenMultipleActiveRecordsExist_ShouldReturnLatestEffectiveDate()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IVehicleValueCatalogRepository>();

        var brandCode =
            $"TEST-BRAND-{Guid.NewGuid():N}";

        var typeCode =
            $"TEST-TYPE-{Guid.NewGuid():N}";

        var olderDate =
            DateTime.UtcNow.AddDays(-2);

        var newerDate =
            DateTime.UtcNow.AddDays(-1);

        var olderCatalog =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = typeCode,

                BrandName = "Test Brand",
                TypeName = "Test Type",

                ModelYear = 2024,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = olderDate,
                ImportedAt = olderDate,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = olderDate
            };

        var newerCatalog =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = typeCode,

                BrandName = "Test Brand",
                TypeName = "Test Type",

                ModelYear = 2024,
                Value = 1_250_000m,

                Source = "IntegrationTest",

                EffectiveDate = newerDate,
                ImportedAt = newerDate,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = newerDate
            };

        context.VehicleValueCatalogs.AddRange(
            olderCatalog,
            newerCatalog);

        await context.SaveChangesAsync();

        var result =
            await repository.GetActiveByKeyAsync(
                brandCode,
                typeCode,
                2024);

        Assert.NotNull(result);

        Assert.Equal(
            newerCatalog.Id,
            result!.Id);

        Assert.Equal(
            newerDate,
            result.EffectiveDate);

        Assert.Equal(
            1_250_000m,
            result.Value);

        context.VehicleValueCatalogs.RemoveRange(
            olderCatalog,
            newerCatalog);

        await context.SaveChangesAsync();
    }
    [Fact]
    public async Task GetActiveByKeyAsync_WhenMatchingRecordDoesNotExist_ShouldReturnNull()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IVehicleValueCatalogRepository>();

        var result =
            await repository.GetActiveByKeyAsync(
                $"NOT-EXIST-{Guid.NewGuid():N}",
                $"TYPE-{Guid.NewGuid():N}",
                2099);

        Assert.Null(result);
    }
    [Fact]
    public async Task GetActiveBrandsAsync_ShouldReturnOnlyActiveNonDeletedBrands()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IVehicleValueCatalogRepository>();

        var activeBrandCode =
            $"TEST-BRAND-{Guid.NewGuid():N}";

        var activeBrandName =
            $"Active Brand {Guid.NewGuid():N}";

        var inactiveBrandCode =
            $"TEST-INACTIVE-{Guid.NewGuid():N}";

        var deletedBrandCode =
            $"TEST-DELETED-{Guid.NewGuid():N}";

        var activeCatalog =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = activeBrandCode,
                TypeCode = $"TYPE-{Guid.NewGuid():N}",

                BrandName = activeBrandName,
                TypeName = "Test Type",

                ModelYear = 2024,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        var inactiveCatalog =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = inactiveBrandCode,
                TypeCode = $"TYPE-{Guid.NewGuid():N}",

                BrandName = "Inactive Brand",
                TypeName = "Test Type",

                ModelYear = 2024,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = false,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        var deletedCatalog =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = deletedBrandCode,
                TypeCode = $"TYPE-{Guid.NewGuid():N}",

                BrandName = "Deleted Brand",
                TypeName = "Test Type",

                ModelYear = 2024,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = true,

                CreatedDate = DateTime.UtcNow
            };

        context.VehicleValueCatalogs.AddRange(
            activeCatalog,
            inactiveCatalog,
            deletedCatalog);

        await context.SaveChangesAsync();

        var result =
            await repository.GetActiveBrandsAsync();

        var matchingActiveBrand =
            result.SingleOrDefault(x =>
                x.BrandCode == activeBrandCode);

        Assert.NotNull(matchingActiveBrand);

        Assert.Equal(
            activeBrandName,
            matchingActiveBrand!.BrandName);

        Assert.DoesNotContain(
            result,
            x => x.BrandCode == inactiveBrandCode);

        Assert.DoesNotContain(
            result,
            x => x.BrandCode == deletedBrandCode);

        context.VehicleValueCatalogs.RemoveRange(
            activeCatalog,
            inactiveCatalog,
            deletedCatalog);

        await context.SaveChangesAsync();
    }
    [Fact]
    public async Task GetActiveBrandsAsync_ShouldReturnEachBrandOnlyOnce()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IVehicleValueCatalogRepository>();

        var brandCode =
            $"TEST-BRAND-{Guid.NewGuid():N}";

        var brandName =
            $"Test Brand {Guid.NewGuid():N}";

        var catalog1 =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = $"TYPE-1-{Guid.NewGuid():N}",

                BrandName = brandName,
                TypeName = "Test Type 1",

                ModelYear = 2024,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow.AddDays(-1),
                ImportedAt = DateTime.UtcNow.AddDays(-1),

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow.AddDays(-1)
            };

        var catalog2 =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = $"TYPE-2-{Guid.NewGuid():N}",

                BrandName = brandName,
                TypeName = "Test Type 2",

                ModelYear = 2023,
                Value = 900_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        context.VehicleValueCatalogs.AddRange(
            catalog1,
            catalog2);

        await context.SaveChangesAsync();

        var result =
            await repository.GetActiveBrandsAsync();

        var matchingBrands =
            result
                .Where(x => x.BrandCode == brandCode)
                .ToList();

        Assert.Single(matchingBrands);

        Assert.Equal(
            brandCode,
            matchingBrands[0].BrandCode);

        Assert.Equal(
            brandName,
            matchingBrands[0].BrandName);

        context.VehicleValueCatalogs.RemoveRange(
            catalog1,
            catalog2);

        await context.SaveChangesAsync();
    }
    [Fact]
    public async Task GetActiveBrandsAsync_ShouldReturnBrandsOrderedByBrandName()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IVehicleValueCatalogRepository>();

        var firstBrandCode =
            $"TEST-BRAND-{Guid.NewGuid():N}";

        var secondBrandCode =
            $"TEST-BRAND-{Guid.NewGuid():N}";

        var thirdBrandCode =
            $"TEST-BRAND-{Guid.NewGuid():N}";

        var firstBrand =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = firstBrandCode,
                TypeCode = $"TYPE-{Guid.NewGuid():N}",

                BrandName = "ZZZ Test Brand",
                TypeName = "Test Type",

                ModelYear = 2024,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        var secondBrand =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = secondBrandCode,
                TypeCode = $"TYPE-{Guid.NewGuid():N}",

                BrandName = "AAA Test Brand",
                TypeName = "Test Type",

                ModelYear = 2024,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        var thirdBrand =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = thirdBrandCode,
                TypeCode = $"TYPE-{Guid.NewGuid():N}",

                BrandName = "MMM Test Brand",
                TypeName = "Test Type",

                ModelYear = 2024,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        context.VehicleValueCatalogs.AddRange(
            firstBrand,
            secondBrand,
            thirdBrand);

        await context.SaveChangesAsync();

        var result =
            await repository.GetActiveBrandsAsync();

        var matchingBrands =
            result
                .Where(x =>
                    x.BrandCode == firstBrandCode ||
                    x.BrandCode == secondBrandCode ||
                    x.BrandCode == thirdBrandCode)
                .ToList();

        Assert.Equal(3, matchingBrands.Count);

        Assert.Equal(
            "AAA Test Brand",
            matchingBrands[0].BrandName);

        Assert.Equal(
            "MMM Test Brand",
            matchingBrands[1].BrandName);

        Assert.Equal(
            "ZZZ Test Brand",
            matchingBrands[2].BrandName);

        context.VehicleValueCatalogs.RemoveRange(
            firstBrand,
            secondBrand,
            thirdBrand);

        await context.SaveChangesAsync();
    }
    [Fact]
    public async Task GetActiveTypesAsync_ShouldReturnTypesForGivenBrand()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IVehicleValueCatalogRepository>();

        var targetBrandCode =
            $"TEST-BRAND-{Guid.NewGuid():N}";

        var otherBrandCode =
            $"OTHER-BRAND-{Guid.NewGuid():N}";

        var targetType1Code =
            $"TYPE-1-{Guid.NewGuid():N}";

        var targetType2Code =
            $"TYPE-2-{Guid.NewGuid():N}";

        var otherTypeCode =
            $"OTHER-TYPE-{Guid.NewGuid():N}";

        var targetCatalog1 =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = targetBrandCode,
                TypeCode = targetType1Code,

                BrandName = "Test Brand",
                TypeName = "Type One",

                ModelYear = 2024,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        var targetCatalog2 =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = targetBrandCode,
                TypeCode = targetType2Code,

                BrandName = "Test Brand",
                TypeName = "Type Two",

                ModelYear = 2024,
                Value = 1_100_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        var otherBrandCatalog =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = otherBrandCode,
                TypeCode = otherTypeCode,

                BrandName = "Other Brand",
                TypeName = "Other Type",

                ModelYear = 2024,
                Value = 900_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        context.VehicleValueCatalogs.AddRange(
            targetCatalog1,
            targetCatalog2,
            otherBrandCatalog);

        await context.SaveChangesAsync();

        var result =
            await repository.GetActiveTypesAsync(
                targetBrandCode);

        Assert.Equal(2, result.Count);

        Assert.Contains(
            result,
            x => x.TypeCode == targetType1Code);

        Assert.Contains(
            result,
            x => x.TypeCode == targetType2Code);

        Assert.DoesNotContain(
            result,
            x => x.TypeCode == otherTypeCode);

        context.VehicleValueCatalogs.RemoveRange(
            targetCatalog1,
            targetCatalog2,
            otherBrandCatalog);

        await context.SaveChangesAsync();
    }
    [Fact]
    public async Task GetActiveTypesAsync_ShouldReturnOnlyActiveNonDeletedTypes()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IVehicleValueCatalogRepository>();

        var brandCode =
            $"TEST-BRAND-{Guid.NewGuid():N}";

        var activeTypeCode =
            $"ACTIVE-TYPE-{Guid.NewGuid():N}";

        var inactiveTypeCode =
            $"INACTIVE-TYPE-{Guid.NewGuid():N}";

        var deletedTypeCode =
            $"DELETED-TYPE-{Guid.NewGuid():N}";

        var activeCatalog =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = activeTypeCode,

                BrandName = "Test Brand",
                TypeName = "Active Type",

                ModelYear = 2024,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        var inactiveCatalog =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = inactiveTypeCode,

                BrandName = "Test Brand",
                TypeName = "Inactive Type",

                ModelYear = 2024,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = false,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        var deletedCatalog =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = deletedTypeCode,

                BrandName = "Test Brand",
                TypeName = "Deleted Type",

                ModelYear = 2024,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = true,

                CreatedDate = DateTime.UtcNow
            };

        context.VehicleValueCatalogs.AddRange(
            activeCatalog,
            inactiveCatalog,
            deletedCatalog);

        await context.SaveChangesAsync();

        var result =
            await repository.GetActiveTypesAsync(
                brandCode);

        Assert.Single(result);

        Assert.Equal(
            activeTypeCode,
            result[0].TypeCode);

        Assert.Equal(
            "Active Type",
            result[0].TypeName);

        Assert.DoesNotContain(
            result,
            x => x.TypeCode == inactiveTypeCode);

        Assert.DoesNotContain(
            result,
            x => x.TypeCode == deletedTypeCode);

        context.VehicleValueCatalogs.RemoveRange(
            activeCatalog,
            inactiveCatalog,
            deletedCatalog);

        await context.SaveChangesAsync();
    }
    [Fact]
    public async Task GetActiveTypesAsync_ShouldReturnEachTypeOnlyOnce()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IVehicleValueCatalogRepository>();

        var brandCode =
            $"TEST-BRAND-{Guid.NewGuid():N}";

        var typeCode =
            $"TEST-TYPE-{Guid.NewGuid():N}";

        var catalog2024 =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = typeCode,

                BrandName = "Test Brand",
                TypeName = "Test Type",

                ModelYear = 2024,
                Value = 1_250_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow.AddDays(-1),
                ImportedAt = DateTime.UtcNow.AddDays(-1),

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow.AddDays(-1)
            };

        var catalog2023 =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = typeCode,

                BrandName = "Test Brand",
                TypeName = "Test Type",

                ModelYear = 2023,
                Value = 1_100_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        context.VehicleValueCatalogs.AddRange(
            catalog2024,
            catalog2023);

        await context.SaveChangesAsync();

        var result =
            await repository.GetActiveTypesAsync(
                brandCode);

        var matchingTypes =
            result
                .Where(x => x.TypeCode == typeCode)
                .ToList();

        Assert.Single(matchingTypes);

        Assert.Equal(
            typeCode,
            matchingTypes[0].TypeCode);

        Assert.Equal(
            "Test Type",
            matchingTypes[0].TypeName);

        context.VehicleValueCatalogs.RemoveRange(
            catalog2024,
            catalog2023);

        await context.SaveChangesAsync();
    }
    [Fact]
    public async Task GetActiveTypesAsync_ShouldReturnTypesOrderedByTypeName()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IVehicleValueCatalogRepository>();

        var brandCode =
            $"TEST-BRAND-{Guid.NewGuid():N}";

        var catalog1 =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = $"TYPE-Z-{Guid.NewGuid():N}",

                BrandName = "Test Brand",
                TypeName = "ZZZ Test Type",

                ModelYear = 2024,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        var catalog2 =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = $"TYPE-A-{Guid.NewGuid():N}",

                BrandName = "Test Brand",
                TypeName = "AAA Test Type",

                ModelYear = 2024,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        var catalog3 =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = $"TYPE-M-{Guid.NewGuid():N}",

                BrandName = "Test Brand",
                TypeName = "MMM Test Type",

                ModelYear = 2024,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        context.VehicleValueCatalogs.AddRange(
            catalog1,
            catalog2,
            catalog3);

        await context.SaveChangesAsync();

        var result =
            await repository.GetActiveTypesAsync(
                brandCode);

        var matchingTypes =
            result
                .Where(x =>
                    x.TypeCode == catalog1.TypeCode ||
                    x.TypeCode == catalog2.TypeCode ||
                    x.TypeCode == catalog3.TypeCode)
                .ToList();

        Assert.Equal(3, matchingTypes.Count);

        Assert.Equal(
            "AAA Test Type",
            matchingTypes[0].TypeName);

        Assert.Equal(
            "MMM Test Type",
            matchingTypes[1].TypeName);

        Assert.Equal(
            "ZZZ Test Type",
            matchingTypes[2].TypeName);

        context.VehicleValueCatalogs.RemoveRange(
            catalog1,
            catalog2,
            catalog3);

        await context.SaveChangesAsync();
    }
    [Fact]
    public async Task GetActiveYearsAsync_ShouldReturnYearsForGivenBrandAndType()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IVehicleValueCatalogRepository>();

        var brandCode =
            $"TEST-BRAND-{Guid.NewGuid():N}";

        var targetTypeCode =
            $"TARGET-TYPE-{Guid.NewGuid():N}";

        var otherTypeCode =
            $"OTHER-TYPE-{Guid.NewGuid():N}";

        var target2024 =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = targetTypeCode,

                BrandName = "Test Brand",
                TypeName = "Target Type",

                ModelYear = 2024,
                Value = 1_250_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        var target2023 =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = targetTypeCode,

                BrandName = "Test Brand",
                TypeName = "Target Type",

                ModelYear = 2023,
                Value = 1_150_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        var otherType =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = otherTypeCode,

                BrandName = "Test Brand",
                TypeName = "Other Type",

                ModelYear = 2022,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        context.VehicleValueCatalogs.AddRange(
            target2024,
            target2023,
            otherType);

        await context.SaveChangesAsync();

        var result =
            await repository.GetActiveYearsAsync(
                brandCode,
                targetTypeCode);

        Assert.Equal(2, result.Count);

        Assert.Contains(2024, result);
        Assert.Contains(2023, result);

        Assert.DoesNotContain(2022, result);

        context.VehicleValueCatalogs.RemoveRange(
            target2024,
            target2023,
            otherType);

        await context.SaveChangesAsync();
    }
    [Fact]
    public async Task GetActiveYearsAsync_ShouldReturnOnlyActiveNonDeletedYears()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IVehicleValueCatalogRepository>();

        var brandCode =
            $"TEST-BRAND-{Guid.NewGuid():N}";

        var typeCode =
            $"TEST-TYPE-{Guid.NewGuid():N}";

        var activeCatalog =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = typeCode,

                BrandName = "Test Brand",
                TypeName = "Test Type",

                ModelYear = 2024,
                Value = 1_250_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        var inactiveCatalog =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = typeCode,

                BrandName = "Test Brand",
                TypeName = "Test Type",

                ModelYear = 2023,
                Value = 1_150_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = false,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        var deletedCatalog =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = typeCode,

                BrandName = "Test Brand",
                TypeName = "Test Type",

                ModelYear = 2022,
                Value = 1_050_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = true,

                CreatedDate = DateTime.UtcNow
            };

        context.VehicleValueCatalogs.AddRange(
            activeCatalog,
            inactiveCatalog,
            deletedCatalog);

        await context.SaveChangesAsync();

        var result =
            await repository.GetActiveYearsAsync(
                brandCode,
                typeCode);

        Assert.Single(result);

        Assert.Contains(2024, result);

        Assert.DoesNotContain(2023, result);
        Assert.DoesNotContain(2022, result);

        context.VehicleValueCatalogs.RemoveRange(
            activeCatalog,
            inactiveCatalog,
            deletedCatalog);

        await context.SaveChangesAsync();
    }
    [Fact]
    public async Task GetActiveYearsAsync_ShouldReturnYearsOrderedDescending()
    {
        await using var factory =
            new WebApplicationFactory<Program>();

        await using var scope =
            factory.Services.CreateAsyncScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<KaskoContext>();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IVehicleValueCatalogRepository>();

        var brandCode =
            $"TEST-BRAND-{Guid.NewGuid():N}";

        var typeCode =
            $"TEST-TYPE-{Guid.NewGuid():N}";

        var catalog2022 =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = typeCode,

                BrandName = "Test Brand",
                TypeName = "Test Type",

                ModelYear = 2022,
                Value = 1_000_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        var catalog2024 =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = typeCode,

                BrandName = "Test Brand",
                TypeName = "Test Type",

                ModelYear = 2024,
                Value = 1_200_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        var catalog2023 =
            new VehicleValueCatalog
            {
                Id = Guid.NewGuid(),

                BrandCode = brandCode,
                TypeCode = typeCode,

                BrandName = "Test Brand",
                TypeName = "Test Type",

                ModelYear = 2023,
                Value = 1_100_000m,

                Source = "IntegrationTest",

                EffectiveDate = DateTime.UtcNow,
                ImportedAt = DateTime.UtcNow,

                IsActive = true,
                IsDeleted = false,

                CreatedDate = DateTime.UtcNow
            };

        context.VehicleValueCatalogs.AddRange(
            catalog2022,
            catalog2024,
            catalog2023);

        await context.SaveChangesAsync();

        var result =
            await repository.GetActiveYearsAsync(
                brandCode,
                typeCode);

        Assert.Equal(3, result.Count);

        Assert.Equal(2024, result[0]);
        Assert.Equal(2023, result[1]);
        Assert.Equal(2022, result[2]);

        context.VehicleValueCatalogs.RemoveRange(
            catalog2022,
            catalog2024,
            catalog2023);

        await context.SaveChangesAsync();
    }
}