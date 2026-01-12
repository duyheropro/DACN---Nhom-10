// TRONG FILE MỚI: Services/JwtUserIdProvider.cs
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BookingTourAPI.Services
{
    // Giúp SignalR xác định UserId từ 'sub' claim trong JWT
    public class JwtUserIdProvider : IUserIdProvider
    {
        public virtual string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}