// File: Controllers/PaymentController.cs
using BookingTourAPI.Data;
using BookingTourAPI.Hubs;
using BookingTourAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BookingTourAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<AdminNotificationHub> _adminHub;
        private readonly IHubContext<UserNotificationHub> _userHub;
        private readonly IEmailSender _emailSender;

        // Thay IP này bằng IP máy tính của bạn để test trên điện thoại
        private const string MY_IP_ADDRESS = "192.168.0.216:5062"; 

        public PaymentController(
            AppDbContext db,
            IHubContext<AdminNotificationHub> adminHub,
            IHubContext<UserNotificationHub> userHub,
            IEmailSender emailSender)
        {
            _db = db;
            _adminHub = adminHub;
            _userHub = userHub;
            _emailSender = emailSender;
        }

        public class MomoRequest
        {
            public required string OrderId { get; set; }
            public required string OrderType { get; set; }
        }

        [HttpPost("create-momo-qr")]
        [Authorize]
        public IActionResult CreateMomoQr([FromBody] MomoRequest request)
        {
            string viewUrl = $"http://{MY_IP_ADDRESS}/api/payment/view-momo-order?orderId={request.OrderId}&orderType={request.OrderType}";
            string qrApi = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={System.Net.WebUtility.UrlEncode(viewUrl)}";
            return Ok(new { qrCodeUrl = qrApi });
        }

        [HttpGet("view-momo-order")]
        [AllowAnonymous]
        public async Task<IActionResult> ViewMomoOrder(string orderId, string orderType)
        {
            // 1. Khai báo biến hiển thị mặc định
            string serviceName = "Dịch vụ chưa xác định";
            string customerName = "Khách hàng";
            decimal amount = 0;
            string createdDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            // 2. Truy vấn dữ liệu thực tế từ DB dựa trên orderId
            if (orderType.ToLower() == "flight")
            {
                var order = await _db.FlightOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null) return Content("<h2 style='color:red;text-align:center'>Không tìm thấy đơn hàng bay!</h2>", "text/html");

                serviceName = $"Vé máy bay (Offer: {order.FlightOfferId})";
                customerName = order.TravelerName ?? "Khách";
                amount = order.TotalPrice;
                createdDate = order.CreatedAt.ToString("dd/MM/yyyy HH:mm");
            }
            else if (orderType.ToLower() == "tour")
            {
                // Include để lấy tên TourPackage từ quan hệ bảng
                var booking = await _db.TourBookings
                    .Include(b => b.TourDeparture)
                    .ThenInclude(td => td.TourPackage)
                    .FirstOrDefaultAsync(b => b.OrderId == orderId);

                if (booking == null) return Content("<h2 style='color:red;text-align:center'>Không tìm thấy đơn tour!</h2>", "text/html");

                // Lấy tên tour (nếu null thì để fallback)
                serviceName = booking.TourDeparture?.TourPackage?.Title ?? "Tour du lịch";
                
                // Cắt ngắn tên tour nếu quá dài để hiển thị đẹp hơn
                if (serviceName.Length > 50) serviceName = serviceName.Substring(0, 47) + "...";

                customerName = booking.ContactName ?? "Khách";
                amount = booking.TotalPrice;
                createdDate = booking.BookingDate.ToString("dd/MM/yyyy HH:mm");
            }

            // 3. Trả về giao diện HTML chi tiết (giống hóa đơn MoMo thật)
            string htmlContent = $@"
                <!DOCTYPE html>
                <html lang='vi'>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1'>
                    <title>Thanh toán MoMo</title>
                    <style>
                        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f4f7f9; margin: 0; padding: 20px; display: flex; justify-content: center; align-items: center; min-height: 100vh; }}
                        .momo-card {{ background: white; width: 100%; max-width: 380px; border-radius: 16px; box-shadow: 0 4px 20px rgba(0,0,0,0.1); overflow: hidden; }}
                        .header {{ background-color: #A50064; padding: 20px; text-align: center; color: white; }}
                        .header img {{ width: 50px; height: 50px; margin-bottom: 10px; background: white; border-radius: 50%; padding: 2px; }}
                        .header h2 {{ margin: 0; font-size: 18px; font-weight: 600; }}
                        .amount {{ font-size: 32px; font-weight: bold; margin: 10px 0; }}
                        .body {{ padding: 20px; }}
                        .info-row {{ display: flex; justify-content: space-between; margin-bottom: 15px; font-size: 14px; color: #555; }}
                        .info-row strong {{ color: #333; font-weight: 600; text-align: right; max-width: 60%; }}
                        .divider {{ border-bottom: 1px dashed #ddd; margin: 15px 0; }}
                        .btn-pay {{ background-color: #A50064; color: white; border: none; width: 100%; padding: 15px; border-radius: 8px; font-size: 16px; font-weight: bold; cursor: pointer; transition: 0.2s; }}
                        .btn-pay:hover {{ background-color: #8E0057; }}
                        .footer {{ text-align: center; font-size: 12px; color: #999; margin-top: 15px; }}
                    </style>
                </head>
                <body>
                    <div class='momo-card'>
                        <div class='header'>
                            <img src='https://upload.wikimedia.org/wikipedia/vi/f/fe/MoMo_Logo.png' alt='MoMo'>
                            <h2>Xác nhận thanh toán</h2>
                            <div class='amount'>{amount:N0} đ</div>
                        </div>
                        <div class='body'>
                            <div class='info-row'>
                                <span>Khách hàng</span>
                                <strong>{customerName}</strong>
                            </div>
                            <div class='info-row'>
                                <span>Dịch vụ</span>
                                <strong>{serviceName}</strong>
                            </div>
                            <div class='info-row'>
                                <span>Mã đơn hàng</span>
                                <strong>{orderId}</strong>
                            </div>
                            <div class='info-row'>
                                <span>Thời gian</span>
                                <strong>{createdDate}</strong>
                            </div>
                            
                            <div class='divider'></div>

                            <form action='/api/payment/process-momo-mobile' method='POST'>
                                <input type='hidden' name='orderId' value='{orderId}' />
                                <input type='hidden' name='orderType' value='{orderType}' />
                                
                                <button type='submit' name='paymentMode' value='FULL' class='btn-pay'>
                                    Thanh toán ngay ({amount:N0} đ)
                                </button>
                                <div style='height:10px'></div>
                                <button type='submit' name='paymentMode' value='DEPOSIT' style='background:#fff; color:#A50064; border:1px solid #A50064' class='btn-pay'>
                                    Đặt cọc 50% ({(amount/2):N0} đ)
                                </button>
                            </form>
                            
                            <div class='footer'>Giao dịch an toàn và bảo mật bởi MoMo</div>
                        </div>
                    </div>
                </body>
                </html>";

            return Content(htmlContent, "text/html");
        }

        [HttpPost("process-momo-mobile")]
        [AllowAnonymous]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> ProcessMomoMobile([FromForm] string orderId, [FromForm] string orderType, [FromForm] string paymentMode)
        {
            string newStatus = (paymentMode == "DEPOSIT") ? "CONFIRMED_DEPOSIT" : "CONFIRMED_FULL";
            
            string? userId = null;
            string? email = null;
            string? name = null;
            decimal amount = 0;
            bool success = false;

            // Biến để chứa thông tin chi tiết cho Email
            string emailDetailHtml = ""; 

            if (orderType.ToLower() == "tour")
            {
                // 1. QUAN TRỌNG: Thêm .Include để lấy thông tin chi tiết Tour
                var booking = await _db.TourBookings
                    .Include(b => b.TourDeparture)
                    .ThenInclude(td => td.TourPackage)
                    .FirstOrDefaultAsync(b => b.OrderId == orderId);

                if (booking != null)
                {
                    booking.Status = newStatus;
                    userId = booking.UserId;
                    email = booking.ContactEmail;
                    name = booking.ContactName;
                    amount = booking.TotalPrice;
                    success = true;

                    // 2. XỬ LÝ DỮ LIỆU HIỂN THỊ CHO TOUR
                    string tourName = booking.TourDeparture?.TourPackage?.Title ?? "Tour du lịch";
                    string startDate = booking.TourDeparture?.StartDate.ToString("dd/MM/yyyy") ?? "N/A";
                    string endDate = booking.TourDeparture?.EndDate?.ToString("dd/MM/yyyy") ?? "N/A";
                    
                    // Xử lý số người
                    List<string> peopleParts = new List<string>();
                    if (booking.NumAdults > 0) peopleParts.Add($"{booking.NumAdults} người lớn");
                    if (booking.NumChildren > 0) peopleParts.Add($"{booking.NumChildren} trẻ em");
                    if (booking.NumInfants > 0) peopleParts.Add($"{booking.NumInfants} em bé");
                    string peopleStr = string.Join(", ", peopleParts);

                    // Xử lý phương tiện
                    string transportStr = "Xe du lịch";
                    if (!string.IsNullOrEmpty(booking.TourDeparture?.Airline))
                    {
                        transportStr = $"{booking.TourDeparture.Airline} ({booking.TourDeparture.FlightNumberOut ?? "?"} - {booking.TourDeparture.FlightNumberIn ?? "?"})";
                    }

                    // Tạo đoạn HTML chi tiết Tour (Style giống bạn yêu cầu)
                    emailDetailHtml = $@"
                        <div style='background-color: #f9f9f9; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #007bff;'>
                            <h3 style='margin-top: 0; color: #007bff;'>{tourName}</h3>
                            <p style='margin: 5px 0;'><b>Lịch trình:</b> {startDate} - {endDate}</p>
                            <p style='margin: 5px 0;'><b>Số người:</b> {peopleStr}</p>
                            <p style='margin: 5px 0;'><b>Phương tiện:</b> {transportStr}</p>
                        </div>";
                }
            }
            else if (orderType.ToLower() == "flight")
            {
                var order = await _db.FlightOrders.FirstOrDefaultAsync(f => f.OrderId == orderId);
                if (order != null)
                {
                    order.Status = newStatus;
                    userId = order.UserId;
                    email = order.TravelerEmail;
                    name = order.TravelerName;
                    amount = order.TotalPrice;
                    success = true;

                    // Tạo đoạn HTML chi tiết Vé máy bay
                    emailDetailHtml = $@"
                        <div style='background-color: #f9f9f9; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #28a745;'>
                            <h3 style='margin-top: 0; color: #28a745;'>Vé máy bay</h3>
                            <p style='margin: 5px 0;'><b>Mã đặt chỗ (Offer ID):</b> {order.FlightOfferId}</p>
                            <p style='margin: 5px 0;'><b>Hành khách:</b> {name}</p>
                        </div>";
                }
            }

            if (success)
            {
                await _db.SaveChangesAsync();

                // SignalR
                await _adminHub.Clients.All.SendAsync("OrderStatusChanged", orderType, orderId, newStatus);
                if (!string.IsNullOrEmpty(userId))
                    await _userHub.Clients.Group(userId).SendAsync("OrderConfirmed", "Thanh toán thành công!", orderId);

                // 3. GỬI EMAIL VỚI NỘI DUNG MỚI
                if (!string.IsNullOrEmpty(email))
                {
                    string subject = $"[BookingTour] Thanh toán thành công đơn #{orderId}";
                    
                    // Lắp ráp body mail
                    string body = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; color: #333;'>
                            <h2 style='color:#28a745; text-align: center;'>Thanh toán thành công!</h2>
                            <p>Xin chào <b>{name}</b>,</p>
                            <p>Chúng tôi đã nhận được khoản thanh toán cho đơn hàng <b>#{orderId}</b>.</p>
                            
                            {emailDetailHtml}
                            
                            <p style='font-size: 18px;'><b>Số tiền đã thanh toán:</b> <span style='color: #d9534f; font-weight: bold;'>{(paymentMode == "DEPOSIT" ? amount/2 : amount):N0} VND</span></p>
                            <hr style='border:0; border-top:1px solid #eee;'>
                            <p style='text-align: center; color: #777; font-size: 12px;'>Cảm ơn bạn đã tin tưởng dịch vụ của BookingTourAPI!</p>
                        </div>";

                    await _emailSender.SendAsync(email, subject, body);
                }

                return Content("<h1 style='text-align:center; color:green; margin-top:50px;'>Thanh toán thành công! Vui lòng kiểm tra email.</h1>", "text/html; charset=utf-8");
            }

            return Content("<h1>Lỗi xử lý thanh toán</h1>", "text/html");
        }
    }
}