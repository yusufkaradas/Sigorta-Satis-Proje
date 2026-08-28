namespace Kasko.Business.Integrations.VehicleValue;

public interface IVehicleValueImportService
{
    Task<VehicleValueImportResult> ImportAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}