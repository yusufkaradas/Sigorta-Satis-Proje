using System.Globalization;
using Kasko.Business.Integrations.VehicleValue;
using Kasko.Business.Notifications;
using Kasko.DataAccess.Repositories.Abstract;

namespace Kasko.API.BackgroundJobs;

public class CatalogImportWorker : BackgroundService
{
    private static readonly CultureInfo Turkish = new("tr-TR");

    private readonly CatalogImportTracker _tracker;

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ILogger<CatalogImportWorker> _logger;

    public CatalogImportWorker(
        CatalogImportTracker tracker,
        IServiceScopeFactory scopeFactory,
        ILogger<CatalogImportWorker> logger)
    {
        _tracker = tracker;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _tracker.Reader.ReadAllAsync(stoppingToken))
        {
            await RunAsync(job, stoppingToken);
        }
    }

    private async Task RunAsync(CatalogImportJob job, CancellationToken stoppingToken)
    {
        _tracker.MarkRunning(job.Id);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var importer = scope.ServiceProvider.GetRequiredService<IVehicleValueImportService>();

            var result = await importer.ImportAsync(
                job.FilePath,
                job.EffectiveDate,
                _tracker.ProgressFor(job.Id),
                job.RequestedBy,
                stoppingToken);

            _tracker.Complete(job.Id, result);

            await NotifyAsync(
                job,
                "CATALOG_IMPORTED",
                "TSB kasko listesi yüklendi",
                $"{PeriodText(job)} listesinden {result.ImportedCount.ToString("N0", Turkish)} araç değeri kataloğa eklendi; {result.DuplicateCount.ToString("N0", Turkish)} kayıt zaten vardı.");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _tracker.Fail(
                job.Id,
                "Sunucu yeniden başlatıldığı için yükleme tamamlanamadı. Aynı dosyayı tekrar yükleyebilirsiniz; eklenmiş kayıtlar atlanır.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "TSB kasko listesi yüklenemedi. Dosya: {FileName}", job.FileName);

            var message = exception is InvalidOperationException or ArgumentException or FileNotFoundException
                ? exception.Message
                : "Dosya işlenirken beklenmeyen bir hata oluştu. Aynı dosyayı tekrar yükleyebilirsiniz; eklenmiş kayıtlar atlanır.";

            _tracker.Fail(job.Id, message);

            await NotifyAsync(
                job,
                "CATALOG_IMPORT_FAILED",
                "TSB kasko listesi yüklenemedi",
                $"{job.FileName}: {message}");
        }
        finally
        {
            DeleteTempFile(job.FilePath);
        }
    }

    private async Task NotifyAsync(CatalogImportJob job, string type, string title, string message)
    {
        if (job.RequestedByUserId is not Guid userId)
        {
            return;
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await NotificationWriter.ToUserAsync(unitOfWork, userId, type, title, message);

            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Katalog yükleme bildirimi oluşturulamadı.");
        }
    }

    private static string PeriodText(CatalogImportJob job)
    {
        return job.EffectiveDate is DateTime date
            ? $"{date:MM.yyyy} dönemi"
            : "Seçilen dönem";
    }

    private void DeleteTempFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "Geçici katalog dosyası silinemedi: {Path}", path);
        }
    }
}
