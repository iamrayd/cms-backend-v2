using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProjectCms.Models;

namespace ProjectCms.Services
{
    public class PageService
    {
        private readonly IMongoCollection<Page> _pagesCollection;

        public PageService(IOptions<MongoDbSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _pagesCollection = database.GetCollection<Page>(mongoSettings.Value.PagesCollectionName);
        }

        public async Task<List<Page>> GetAsync() =>
            await _pagesCollection.Find(_ => true).ToListAsync();

        public async Task<Page?> GetAsync(string id) =>
            await _pagesCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(Page newPage) =>
            await _pagesCollection.InsertOneAsync(newPage);

        public async Task UpdateAsync(string id, Page updatedPage) =>
            await _pagesCollection.ReplaceOneAsync(x => x.Id == id, updatedPage);

        public async Task RemoveAsync(string id) =>
            await _pagesCollection.DeleteOneAsync(x => x.Id == id);
    }
}