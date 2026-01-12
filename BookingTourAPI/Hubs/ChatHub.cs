using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System;

namespace BookingTourAPI.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        // Lưu danh sách Admin đang online
        private static readonly ConcurrentDictionary<string, string> OnlineAdmins = new ConcurrentDictionary<string, string>();

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var userName = Context.User?.Identity?.Name;
            
            // Debug Log: Xem ai đang kết nối
            Console.WriteLine($"[SignalR] Connected: {userName} ({userId}) - ID: {Context.ConnectionId}");

            // Cách kiểm tra Role chuẩn nhất trong SignalR
            if (Context.User != null && Context.User.IsInRole("Admin"))
            {
                Console.WriteLine($"[SignalR] --> User {userName} is ADMIN. Joining 'Admins' group.");
                OnlineAdmins.TryAdd(Context.ConnectionId, userId);
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }
            else
            {
                Console.WriteLine($"[SignalR] --> User {userName} is CLIENT. Joining own group.");
                // User thường sẽ join vào group tên là ID của chính họ
                if (!string.IsNullOrEmpty(userId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (Context.User != null && Context.User.IsInRole("Admin"))
            {
                OnlineAdmins.TryRemove(Context.ConnectionId, out _);
                Console.WriteLine($"[SignalR] Admin {userId} disconnected.");
            }
            await base.OnDisconnectedAsync(exception);
        }

        // 1. User gửi tin nhắn cho Admin
        public async Task SendMessageToAdmin(string message)
        {
            var userId = Context.UserIdentifier;
            // Lấy tên từ Claim hoặc fallback
            var userName = Context.User?.FindFirst("FullName")?.Value ?? Context.User?.Identity?.Name ?? "Khách";

            Console.WriteLine($"[SignalR] Msg from {userName}: {message} -> Sending to 'Admins' group.");

            // Gửi tới group "Admins"
            await Clients.Group("Admins").SendAsync("ReceiveMessageFromUser", userId, userName, message);
        }

        // 2. Admin trả lời lại User cụ thể
        [Authorize(Roles = "Admin")]
        public async Task SendMessageToUser(string userId, string message)
        {
            Console.WriteLine($"[SignalR] Admin replying to {userId}: {message}");
            // Gửi tới group userId (User đó)
            await Clients.Group(userId).SendAsync("OrderConfirmed", "Tin nhắn từ Admin: " + message, "CHAT"); // Tận dụng event có sẵn hoặc tạo mới
            
            // Hoặc gửi sự kiện chuẩn chat (cần update ở main.js client để nghe)
            await Clients.Group(userId).SendAsync("ReceiveMessageFromAdmin", message);
        }
    }
}