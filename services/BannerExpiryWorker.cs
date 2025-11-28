using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProjectCms.Models;

namespace ProjectCms.Services
{
    public class BannerExpiryWorker : BackgroundService
    {
        private readonly IMongoCollection<Banner> _banners;
        private readonly IMongoCollection<ArchivedBanner> _archivedBanners;  // ⭐ FIXED: Correct type
        private readonly ILogger<BannerExpiryWorker> _logger;

        public BannerExpiryWorker(
            IOptions<MongoDbSettings> options,
            ILogger<BannerExpiryWorker> logger)
        {
            _logger = logger;
            var client = new MongoClient(options.Value.ConnectionString);
            var database = client.GetDatabase(options.Value.DatabaseName);

            _banners = database.GetCollection<Banner>("Banners");
            _archivedBanners = database.GetCollection<ArchivedBanner>("ArchivedBanners");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BannerExpiryWorker started at: {Time}", DateTime.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredBannersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing expired banners");
                }

                // ⚠️ PRODUCTION: Change to 1 hour
                // await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

                // 🔧 DEVELOPMENT: Check every 50 seconds
                await Task.Delay(TimeSpan.FromSeconds(50), stoppingToken);
            }

            _logger.LogInformation("BannerExpiryWorker stopped at: {Time}", DateTime.Now);
        }

        private async Task ProcessExpiredBannersAsync(CancellationToken cancellationToken)
        {
            var now = DateTime.Now;

            // Build filter: expireAt is not null AND expireAt <= now
            var builder = Builders<Banner>.Filter;
            var filterExpired = builder.And(
                builder.Ne(b => b.ExpireAt, null),
                builder.Lte(b => b.ExpireAt, now)
            );

            var expired = await _banners
                .Find(filterExpired)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "[BannerExpiryWorker] {Time}: Found {Count} expired banners",
                DateTime.Now,
                expired.Count
            );

            if (expired.Count > 0)
            {
                // ⭐ FIXED: Map Banner to ArchivedBanner
                var archivedItems = expired.Select(banner => new ArchivedBanner
                {
                    Title = banner.Title,
                    ImageUrl = banner.ImageUrl,
                    Status = banner.Status,
                    Link = banner.Link ?? string.Empty,
                    PublishAt = banner.PublishAt ?? DateTime.UtcNow,
                    ExpireAt = banner.ExpireAt ?? DateTime.UtcNow,
                    Content = banner.Content
                }).ToList();

                try
                {
                    // Insert to ArchivedBanners
                    await _archivedBanners.InsertManyAsync(
                        archivedItems,
                        cancellationToken: cancellationToken
                    );

                    // Delete from Banners
                    var ids = expired.Select(b => b.Id).ToList();
                    var deleteFilter = Builders<Banner>.Filter.In(b => b.Id, ids);
                    var deleteResult = await _banners.DeleteManyAsync(deleteFilter, cancellationToken);

                    _logger.LogInformation(
                        "[BannerExpiryWorker] Successfully moved {Count} banners to archive. Deleted: {DeletedCount}",
                        expired.Count,
                        deleteResult.DeletedCount
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error archiving expired banners");
                    throw;
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("BannerExpiryWorker is stopping");
            return base.StopAsync(cancellationToken);
        }
    }
}