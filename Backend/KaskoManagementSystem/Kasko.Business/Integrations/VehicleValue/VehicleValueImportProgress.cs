namespace Kasko.Business.Integrations.VehicleValue;

public static class VehicleValueImportStage
{
    public const string Reading = "Reading";

    public const string Processing = "Processing";

    public const string Saving = "Saving";
}

public record VehicleValueImportProgress(
    string Stage,
    int ProcessedRows,
    int TotalRows);
