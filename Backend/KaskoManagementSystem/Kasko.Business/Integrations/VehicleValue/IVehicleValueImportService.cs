namespace Kasko.Business.Integrations.VehicleValue;

public interface IVehicleValueImportService
{
    Task<VehicleValueImportResult> ImportAsync(
        string filePath,
        DateTime? effectiveDate = null,
        CancellationToken cancellationToken = default);
}