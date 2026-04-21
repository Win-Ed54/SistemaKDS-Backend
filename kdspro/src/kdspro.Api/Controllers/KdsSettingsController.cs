using kdspro.Api.Hubs;
using kdspro.Application.DTOs;
using kdspro.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace kdspro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KdsSettingsController : ControllerBase
{
    private readonly IKdsSettingsService _settingsService;
    private readonly IHubContext<OrdersHub> _hub;

    public KdsSettingsController(IKdsSettingsService settingsService, IHubContext<OrdersHub> hub)
    {
        _settingsService = settingsService;
        _hub = hub;
    }

    [Authorize(Roles = "waiter,host,admin,cashier")]
    [HttpGet]
    public async Task<ActionResult<KdsSettingsDto>> Get() =>
        Ok(await _settingsService.GetAsync());

    [Authorize(Roles = "admin")]
    [HttpPut]
    public async Task<ActionResult<KdsSettingsDto>> Update([FromBody] KdsSettingsDto dto)
    {
        var saved = await _settingsService.UpdateAsync(dto);
        await _hub.Clients.Groups("waiter", "host", "admin", "cashier").SendAsync("settingsupdated", saved);
        return Ok(saved);
    }
}
