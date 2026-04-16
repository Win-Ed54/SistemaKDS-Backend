using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;
using kdspro.Infrastructure.Persistence;
using MongoDB.Driver;

namespace kdspro.Infrastructure.Repositories;

public class KdsSettingsRepository : IKdsSettingsRepository
{
    private readonly IMongoCollection<KdsSettings> _collection;

    public KdsSettingsRepository(MongoDbContext context)
    {
        _collection = context.KdsSettings;
    }

    public async Task<KdsSettings?> GetAsync() =>
        await _collection.Find(settings => settings.Id == "default").FirstOrDefaultAsync();

    public async Task<KdsSettings> UpsertAsync(KdsSettings settings)
    {
        settings.Id = "default";
        settings.UpdatedAt = DateTime.UtcNow;

        await _collection.ReplaceOneAsync(
            item => item.Id == settings.Id,
            settings,
            new ReplaceOptions { IsUpsert = true }
        );

        return settings;
    }
}
