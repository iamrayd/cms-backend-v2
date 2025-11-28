using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProjectCms.Models;

namespace ProjectCms.Services
{
    public class BannerExpiryWorker : BackgroundService
    {
        private readonly IMongoCollection<Banner> _banners;
        private readonly IMongoCollection<Banner> _archivedBanners;

        public BannerExpiryWorker(IOptions<MongoDbSettings> options)
        {
            var client = new MongoClient(options.Value.ConnectionString);
            var database = client.GetDatabase(options.Value.DatabaseName);

            _banners = database.GetCollection<Banner>("Banners");
            _archivedBanners = database.GetCollection<Banner>("ArchivedBanners");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 🔸 CHANGED: use local time instead of UtcNow
                    var now = DateTime.Now;

                    // 🔸 NEW: build filter with AND (expireAt not null AND <= now)
                    var builder = Builders<Banner>.Filter;
                    var filterExpired = builder.And(
                        builder.Ne(b => b.ExpireAt, null),   // expireAt is not null
                        builder.Lte(b => b.ExpireAt, now)    // expireAt <= now
                    );

                    var expired = await _banners
                        .Find(filterExpired)
                        .ToListAsync(stoppingToken);

                    // 🔸 NEW: simple logging para makita kung pila ka expired ang nakit-an
                    Console.WriteLine(
                        $"[BannerExpiryWorker] {DateTime.Now}: Found {expired.Count} expired banners."
                    );

                    if (expired.Count > 0)
                    {
                        // insert to ArchivedBanners
                        await _archivedBanners.InsertManyAsync(
                            expired,
                            cancellationToken: stoppingToken
                        );

                        // delete from Banners
                        var ids = expired.Select(b => b.Id).ToList();
                        var deleteFilter = Builders<Banner>.Filter.In(b => b.Id, ids);
                        await _banners.DeleteManyAsync(deleteFilter, stoppingToken);

                        Console.WriteLine(
                            $"[BannerExpiryWorker] Moved {expired.Count} banners to archive."
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BannerExpiryWorker] Error: {ex.Message}");
                }

                // 🔸 RIGHT NOW: test mode (every 15 seconds)
                await Task.Delay(TimeSpan.FromSeconds(50), stoppingToken);

                // 🔸 PAG PRODUCTION NA: ilisi balik og 1 hour
                // await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}