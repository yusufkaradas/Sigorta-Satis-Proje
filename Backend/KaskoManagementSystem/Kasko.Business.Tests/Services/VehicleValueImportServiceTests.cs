using Kasko.Business.Integrations.VehicleValue;

namespace Kasko.Business.Tests;

public class VehicleValueImportServiceTests
{
    [Fact]
    public async Task ImportAsync_ShouldReadTsbExcelCorrectly()
    {
        var filePath =
            @"BURAYA_EXCEL_DOSYANIN_GERCEK_YOLU";

        var service =
            new VehicleValueImportService(null!);

        var result =
            await service.ImportAsync(filePath);

        Assert.True(result.ExcelRowCount > 0);

        Assert.True(result.YearValueCount > 0);

        Assert.True(result.ImportedCount > 0);

        Assert.True(result.SkippedZeroValueCount > 0);
    }
}