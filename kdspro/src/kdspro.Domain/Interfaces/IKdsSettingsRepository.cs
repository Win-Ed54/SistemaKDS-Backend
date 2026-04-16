using kdspro.Domain.Entities;

namespace kdspro.Domain.Interfaces;

public interface IKdsSettingsRepository
{
    Task<KdsSettings?> GetAsync();
    Task<KdsSettings> UpsertAsync(KdsSettings settings);
}
