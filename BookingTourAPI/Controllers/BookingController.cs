using BookingTourAPI.Data;
using BookingTourAPI.DTOs;
using BookingTourAPI.Hubs;
using BookingTourAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json.Nodes;

namespace BookingTourAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<AdminNotificationHub> _adminHubContext;
        private readonly IHubContext<PublicHub> _publicHubContext;

        public BookingController(AppDbContext db, IHubContext<AdminNotificationHub> adminHubContext, IHubContext<PublicHub> publicHubContext)
        {
            _db = db;
            _adminHubContext = adminHubContext;
            _publicHubContext = publicHubContext;
        }

        // 1. TẠO ĐƠN HÀNG (PENDING)
        [HttpPost("create-pending-order")]
        [Authorize]
        public async Task<IActionResult> CreatePendingOrder([FromBody] PendingOrderRequest request)
        {
            if (string.IsNullOrEmpty(request.ServiceType)) return BadRequest(new { message = "ServiceType là bắt buộc." });
            if (!ModelState.IsValid || request.ItemData == null || request.Traveler == null) return BadRequest(ModelState);

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string bookingId = $"DEMO-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            try
            {
                object? newOrderData = null;

                switch (request.ServiceType.ToLower())
                {
                    case "flight":
                        newOrderData = await CreatePendingFlightOrder(request.ItemData, request.Traveler, userId, bookingId);
                        break;
                    case "tour":
                        newOrderData = await CreatePendingTourOrder(request.ItemData, request.Traveler, userId, bookingId);
                        break;
                    default:
                        return BadRequest("Loại dịch vụ không hợp lệ.");
                }

                if (newOrderData != null)
                {
                    await _adminHubContext.Clients.All.SendAsync("NewPendingOrder", request.ServiceType.ToLower(), newOrderData);
                }

                return Ok(new { message = "Yêu cầu đặt vé đã được ghi nhận.", bookingId });
            }
            catch (ArgumentException argEx)
            {
                return BadRequest(new { message = $"Dữ liệu {request.ServiceType} không hợp lệ: {argEx.Message}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR creating pending order ({request.ServiceType}): {ex.Message}");
                return StatusCode(500, new { message = "Lỗi máy chủ khi lưu yêu cầu." });
            }
        }


        // 2. LẤY LỊCH SỬ ĐƠN HÀNG
        [HttpGet("my-orders")]
        [Authorize]
        public async Task<IActionResult> GetMyOrders()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Không tìm thấy thông tin người dùng.");

            try
            {
                // 1. Lấy đơn máy bay
                var flightOrders = await _db.FlightOrders
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(fo => fo.CreatedAt)
                    .Select(fo => new {
                        Type = "Flight",
                        OrderId = fo.OrderId,
                        Date = fo.CreatedAt,
                        Details = $"Flight Offer ID: {fo.FlightOfferId}",
                        TotalPrice = fo.TotalPrice,
                        Currency = fo.Currency,
                        Status = fo.Status,
                        // Các trường của Tour để null hoặc rỗng
                        StartDate = (DateTime?)null,
                        EndDate = (DateTime?)null,
                        People = fo.TravelerName ?? "1 người",
                        Transport = "Máy bay"
                    })
                    .ToListAsync();

                // 2. Lấy đơn Tour (CẬP NHẬT LẤY CHI TIẾT)
                var tourBookings = await _db.TourBookings
                    .Where(b => b.UserId == userId)
                    .Include(tb => tb.TourDeparture).ThenInclude(td => td.TourPackage)
                    .OrderByDescending(tb => tb.BookingDate)
                    .Select(tb => new {
                        Type = "Tour",
                        OrderId = tb.OrderId,
                        Date = tb.BookingDate, // Ngày đặt
                        Details = tb.TourDeparture.TourPackage.Title ?? "Tour du lịch",
                        TotalPrice = tb.TotalPrice,
                        Currency = "VND",
                        Status = tb.Status,
                        
                        // --- LẤY THÔNG TIN CHI TIẾT ---
                        StartDate = tb.TourDeparture.StartDate,
                        EndDate = tb.TourDeparture.EndDate,
                        // Tạo chuỗi hiển thị số người
                        People = $"{tb.NumAdults} người lớn" + 
                                (tb.NumChildren > 0 ? $", {tb.NumChildren} trẻ em" : "") + 
                                (tb.NumInfants > 0 ? $", {tb.NumInfants} em bé" : ""),
                        // Hiển thị phương tiện
                        Transport = (tb.TourDeparture.Airline != null) 
                                    ? $"{tb.TourDeparture.Airline} ({tb.TourDeparture.FlightNumberOut ?? "N/A"} - {tb.TourDeparture.FlightNumberIn ?? "N/A"})" 
                                    : "Xe du lịch"
                    })
                    .ToListAsync();

                // Gộp và sắp xếp
                var allOrders = flightOrders.Cast<object>().Concat(tourBookings.Cast<object>()).ToList();

                allOrders.Sort((a, b) => {
                    DateTime? dateA = a.GetType().GetProperty("Date")?.GetValue(a) as DateTime?;
                    DateTime? dateB = b.GetType().GetProperty("Date")?.GetValue(b) as DateTime?;
                    return (dateB ?? DateTime.MinValue).CompareTo(dateA ?? DateTime.MinValue);
                });

                return Ok(allOrders);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString()); // Log lỗi nếu có
                return StatusCode(500, new { message = "Lỗi máy chủ khi lấy lịch sử đặt vé." });
            }
        }

        // 3. HỦY ĐƠN HÀNG (FIXED: LOGGING + NO USER CHECK)

        [HttpPut("cancel/{orderType}/{orderId}")]
        [Authorize]
        public async Task<IActionResult> CancelMyOrder(string orderType, string orderId)
        {
            Console.WriteLine($"[API HIT] Yêu cầu hủy: Type={orderType}, ID={orderId}");

            if (orderType.ToLower() == "flight")
            {
                var order = await _db.FlightOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null) return NotFound(new { message = "Không tìm thấy đơn vé máy bay." });

                if (order.Status == "PENDING_CONFIRMATION")
                {
                    _db.FlightOrders.Remove(order);
                    await _db.SaveChangesAsync();
                    return Ok(new { message = "Đã xóa yêu cầu đặt vé.", action = "deleted" });
                }
                else
                {
                    order.Status = "CANCELLED";
                    await _db.SaveChangesAsync();
                    return Ok(new { message = "Đã hủy vé thành công.", action = "cancelled" });
                }
            }
            else if (orderType.ToLower() == "tour")
            {
                // QUAN TRỌNG: Phải Include TourDeparture để truy cập được AvailableSeats
                var booking = await _db.TourBookings
                    .Include(b => b.TourDeparture) 
                    .FirstOrDefaultAsync(b => b.OrderId == orderId);
                
                if (booking == null) return NotFound(new { message = "Không tìm thấy đơn tour." });

                if (booking.Status == "CANCELLED")
                {
                    return BadRequest(new { message = "Đơn hàng này đã bị hủy trước đó." });
                }

                // --- LOGIC HOÀN TRẢ CHỖ NGỒI ---
                // Chỉ hoàn chỗ nếu đơn hàng ĐÃ ĐƯỢC XÁC NHẬN (tức là đã bị trừ chỗ trước đó)
                if (booking.Status.Contains("CONFIRMED") && booking.TourDeparture != null)
                {
                    // Tính tổng số chỗ cần trả lại (Người lớn + Trẻ em)
                    int seatsToReturn = booking.NumAdults + booking.NumChildren;
                    
                    // Cộng lại vào kho
                    booking.TourDeparture.AvailableSeats += seatsToReturn;
                    
                    // (Tùy chọn) Log ra console để kiểm tra
                    Console.WriteLine($"[Cancel Tour] Hoàn lại {seatsToReturn} chỗ cho Tour {booking.TourDepartureId}. Chỗ hiện tại: {booking.TourDeparture.AvailableSeats}");
                }
                // -------------------------------

                if (booking.Status == "PENDING_CONFIRMATION")
                {
                    // Nếu mới chờ xác nhận (chưa trừ chỗ) thì chỉ cần xóa
                    _db.TourBookings.Remove(booking);
                    await _db.SaveChangesAsync();
                    return Ok(new { message = "Đã xóa yêu cầu đặt tour.", action = "deleted" });
                }
                else
                {
                    // Nếu đã xác nhận thì đổi trạng thái và lưu số chỗ mới
                    booking.Status = "CANCELLED";
                    await _db.SaveChangesAsync();

                    // Gửi SignalR báo cho mọi người biết số chỗ đã thay đổi (để cập nhật realtime)
                    if (_publicHubContext != null && booking.TourDeparture != null)
                    {
                        await _publicHubContext.Clients.Group($"Tour_{booking.TourDeparture.TourPackageId}")
                            .SendAsync("UpdateSeats", booking.TourDeparture.Id, booking.TourDeparture.AvailableSeats);
                    }

                    return Ok(new { message = "Đã hủy tour thành công. Số chỗ đã được hoàn lại.", action = "cancelled" });
                }
            }

            return BadRequest(new { message = "Loại dịch vụ không hợp lệ." });
        }

        // --- CÁC HÀM PRIVATE HỖ TRỢ ---
        private async Task<FlightOrder> CreatePendingFlightOrder(JsonObject itemData, TravelerInfo traveler, string? userId, string bookingId)
        {
            string? flightOfferId = itemData["id"]?.GetValue<string>();
            decimal totalPrice = 0;
            string? currency = null;

            if (itemData.TryGetPropertyValue("price", out var priceNode) && priceNode is JsonObject priceObj)
            {
                if (priceObj["total"] is JsonValue totalValue && totalValue.TryGetValue<string>(out var totalStr))
                {
                    decimal.TryParse(totalStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out totalPrice);
                }
                currency = priceObj["currency"]?.GetValue<string>();
            }
            
            var newOrder = new FlightOrder
            {
                OrderId = bookingId,
                FlightOfferId = flightOfferId,
                UserId = userId,
                TravelerName = traveler.Name,
                TravelerEmail = traveler.Email,
                TravelerPhone = traveler.Phone,
                TotalPrice = totalPrice,
                Currency = currency,
                Status = "PENDING_CONFIRMATION",
                CreatedAt = DateTime.Now
            };
            _db.FlightOrders.Add(newOrder);
            await _db.SaveChangesAsync();
            return newOrder;
        }

        private async Task<TourBooking> CreatePendingTourOrder(JsonObject itemData, TravelerInfo traveler, string? userId, string bookingId)
        {
            int departureId = 0;
            // LẤY SỐ LƯỢNG NGƯỜI TỪ JSON
            int nAdults = 1, nChildren = 0, nInfants = 0;
            if (itemData.TryGetPropertyValue("quantities", out var qtyNode) && qtyNode is JsonObject qtyObj)
            {
                if (qtyObj["adults"] is JsonValue aVal) nAdults = aVal.GetValue<int>();
                if (qtyObj["children"] is JsonValue cVal) nChildren = cVal.GetValue<int>();
                if (qtyObj["infants"] is JsonValue iVal) nInfants = iVal.GetValue<int>();
            }

            // LẤY TỔNG GIÁ TỪ JSON (Client đã tính)
            decimal totalPrice = 0;
            if (itemData.TryGetPropertyValue("price", out var priceNode) && priceNode is JsonValue priceValue)
            {
                if (priceValue.TryGetValue<decimal>(out var priceDec)) totalPrice = priceDec;
            }
            
            if (itemData.TryGetPropertyValue("departureId", out var depNode) && depNode is JsonValue depVal)
            {
                departureId = depVal.GetValue<int>();
            }

            var newBooking = new TourBooking
            {
                OrderId = bookingId,
                TourDepartureId = departureId,
                UserId = userId,
                ContactName = traveler.Name,
                ContactEmail = traveler.Email,
                ContactPhone = traveler.Phone,
                BookingDate = DateTime.Now,
                TotalPrice = totalPrice,
                Status = "PENDING_CONFIRMATION",
                // LƯU SỐ LƯỢNG VÀO DB
                NumAdults = nAdults,
                NumChildren = nChildren,
                NumInfants = nInfants
            };

            _db.TourBookings.Add(newBooking);
            await _db.SaveChangesAsync();
            return newBooking;
        }
    }
}