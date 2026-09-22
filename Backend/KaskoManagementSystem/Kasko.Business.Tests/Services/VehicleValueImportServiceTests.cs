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
            .Setup(x => x.GetAllAsync())
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
}