using Kasko.Business.Integrations.VehicleValue;

namespace Kasko.Business.Tests;

public class CatalogImportTrackerTests
{
    [Fact]
    public void TryEnqueue_WhenImportIsActive_ShouldRejectSecondJob()
    {
        var tracker = new CatalogImportTracker();

        var first = new CatalogImportJob { FileName = "202609.xlsx" };
        var second = new CatalogImportJob { FileName = "202610.xlsx" };

        Assert.True(tracker.TryEnqueue(first));
        Assert.True(tracker.IsBusy);
        Assert.False(tracker.TryEnqueue(second));

        var status = tracker.GetStatus();

        Assert.Equal(first.Id, status.JobId);
        Assert.Equal(CatalogImportState.Queued, status.State);
        Assert.True(tracker.Reader.TryRead(out var queued));
        Assert.Equal(first.Id, queued!.Id);
        Assert.False(tracker.Reader.TryRead(out _));
    }

    [Fact]
    public void TryEnqueue_AfterCompletion_ShouldAcceptNewJob()
    {
        var tracker = new CatalogImportTracker();

        var first = new CatalogImportJob();

        tracker.TryEnqueue(first);
        tracker.MarkRunning(first.Id);
        tracker.Complete(first.Id, new VehicleValueImportResult { ImportedCount = 10 });

        Assert.False(tracker.IsBusy);
        Assert.True(tracker.TryEnqueue(new CatalogImportJob()));
    }

    [Fact]
    public void Report_ShouldIncreasePercentAndNeverGoBack()
    {
        var tracker = new CatalogImportTracker();

        var job = new CatalogImportJob();

        tracker.TryEnqueue(job);
        tracker.MarkRunning(job.Id);

        tracker.Report(job.Id, new VehicleValueImportProgress(VehicleValueImportStage.Processing, 50, 100));

        var halfway = tracker.GetStatus();

        Assert.Equal(CatalogImportState.Running, halfway.State);
        Assert.Equal(50, halfway.ProcessedRows);
        Assert.Equal(50, halfway.Percent);

        tracker.Report(job.Id, new VehicleValueImportProgress(VehicleValueImportStage.Processing, 10, 100));

        Assert.Equal(50, tracker.GetStatus().Percent);

        tracker.Report(job.Id, new VehicleValueImportProgress(VehicleValueImportStage.Saving, 100, 100));

        Assert.Equal(97, tracker.GetStatus().Percent);

        tracker.Complete(job.Id, new VehicleValueImportResult());

        var done = tracker.GetStatus();

        Assert.Equal(CatalogImportState.Completed, done.State);
        Assert.Equal(100, done.Percent);
        Assert.NotNull(done.FinishedAt);
    }

    [Fact]
    public void Fail_ShouldKeepErrorAndReleaseQueue()
    {
        var tracker = new CatalogImportTracker();

        var job = new CatalogImportJob();

        tracker.TryEnqueue(job);
        tracker.MarkRunning(job.Id);
        tracker.Fail(job.Id, "Excel içerisinde 'Smarka' sayfası bulunamadı.");

        var status = tracker.GetStatus();

        Assert.Equal(CatalogImportState.Failed, status.State);
        Assert.Equal("Excel içerisinde 'Smarka' sayfası bulunamadı.", status.Error);
        Assert.False(tracker.IsBusy);
    }

    [Fact]
    public void Updates_ForAnotherJob_ShouldBeIgnored()
    {
        var tracker = new CatalogImportTracker();

        var job = new CatalogImportJob();

        tracker.TryEnqueue(job);

        tracker.Complete(Guid.NewGuid(), new VehicleValueImportResult());

        Assert.Equal(CatalogImportState.Queued, tracker.GetStatus().State);
    }

    [Fact]
    public void GetStatus_ShouldReturnCopy()
    {
        var tracker = new CatalogImportTracker();

        var job = new CatalogImportJob();

        tracker.TryEnqueue(job);

        var snapshot = tracker.GetStatus();

        snapshot.State = CatalogImportState.Failed;

        Assert.Equal(CatalogImportState.Queued, tracker.GetStatus().State);
    }

    [Theory]
    [InlineData(VehicleValueImportStage.Reading, 0, 0, 2)]
    [InlineData(VehicleValueImportStage.Processing, 0, 0, 5)]
    [InlineData(VehicleValueImportStage.Processing, 0, 200, 5)]
    [InlineData(VehicleValueImportStage.Processing, 200, 200, 95)]
    [InlineData(VehicleValueImportStage.Processing, 500, 200, 95)]
    [InlineData(VehicleValueImportStage.Saving, 200, 200, 97)]
    public void CalculatePercent_ShouldMapStages(string stage, int processed, int total, int expected)
    {
        Assert.Equal(
            expected,
            CatalogImportTracker.CalculatePercent(new VehicleValueImportProgress(stage, processed, total)));
    }
}
