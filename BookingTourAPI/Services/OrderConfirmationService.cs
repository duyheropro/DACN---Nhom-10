using BookingTourAPI.Data;
using BookingTourAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore; // Quan trọng để dùng .Include()
using System;
using System.Threading.Tasks;

namespace BookingTourAPI.Services
{
    public class OrderConfirmationService : IOrderConfirmationService
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<AdminNotificationHub> _adminHubContext;
        private readonly IHubContext<UserNotificationHub> _userHubContext;
        private readonly IEmailSender _emailSender;
        private readonly IHubContext<PublicHub> _publicHub;

        public OrderConfirmationService(
            AppDbContext db,
            IHubContext<AdminNotificationHub> adminHubContext,
            IHubContext<UserNotificationHub> userHubContext,
            IEmailSender emailSender,
            IHubContext<PublicHub> publicHub)
        {
            _db = db;
            _adminHubContext = adminHubContext;
            _userHubContext = userHubContext;
            _emailSender = emailSender;
            _publicHub = publicHub;
        }

        public async Task<bool> ConfirmOrderAsync(string orderType, string orderId)
        {
            string? userIdToNotify = null;
            string customerEmail = "";
            string customerName = "Khách hàng";
            string emailSubject = "";
            string emailBody = "";
            bool statusChanged = false;
            string newStatus = "CONFIRMED";

            try
            {
                if (orderType.ToLower() == "tour")
                {
                    // --- 1. LẤY THÔNG TIN CHI TIẾT TOUR ---
                    var booking = await _db.TourBookings
                        .Include(b => b.TourDeparture)          // Kèm lịch khởi hành
                        .ThenInclude(td => td.TourPackage)      // Kèm thông tin gói tour
                        .FirstOrDefaultAsync(b => b.OrderId == orderId);

                    if (booking != null)
                    {
                        booking.Status = newStatus;
                        await _db.SaveChangesAsync(); // Lưu trạng thái trước

                        // Lấy dữ liệu để gửi mail
                        userIdToNotify = booking.UserId;
                        customerEmail = booking.ContactEmail;
                        customerName = booking.ContactName;
                        statusChanged = true;

                        // Tạo nội dung chi tiết
                        string tourName = booking.TourDeparture?.TourPackage?.Title ?? "Tour du lịch";
                        string startDate = booking.TourDeparture?.StartDate.ToString("dd/MM/yyyy") ?? "N/A";
                        string endDate = booking.TourDeparture?.EndDate?.ToString("dd/MM/yyyy") ?? "N/A";
                        
                        // Tạo chuỗi số lượng khách
                        string passengers = $"{booking.NumAdults} Người lớn";
                        if(booking.NumChildren > 0) passengers += $", {booking.NumChildren} Trẻ em";
                        if(booking.NumInfants > 0) passengers += $", {booking.NumInfants} Em bé";

                        emailSubject = $"[BookingTour] Xác nhận đặt tour #{orderId} thành công";
                        emailBody = $@"
                            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #ddd; border-radius: 8px; overflow: hidden;'>
                                <div style='background-color: #007bff; padding: 20px; text-align: center; color: white;'>
                                    <h2 style='margin: 0;'>XÁC NHẬN ĐẶT TOUR</h2>
                                </div>
                                <div style='padding: 20px;'>
                                    <p>Xin chào <strong>{customerName}</strong>,</p>
                                    <p>Đơn hàng <b>#{orderId}</b> của bạn đã được xác nhận thành công.</p>
                                    
                                    <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
                                    
                                    <h3 style='color: #007bff;'>Thông tin chuyến đi</h3>
                                    <table style='width: 100%; border-collapse: collapse;'>
                                        <tr>
                                            <td style='padding: 8px 0; color: #666;'>Tên Tour:</td>
                                            <td style='padding: 8px 0; font-weight: bold;'>{tourName}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px 0; color: #666;'>Ngày đi - về:</td>
                                            <td style='padding: 8px 0;'>{startDate} - {endDate}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 8px 0; color: #666;'>Hành khách:</td>
                                            <td style='padding: 8px 0;'>{passengers}</td>
                                        </tr>
                                    </table>

                                    <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
                                    
                                    <div style='text-align: right;'>
                                        <p style='margin: 0; color: #666;'>Tổng thanh toán:</p>
                                        <p style='margin: 5px 0 0; font-size: 24px; font-weight: bold; color: #d9534f;'>{booking.TotalPrice:N0} VND</p>
                                    </div>
                                </div>
                                <div style='background-color: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #888;'>
                                    <p>Cảm ơn bạn đã tin tưởng dịch vụ của BookingTourAPI.</p>
                                </div>
                            </div>";
                    }
                }
                else if (orderType.ToLower() == "flight")
                {
                    // --- XỬ LÝ VÉ MÁY BAY (Tương tự nếu cần) ---
                    var order = await _db.FlightOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                    if (order != null)
                    {
                        order.Status = newStatus;
                        await _db.SaveChangesAsync();

                        userIdToNotify = order.UserId;
                        customerEmail = order.TravelerEmail ?? "";
                        customerName = order.TravelerName ?? "Khách hàng";
                        statusChanged = true;

                        emailSubject = $"[BookingTour] Xác nhận vé máy bay #{orderId}";
                        emailBody = $@"
                            <h3>Xin chào {customerName},</h3>
                            <p>Đơn vé máy bay <b>#{orderId}</b> đã được xác nhận.</p>
                            <p>Mã đặt chỗ (Offer ID): <b>{order.FlightOfferId}</b></p>
                            <p>Tổng tiền: <b>{order.TotalPrice:N0} VND</b></p>
                            <p>Chúc bạn có chuyến bay tốt đẹp!</p>";
                    }
                }

                if (statusChanged)
                {
                    // SignalR Admin
                    await _adminHubContext.Clients.All.SendAsync("OrderStatusChanged", orderType, orderId, newStatus);

                    // SignalR User
                    if (!string.IsNullOrEmpty(userIdToNotify))
                    {
                        await _userHubContext.Clients.Group(userIdToNotify).SendAsync("OrderConfirmed", $"Đơn hàng {orderId} đã được xác nhận!", orderId);
                    }

                    // Gửi Email Chi Tiết
                    if (!string.IsNullOrEmpty(customerEmail))
                    {
                        await _emailSender.SendAsync(customerEmail, emailSubject, emailBody);
                    }

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error confirming order: {ex.Message}");
                return false;
            }
        }
    }
}