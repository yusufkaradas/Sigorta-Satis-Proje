namespace Kasko.Business.Integrations.VehicleValue;

public class VehicleValueImportResult
{
    public int ExcelRowCount { get; set; }

    public int YearValueCount { get; set; }

    public int ImportedCount { get; set; }

    public int SkippedZeroValueCount { get; set; }

    public int DuplicateCount { get; set; }
    public int InvalidRowCount { get; set; }

}