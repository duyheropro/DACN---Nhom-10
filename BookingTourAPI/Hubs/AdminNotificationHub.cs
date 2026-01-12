// TRONG FILE: Hubs/AdminNotificationHub.cs
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization; // Cần thiết để bảo vệ Hub

namespace BookingTourAPI.Hubs
{
    // Chỉ Admin mới được kết nối vào Hub này
    [Authorize(Roles = "Admin")]
    public class AdminNotificationHub : Hub
    {
        // Hub này hiện tại không cần phương thức nào
        // Client sẽ lắng nghe sự kiện do Server gửi tới
        // Ví dụ: Server sẽ gọi Clients.All.SendAsync("UserLockStatusChanged", ...)
    }
}