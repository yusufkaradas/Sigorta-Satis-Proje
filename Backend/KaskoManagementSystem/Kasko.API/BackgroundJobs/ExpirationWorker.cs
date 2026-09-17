using Kasko.DataAccess;
using Kasko.Entities.Concrete;
using Kasko.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kasko.API.BackgroundJobs;

public class ExpirationWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ILogger<ExpirationWorker> _logger;

    public ExpirationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await ExpireAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Süresi dolan kayıtlar güncellenemedi.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExpireAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<KaskoContext>();

        var now = DateTime.UtcNow;

        var policies = await context.Set<Policy>()
            .Where(x => !x.IsDeleted && x.Status == PolicyStatus.Active && x.EndDate < now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, PolicyStatus.Expired)
                .SetProperty(x => x.UpdatedDate, now), cancellationToken);

        var quotes = await context.Set<Quote>()
            .Where(x => !x.IsDeleted &&
                        (x.Status == QuoteStatus.Draft || x.Status == QuoteStatus.Offered) &&
                        x.ValidUntil < now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, QuoteStatus.Expired)
                .SetProperty(x => x.UpdatedDate, now), cancellationToken);

        if (policies + quotes > 0)
        {
            _logger.LogInformation("{Policies} poliçe ve {Quotes} teklif süresi dolduğu için güncellendi.", policies, quotes);
        }
    }
}
