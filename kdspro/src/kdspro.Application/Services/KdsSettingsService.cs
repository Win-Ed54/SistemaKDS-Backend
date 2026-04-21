using kdspro.Application.DTOs;
using kdspro.Application.Interfaces;
using kdspro.Domain.Entities;
using kdspro.Domain.Interfaces;

namespace kdspro.Application.Services;

public class KdsSettingsService : IKdsSettingsService
{
    private readonly IKdsSettingsRepository _repository;

    public KdsSettingsService(IKdsSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<KdsSettingsDto> GetAsync()
    {
        var settings = await _repository.GetAsync();
        return MapToDto(Resolve(settings));
    }

    public async Task<KdsSettingsDto> UpdateAsync(KdsSettingsDto dto)
    {
        var normalizedMode = OrderValidationRules.NormalizeMode(dto.ServiceMode);
        var defaults = OrderValidationRules.GetDefaults(normalizedMode);

        var settings = new KdsSettings
        {
            Id = "default",
            ServiceMode = normalizedMode,
            MaxDistinctItems = NormalizePositive(dto.MaxDistinctItems, defaults.MaxDistinctItems),
            MaxTotalUnits = NormalizePositive(dto.MaxTotalUnits, defaults.MaxTotalUnits),
            MaxQuantityPerProduct = NormalizePositive(dto.MaxQuantityPerProduct, defaults.MaxQuantityPerProduct),
            LargeOrderUnitsWarning = NormalizePositive(dto.LargeOrderUnitsWarning, defaults.LargeOrderUnitsWarning),
            TakeoutRequirePrepayment = dto.TakeoutRequirePrepayment,
            RequireCustomerNameForTakeout = dto.RequireCustomerNameForTakeout,
            DefaultCleaningMinutes = NormalizePositive(dto.DefaultCleaningMinutes, 8),
            MaxPartySize = NormalizePositive(dto.MaxPartySize, 10),
            UpdatedAt = DateTime.UtcNow,
        };

        settings = Resolve(settings);
        var saved = await _repository.UpsertAsync(settings);
        return MapToDto(saved);
    }

    private static int NormalizePositive(int value, int fallback) => value > 0 ? value : fallback;

    private static KdsSettings Resolve(KdsSettings? settings)
    {
        var mode = OrderValidationRules.NormalizeMode(settings?.ServiceMode);
        var defaults = OrderValidationRules.GetDefaults(mode);

        return new KdsSettings
        {
            Id = settings?.Id ?? "default",
            ServiceMode = mode,
            MaxDistinctItems = settings?.MaxDistinctItems > 0 ? settings.MaxDistinctItems : defaults.MaxDistinctItems,
            MaxTotalUnits = settings?.MaxTotalUnits > 0 ? settings.MaxTotalUnits : defaults.MaxTotalUnits,
            MaxQuantityPerProduct = settings?.MaxQuantityPerProduct > 0 ? settings.MaxQuantityPerProduct : defaults.MaxQuantityPerProduct,
            LargeOrderUnitsWarning = settings?.LargeOrderUnitsWarning > 0 ? settings.LargeOrderUnitsWarning : defaults.LargeOrderUnitsWarning,
            TakeoutRequirePrepayment = settings?.TakeoutRequirePrepayment ?? false,
            RequireCustomerNameForTakeout = settings?.RequireCustomerNameForTakeout ?? true,
            DefaultCleaningMinutes = settings?.DefaultCleaningMinutes > 0 ? settings.DefaultCleaningMinutes : 8,
            MaxPartySize = settings?.MaxPartySize > 0 ? settings.MaxPartySize : 10,
            UpdatedAt = settings?.UpdatedAt ?? DateTime.UtcNow,
        };
    }

    private static KdsSettingsDto MapToDto(KdsSettings settings) => new()
    {
        ServiceMode = settings.ServiceMode,
        MaxDistinctItems = settings.MaxDistinctItems,
        MaxTotalUnits = settings.MaxTotalUnits,
        MaxQuantityPerProduct = settings.MaxQuantityPerProduct,
        LargeOrderUnitsWarning = settings.LargeOrderUnitsWarning,
        TakeoutRequirePrepayment = settings.TakeoutRequirePrepayment,
        RequireCustomerNameForTakeout = settings.RequireCustomerNameForTakeout,
        DefaultCleaningMinutes = settings.DefaultCleaningMinutes,
        MaxPartySize = settings.MaxPartySize,
    };
}
