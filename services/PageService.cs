using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProjectCms.Models;

namespace ProjectCms.Services
{
    public class PageService
    {
        private readonly IMongoCollection<Page> _pagesCollection;
        private readonly ILogger<PageService> _logger;

        public PageService(
            IOptions<MongoDbSettings> mongoSettings,
            ILogger<PageService> logger)
        {
            _logger = logger;

            try
            {
                var client = new MongoClient(mongoSettings.Value.ConnectionString);
                var database = client.GetDatabase(mongoSettings.Value.DatabaseName);

                // ⭐ OPTION 1: Hardcode collection name (recommended for consistency)
                _pagesCollection = database.GetCollection<Page>("Pages");

                // ⭐ OPTION 2: Use configuration (if you prefer)
                // Uncomment below and comment above if you want config-based approach
                /*
                if (string.IsNullOrWhiteSpace(mongoSettings.Value.PagesCollectionName))
                {
                    throw new InvalidOperationException(
                        "PagesCollectionName is not configured in appsettings.json");
                }
                _pagesCollection = database.GetCollection<Page>(
                    mongoSettings.Value.PagesCollectionName);
                */

                _logger.LogInformation(
                    "PageService initialized - Database: {Database}, Collection: Pages",
                    mongoSettings.Value.DatabaseName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize PageService");
                throw;
            }
        }

        public async Task<List<Page>> GetAsync()
        {
            try
            {
                _logger.LogDebug("Fetching all pages");
                var pages = await _pagesCollection.Find(_ => true).ToListAsync();
                _logger.LogInformation("Retrieved {Count} pages", pages.Count);
                return pages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all pages");
                throw;
            }
        }

        public async Task<Page?> GetAsync(string id)
        {
            try
            {
                _logger.LogDebug("Fetching page with ID: {PageId}", id);
                var page = await _pagesCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

                if (page == null)
                {
                    _logger.LogWarning("Page not found with ID: {PageId}", id);
                }
                else
                {
                    _logger.LogDebug("Page found: {PageTitle}", page.Title);
                }

                return page;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching page with ID: {PageId}", id);
                throw;
            }
        }

        public async Task CreateAsync(Page newPage)
        {
            try
            {
                _logger.LogInformation("Creating new page: {PageTitle}", newPage.Title);

                // Ensure timestamps
                newPage.CreatedAt = DateTime.UtcNow;
                newPage.UpdatedAt = DateTime.UtcNow;

                await _pagesCollection.InsertOneAsync(newPage);
                _logger.LogInformation(
                    "Page created successfully - ID: {PageId}, Title: {PageTitle}",
                    newPage.Id,
                    newPage.Title
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating page: {PageTitle}", newPage.Title);
                throw;
            }
        }

        public async Task UpdateAsync(string id, Page updatedPage)
        {
            try
            {
                _logger.LogInformation("Updating page with ID: {PageId}", id);

                // Update timestamp
                updatedPage.UpdatedAt = DateTime.UtcNow;

                var result = await _pagesCollection.ReplaceOneAsync(x => x.Id == id, updatedPage);

                if (result.ModifiedCount > 0)
                {
                    _logger.LogInformation("Page updated successfully: {PageId}", id);
                }
                else
                {
                    _logger.LogWarning("No page was updated with ID: {PageId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating page with ID: {PageId}", id);
                throw;
            }
        }

        public async Task RemoveAsync(string id)
        {
            try
            {
                _logger.LogInformation("Deleting page with ID: {PageId}", id);
                var result = await _pagesCollection.DeleteOneAsync(x => x.Id == id);

                if (result.DeletedCount > 0)
                {
                    _logger.LogInformation("Page deleted successfully: {PageId}", id);
                }
                else
                {
                    _logger.LogWarning("No page was deleted with ID: {PageId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting page with ID: {PageId}", id);
                throw;
            }
        }
    }
}