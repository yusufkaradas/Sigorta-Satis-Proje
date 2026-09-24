using System.Threading.Channels;

namespace Kasko.Business.Integrations.VehicleValue;

public static class CatalogImportState
{
    public const string Idle = "Idle";

    public const string Queued = "Queued";

    public const string Running = "Running";

    public const string Completed = "Completed";

    public const string Failed = "Failed";
}

public class CatalogImportJob
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string FilePath { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public DateTime? EffectiveDate { get; init; }

    public Guid? RequestedByUserId { get; init; }

    public string? RequestedBy { get; init; }
}

public class CatalogImportStatus
{
    public Guid? JobId { get; set; }

    public string State { get; set; } = CatalogImportState.Idle;

    public string? Stage { get; set; }

    public int Percent { get; set; }

    public int ProcessedRows { get; set; }

    public int TotalRows { get; set; }

    public string? FileName { get; set; }

    public DateTime? EffectiveDate { get; set; }

    public DateTime? QueuedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public VehicleValueImportResult? Result { get; set; }

    public string? Error { get; set; }

    public bool IsActive =>
        State == CatalogImportState.Queued ||
        State == CatalogImportState.Running;

    public CatalogImportStatus Copy()
    {
        return (CatalogImportStatus)MemberwiseClone();
    }
}

public class CatalogImportTracker
{
    private readonly object _sync = new();

    private readonly Channel<CatalogImportJob> _queue =
        Channel.CreateUnbounded<CatalogImportJob>(
            new UnboundedChannelOptions { SingleReader = true });

    private CatalogImportStatus _status = new();

    public ChannelReader<CatalogImportJob> Reader => _queue.Reader;

    public bool IsBusy
    {
        get
        {
            lock (_sync)
            {
                return _status.IsActive;
            }
        }
    }

    public CatalogImportStatus GetStatus()
    {
        lock (_sync)
        {
            return _status.Copy();
        }
    }

    public bool TryEnqueue(CatalogImportJob job)
    {
        lock (_sync)
        {
            if (_status.IsActive)
            {
                return false;
            }

            _status = new CatalogImportStatus
            {
                JobId = job.Id,
                State = CatalogImportState.Queued,
                Stage = VehicleValueImportStage.Reading,
                FileName = job.FileName,
                EffectiveDate = job.EffectiveDate,
                QueuedAt = DateTime.UtcNow
            };

            return _queue.Writer.TryWrite(job);
        }
    }

    public void MarkRunning(Guid jobId)
    {
        Update(jobId, status =>
        {
            status.State = CatalogImportState.Running;
            status.Stage = VehicleValueImportStage.Reading;
            status.Percent = CalculatePercent(new VehicleValueImportProgress(VehicleValueImportStage.Reading, 0, 0));
            status.StartedAt = DateTime.UtcNow;
        });
    }

    public void Report(Guid jobId, VehicleValueImportProgress progress)
    {
        Update(jobId, status =>
        {
            status.Stage = progress.Stage;
            status.ProcessedRows = progress.ProcessedRows;
            status.TotalRows = progress.TotalRows;
            status.Percent = Math.Max(status.Percent, CalculatePercent(progress));
        });
    }

    public void Complete(Guid jobId, VehicleValueImportResult result)
    {
        Update(jobId, status =>
        {
            status.State = CatalogImportState.Completed;
            status.Stage = null;
            status.Percent = 100;
            status.ProcessedRows = status.TotalRows;
            status.Result = result;
            status.FinishedAt = DateTime.UtcNow;
        });
    }

    public void Fail(Guid jobId, string error)
    {
        Update(jobId, status =>
        {
            status.State = CatalogImportState.Failed;
            status.Stage = null;
            status.Error = error;
            status.FinishedAt = DateTime.UtcNow;
        });
    }

    public IProgress<VehicleValueImportProgress> ProgressFor(Guid jobId)
    {
        return new TrackerProgress(this, jobId);
    }

    public static int CalculatePercent(VehicleValueImportProgress progress)
    {
        return progress.Stage switch
        {
            VehicleValueImportStage.Reading => 2,
            VehicleValueImportStage.Processing when progress.TotalRows > 0 =>
                5 + (int)(90L * Math.Min(progress.ProcessedRows, progress.TotalRows) / progress.TotalRows),
            VehicleValueImportStage.Processing => 5,
            VehicleValueImportStage.Saving => 97,
            _ => 0
        };
    }

    private void Update(Guid jobId, Action<CatalogImportStatus> apply)
    {
        lock (_sync)
        {
            if (_status.JobId != jobId)
            {
                return;
            }

            apply(_status);
        }
    }

    private sealed class TrackerProgress : IProgress<VehicleValueImportProgress>
    {
        private readonly CatalogImportTracker _tracker;

        private readonly Guid _jobId;

        public TrackerProgress(CatalogImportTracker tracker, Guid jobId)
        {
            _tracker = tracker;
            _jobId = jobId;
        }

        public void Report(VehicleValueImportProgress value)
        {
            _tracker.Report(_jobId, value);
        }
    }
}
