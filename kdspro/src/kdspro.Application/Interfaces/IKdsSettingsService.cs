using kdspro.Application.DTOs;

namespace kdspro.Application.Interfaces;

public interface IKdsSettingsService
{
    Task<KdsSettingsDto> GetAsync();
    Task<KdsSettingsDto> UpdateAsync(KdsSettingsDto dto);
}
