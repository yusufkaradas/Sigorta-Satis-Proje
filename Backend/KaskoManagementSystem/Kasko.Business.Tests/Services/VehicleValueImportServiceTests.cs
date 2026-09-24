using Kasko.Business.Integrations.VehicleValue;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Moq;

namespace Kasko.Business.Tests;

public class VehicleValueImportServiceTests
{
    [Fact]
    public async Task ImportAsync_ShouldReadTsbExcelCorrectly()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "TestData", "202608R4.xlsx");

        var unitOfWorkMock =
            new Mock<IUnitOfWork>();

        var repositoryMock =
            new Mock<IVehicleValueCatalogRepository>();

        unitOfWorkMock
            .Setup(x => x.VehicleValueCatalogs)
            .Returns(repositoryMock.Object);

        repositoryMock
            .Setup(x => x.GetKeysByEffectiveDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(Array.Empty<VehicleValueCatalog>());

        unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var service =
            new VehicleValueImportService(
                unitOfWorkMock.Object);

        var result =
            await service.ImportAsync(filePath);

        Assert.True(result.ExcelRowCount > 0);
        Assert.True(result.YearValueCount > 0);
        Assert.True(result.ImportedCount > 0);
        Assert.True(result.SkippedZeroValueCount > 0);
    }

    [Fact]
    public async Task ImportAsync_ShouldReportProgressUntilSaving()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "TestData", "202608R4.xlsx");

        var unitOfWorkMock =
            new Mock<IUnitOfWork>();

        var repositoryMock =
            new Mock<IVehicleValueCatalogRepository>();

        unitOfWorkMock
            .Setup(x => x.VehicleValueCatalogs)
            .Returns(repositoryMock.Object);

        repositoryMock
            .Setup(x => x.GetKeysByEffectiveDateAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(Array.Empty<VehicleValueCatalog>());

        unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var reports = new List<VehicleValueImportProgress>();

        var service =
            new VehicleValueImportService(
                unitOfWorkMock.Object);

        var result =
            await service.ImportAsync(
                filePath,
                new DateTime(2026, 9, 1),
                new SynchronousProgress(reports.Add),
                "admin@test.com");

        Assert.Equal(VehicleValueImportStage.Reading, reports.First().Stage);
        Assert.Equal(VehicleValueImportStage.Saving, reports.Last().Stage);
        Assert.Contains(reports, x => x.Stage == VehicleValueImportStage.Processing && x.ProcessedRows > 0);
        Assert.Equal(result.ExcelRowCount, reports.Last().TotalRows);

        var processed = reports
            .Where(x => x.Stage == VehicleValueImportStage.Processing)
            .Select(x => x.ProcessedRows)
            .ToList();

        Assert.Equal(processed.OrderBy(x => x), processed);

        repositoryMock.Verify(
            x => x.GetKeysByEffectiveDateAsync(new DateTime(2026, 9, 1)),
            Times.Once);

        repositoryMock.Verify(
            x => x.AddAsync(It.Is<VehicleValueCatalog>(entity => entity.CreatedBy == "admin@test.com")),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ImportAsync_ShouldSkipKeysAlreadyImportedForSamePeriod()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "TestData", "202608R4.xlsx");

        var period = new DateTime(2026, 9, 1);

        var firstRun = await RunImportAsync(filePath, period, Array.Empty<VehicleValueCatalog>());

        var added = firstRun.Added
            .Select(x => new VehicleValueCatalog
            {
                BrandCode = x.BrandCode,
                TypeCode = x.TypeCode,
                ModelYear = x.ModelYear,
                EffectiveDate = x.EffectiveDate
            })
            .ToList();

        var secondRun = await RunImportAsync(filePath, period, added);

        Assert.Equal(0, secondRun.Result.ImportedCount);
        Assert.Equal(firstRun.Result.ImportedCount, secondRun.Result.DuplicateCount);
    }

    private static async Task<(VehicleValueImportResult Result, List<VehicleValueCatalog> Added)> RunImportAsync(
        string filePath,
        DateTime period,
        IReadOnlyList<VehicleValueCatalog> existing)
    {
        var added = new List<VehicleValueCatalog>();

        var unitOfWorkMock =
            new Mock<IUnitOfWork>();

        var repositoryMock =
            new Mock<IVehicleValueCatalogRepository>();

        unitOfWorkMock
            .Setup(x => x.VehicleValueCatalogs)
            .Returns(repositoryMock.Object);

        repositoryMock
            .Setup(x => x.GetKeysByEffectiveDateAsync(period))
            .ReturnsAsync(existing);

        repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<VehicleValueCatalog>()))
            .Callback<VehicleValueCatalog>(added.Add)
            .Returns(Task.CompletedTask);

        unitOfWorkMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await new VehicleValueImportService(unitOfWorkMock.Object)
            .ImportAsync(filePath, period);

        return (result, added);
    }

    private sealed class SynchronousProgress : IProgress<VehicleValueImportProgress>
    {
        private readonly Action<VehicleValueImportProgress> _report;

        public SynchronousProgress(Action<VehicleValueImportProgress> report)
        {
            _report = report;
        }

        public void Report(VehicleValueImportProgress value)
        {
            _report(value);
        }
    }
}