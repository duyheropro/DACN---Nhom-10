using Microsoft.AspNetCore.SignalR;

namespace BookingTourAPI.Hubs
{
    // Hub này cho phép mọi người (kể cả chưa login) kết nối để nhận cập nhật realtime
    public class PublicHub : Hub
    {
        // Client sẽ join vào group theo TourId để chỉ nhận tin của tour mình đang xem
        public async Task JoinTourGroup(string tourId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Tour_{tourId}");
        }

        public async Task LeaveTourGroup(string tourId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Tour_{tourId}");
        }
    }
}