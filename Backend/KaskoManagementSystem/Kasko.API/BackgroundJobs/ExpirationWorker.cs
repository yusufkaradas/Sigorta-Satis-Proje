using Kasko.Business.Notifications;
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

        var notifications = new List<Notification>();

        var expiredPolicies = await context.Set<Policy>()
            .Where(x => !x.IsDeleted && x.Status == PolicyStatus.Active && x.EndDate < now)
            .Select(x => new { x.Id, x.CustomerId, x.PolicyNumber })
            .ToListAsync(cancellationToken);

        if (expiredPolicies.Count > 0)
        {
            var ids = expiredPolicies.Select(x => x.Id).ToList();

            await context.Set<Policy>()
                .Where(x => ids.Contains(x.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, PolicyStatus.Expired)
                    .SetProperty(x => x.UpdatedDate, now), cancellationToken);

            notifications.AddRange(expiredPolicies.Select(x => Create(
                x.CustomerId,
                "POLICY_EXPIRED",
                "Poliçenizin süresi doldu",
                $"{x.PolicyNumber} numaralı poliçenizin süresi doldu. Aracınızın korumasız kalmaması için yeni teklif alabilirsiniz.",
                x.Id,
                now)));
        }

        var expiredQuotes = await context.Set<Quote>()
            .Where(x => !x.IsDeleted &&
                        (x.Status == QuoteStatus.Draft || x.Status == QuoteStatus.Offered) &&
                        x.ValidUntil < now)
            .Select(x => new { x.Id, x.CustomerId, x.QuoteNumber })
            .ToListAsync(cancellationToken);

        if (expiredQuotes.Count > 0)
        {
            var ids = expiredQuotes.Select(x => x.Id).ToList();

            await context.Set<Quote>()
                .Where(x => ids.Contains(x.Id))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, QuoteStatus.Expired)
                    .SetProperty(x => x.UpdatedDate, now), cancellationToken);

            notifications.AddRange(expiredQuotes.Select(x => Create(
                x.CustomerId,
                "QUOTE_EXPIRED",
                "Teklifinizin süresi doldu",
                $"{x.QuoteNumber} numaralı teklifinizin geçerlilik süresi doldu. Güncel fiyatla yeni teklif alabilirsiniz.",
                x.Id,
                now)));
        }

        var renewalLimit = now.AddDays(30);

        var renewals = await context.Set<Policy>()
            .Where(x => !x.IsDeleted &&
                        x.Status == PolicyStatus.Active &&
                        x.EndDate >= now &&
                        x.EndDate <= renewalLimit &&
                        !context.Set<Notification>().Any(n => n.RelatedEntityId == x.Id && n.Type == "POLICY_RENEWAL_DUE"))
            .Select(x => new { x.Id, x.CustomerId, x.PolicyNumber, x.EndDate })
            .ToListAsync(cancellationToken);

        notifications.AddRange(renewals.Select(x => Create(
            x.CustomerId,
            "POLICY_RENEWAL_DUE",
            "Poliçe yenileme zamanı",
            $"{x.PolicyNumber} numaralı poliçeniz {NotificationWriter.FormatDate(x.EndDate)} tarihinde sona eriyor. Poliçe detayından yenileme teklifi alabilirsiniz.",
            x.Id,
            now)));

        var expiringLimit = now.AddDays(1);

        var expiringQuotes = await context.Set<Quote>()
            .Where(x => !x.IsDeleted &&
                        x.Status == QuoteStatus.Offered &&
                        x.ValidUntil >= now &&
                        x.ValidUntil <= expiringLimit &&
                        !context.Set<Notification>().Any(n => n.RelatedEntityId == x.Id && n.Type == "QUOTE_EXPIRING"))
            .Select(x => new { x.Id, x.CustomerId, x.QuoteNumber })
            .ToListAsync(cancellationToken);

        notifications.AddRange(expiringQuotes.Select(x => Create(
            x.CustomerId,
            "QUOTE_EXPIRING",
            "Teklifinizin süresi doluyor",
            $"{x.QuoteNumber} numaralı teklifiniz 24 saat içinde geçersiz olacak. Aynı fiyatla satın almak için Tekliflerim sayfasını ziyaret edin.",
            x.Id,
            now)));

        if (notifications.Count > 0)
        {
            context.Set<Notification>().AddRange(notifications);

            await context.SaveChangesAsync(cancellationToken);
        }

        if (expiredPolicies.Count + expiredQuotes.Count > 0)
        {
            _logger.LogInformation("{Policies} poliçe ve {Quotes} teklif süresi dolduğu için güncellendi.", expiredPolicies.Count, expiredQuotes.Count);
        }
    }

    private static Notification Create(
        Guid customerId,
        string type,
        string title,
        string message,
        Guid relatedEntityId,
        DateTime now)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityId = relatedEntityId,
            IsRead = false,
            IsDeleted = false,
            CreatedDate = now
        };
    }
}
