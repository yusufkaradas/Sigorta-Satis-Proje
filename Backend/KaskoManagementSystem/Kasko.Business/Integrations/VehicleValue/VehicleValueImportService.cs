using ClosedXML.Excel;
using Kasko.Business.Services;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.Business.Integrations.VehicleValue;

public class VehicleValueImportService
    : IVehicleValueImportService
{
    private readonly IUnitOfWork _unitOfWork;
    private static string BuildKey(
    string brandCode,
    string typeCode,
    int modelYear,
    DateTime effectiveDate)
    {
        return $"{brandCode}|{typeCode}|{modelYear}|{effectiveDate:yyyy-MM-dd}";
    }

    public VehicleValueImportService(
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<VehicleValueImportResult> ImportAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "Excel dosya yolu boş olamaz.",
                nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Excel dosyası bulunamadı.",
                filePath);
        }

        var result = new VehicleValueImportResult();

        using var workbook = new XLWorkbook(filePath);
        
        var effectiveDate =
    
            new DateTime(2026, 8, 1);

        var importedAt =
            DateTime.UtcNow;

        var existingRecords =
            await _unitOfWork
                .VehicleValueCatalogs
                .GetAllAsync();

        var existingKeys =
            existingRecords
                .Where(x => !x.IsDeleted)
                .Select(x => BuildKey(
                    x.BrandCode,
                    x.TypeCode,
                    x.ModelYear,
                    x.EffectiveDate))
                .ToHashSet();
        var worksheet = workbook.Worksheets
            .FirstOrDefault(x =>
                x.Name.Equals(
                    "Smarka",
                    StringComparison.OrdinalIgnoreCase));

        if (worksheet == null)
        {
            throw new InvalidOperationException(
                "Excel içerisinde 'Smarka' sayfası bulunamadı.");
        }

        var usedRange = worksheet.RangeUsed();

        if (usedRange == null)
        {
            throw new InvalidOperationException(
                "Excel sayfasında veri bulunamadı.");
        }

        const int headerRow = 2;
        const int firstDataRow = 3;

        var lastRow =
            usedRange.LastRow().RowNumber();

        var lastColumn =
            usedRange.LastColumn().ColumnNumber();

        for (var rowNumber = firstDataRow;
             rowNumber <= lastRow;
             rowNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            result.ExcelRowCount++;

            var brandCode =
                worksheet.Cell(rowNumber, 1)
                    .GetString()
                    .Trim();

            var typeCode =
                worksheet.Cell(rowNumber, 2)
                    .GetString()
                    .Trim();

            var brandName =
                worksheet.Cell(rowNumber, 3)
                    .GetString()
                    .Trim();

            var typeName =
                worksheet.Cell(rowNumber, 4)
                    .GetString()
                    .Trim();

            if (string.IsNullOrWhiteSpace(brandCode) ||
                string.IsNullOrWhiteSpace(typeCode) ||
                string.IsNullOrWhiteSpace(brandName) ||
                string.IsNullOrWhiteSpace(typeName))
            {
                result.InvalidRowCount++;
                continue;
            }

            for (var columnNumber = 5;
                 columnNumber <= lastColumn;
                 columnNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var yearText =
                    worksheet.Cell(headerRow, columnNumber)
                        .GetString()
                        .Trim();

                if (!int.TryParse(
                        yearText,
                        out var modelYear))
                {
                    continue;
                }

                result.YearValueCount++;

                var valueCell =
                    worksheet.Cell(
                        rowNumber,
                        columnNumber);

                if (!valueCell.TryGetValue<decimal>(
                        out var value))
                {
                    result.InvalidRowCount++;
                    continue;
                }

                if (value <= 0)
                {
                    result.SkippedZeroValueCount++;
                    continue;
                }
                var key =
    
                    BuildKey(
        
                        brandCode,
       
                        typeCode,
        
                        modelYear,
        
                        effectiveDate);

                if (existingKeys.Contains(key))
                {
                    result.DuplicateCount++;
                    continue;
                }

                var entity =
                    new VehicleValueCatalog
                    {
                        Id = Guid.NewGuid(),

                        BrandCode = brandCode,

                        TypeCode = typeCode,

                        BrandName = brandName,

                        TypeName = typeName,

                        ModelYear = modelYear,

                        Value = value,

                        Source = "TSB",

                        EffectiveDate =
                            effectiveDate,

                        ImportedAt =
                            importedAt,

                        IsActive = true,

                        IsDeleted = false,

                        CreatedDate =
                            importedAt,

                        VehicleCategory =
                                          VehicleCategoryClassifier.Classify(brandName, typeName),
                    };

                await _unitOfWork
                    .VehicleValueCatalogs
                    .AddAsync(entity);

                existingKeys.Add(key);

                result.ImportedCount++;
            }
        }

        await _unitOfWork.SaveChangesAsync();

        return result;
    }
}