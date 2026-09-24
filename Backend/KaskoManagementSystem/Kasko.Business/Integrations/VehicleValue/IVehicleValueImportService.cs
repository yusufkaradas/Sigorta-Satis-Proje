namespace Kasko.Business.Integrations.VehicleValue;

public interface IVehicleValueImportService
{
    Task<VehicleValueImportResult> ImportAsync(
        string filePath,
        DateTime? effectiveDate = null,
        IProgress<VehicleValueImportProgress>? progress = null,
        string? importedBy = null,
        CancellationToken cancellationToken = default);
}