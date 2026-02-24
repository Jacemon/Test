using LinkShorter.Data;
using Microsoft.EntityFrameworkCore;

namespace LinkShorter.Services;

public class ClickDatabaseWriter(
    ClickBuffer buffer, 
    IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            var clicks = buffer.Flush();
            if (!clicks.Any()) continue;

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var codes = clicks.Keys.ToList();
            var urlsToUpdate = await db.Urls
                .Where(u => codes.Contains(u.ShortCode))
                .ToListAsync(stoppingToken);
            
            foreach (var url in urlsToUpdate)
            {
                if (clicks.TryGetValue(url.ShortCode, out int newClicks))
                {
                    url.ClickCount += newClicks;
                }
            }
            
            await db.SaveChangesAsync(stoppingToken);
        }
    }
}
