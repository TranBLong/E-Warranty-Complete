using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Enums;

namespace Warranty.Services;

/// <summary>
/// Background service that periodically marks Active warranty cards as Expired
/// when EndDate has passed (Buổi 13 / business rule).
/// </summary>
public sealed class WarrantyExpiryBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<WarrantyExpiryBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WarrantyExpiryBackgroundService started. Interval={Interval}", Interval);

        // Small delay so the app finishes starting
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireCardsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while expiring warranty cards");
            }

            await Task.Delay(Interval, stoppingToken);
        }

        logger.LogInformation("WarrantyExpiryBackgroundService stopped");
    }

    private async Task ExpireCardsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var today = DateTime.UtcNow.Date;
        var expired = await db.WarrantyCards
            .Where(c => c.Status == WarrantyStatus.Active && c.EndDate < today)
            .ToListAsync(cancellationToken);

        if (expired.Count == 0)
        {
            logger.LogDebug("No warranty cards to expire");
            return;
        }

        foreach (var card in expired)
        {
            card.Status = WarrantyStatus.Expired;
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Marked {Count} warranty card(s) as Expired", expired.Count);
    }
}
