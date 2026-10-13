using CityGuide.Web.Data;
using CityGuide.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace CityGuide.Web.Services;

/// <summary>Hourly job that hides property listings once their 30-day period ends.</summary>
public class ExpiryService(IServiceProvider services, ILogger<ExpiryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTime.UtcNow;
                var due = await db.Properties
                    .Where(p => p.Status == ListingStatus.Published && p.ExpiresAt != null && p.ExpiresAt <= now)
                    .ToListAsync(stoppingToken);
                if (ListingRules.ExpireDue(due, now) > 0)
                {
                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Expired {Count} property listings", due.Count);
                }
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "Expiry job failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
