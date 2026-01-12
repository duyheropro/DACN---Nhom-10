// TRONG FILE: Hubs/UserNotificationHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

[Authorize] // Chỉ những user đã đăng nhập mới được kết nối
public class UserNotificationHub : Hub
{
    // Phương thức này sẽ tự động chạy mỗi khi có một user kết nối thành công
    public override async Task OnConnectedAsync()
    {
        // Lấy ID của user từ token mà họ gửi lên khi kết nối
        var userId = Context.UserIdentifier; 

        if (!string.IsNullOrEmpty(userId))
        {
            // Cho user này vào một "nhóm" riêng, tên của nhóm chính là ID của họ.
            // Việc này giúp chúng ta có thể gửi tin nhắn chỉ cho user này sau này.
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            Console.WriteLine($"[SignalR] User connected and added to group: {userId}");
        }

        await base.OnConnectedAsync();
    }
}