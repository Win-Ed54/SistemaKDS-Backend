using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace kdspro.Api.Services // O el namespace que prefieras
{
    public class NameIdentifierUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}