using BookingTourAPI.Data; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using BookingTourAPI.Hubs;
using BookingTourAPI.Models;
using BookingTourAPI.Services;

namespace BookingTourAPI.Controllers
{
    [ApiController]
    [Route("api/admin")] 
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IOrderConfirmationService _orderService;

        public AdminController(AppDbContext db, IOrderConfirmationService orderService)
        {
            _db = db;
            _orderService = orderService;
        }
        
        // --- 1. Dashboard Stats ---
        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            // Tính doanh thu chuyến bay
            var flightRevenue = await _db.FlightOrders
                .Where(o => o.Status.Contains("CONFIRMED")) 
                .SumAsync(o => o.TotalPrice);
            
            // Tính doanh thu Tour (Sửa == thành Contains)
            var tourRevenue = await _db.TourBookings
                .Where(b => b.Status.Contains("CONFIRMED"))
                .SumAsync(b => b.TotalPrice);
            
            var stats = new { 
                TotalRevenue = (double)flightRevenue + (double)tourRevenue, 
                TotalHotelBookings = 0, // Đã xóa hotel
                TotalFlightOrders = await _db.FlightOrders.CountAsync(), 
                
                // Đếm số lượng đơn Tour từ bảng mới
                TotalActivityBookings = await _db.TourBookings.CountAsync() 
            };
            return Ok(stats);
        }

        // --- 2. API lấy danh sách đơn Tour (SỬA LỖI 404 TẠI ĐÂY) ---
        // Giữ nguyên đường dẫn "activity-bookings" để file dashboard.js không bị lỗi
        [HttpGet("activity-bookings")] 
        public async Task<IActionResult> GetActivityBookings()
        {
            var bookings = await _db.TourBookings
                .Include(tb => tb.TourDeparture)       // Join bảng Lịch khởi hành
                .ThenInclude(td => td.TourPackage)     // Join bảng Tour để lấy tên
                .OrderByDescending(b => b.BookingDate)
                .Take(50)
                .Select(b => new {
                    // Map các trường của TourBooking sang tên biến cũ mà JS đang dùng
                    amadeusOrderId = b.OrderId, 
                    activityName = b.TourDeparture.TourPackage.Title, // Lấy tên Tour
                    bookingDate = b.BookingDate,
                    totalPrice = b.TotalPrice,
                    currency = "VND",
                    status = b.Status,
                    tourDepartureId = b.TourDepartureId, // <--- THIS IS CORRECT
                    startDate = b.TourDeparture.StartDate // <--- THIS IS CORRECT
                })
                .ToListAsync();

            return Ok(bookings);
        }

        // --- 3. API lấy danh sách đơn Chuyến bay ---
        [HttpGet("flight-orders")]
        public async Task<IActionResult> GetFlightOrders()
        {
            var orders = await _db.FlightOrders
                .OrderByDescending(o => o.CreatedAt)
                .Take(50)
                .ToListAsync();
            return Ok(orders);
        }

        // --- 4. Xác nhận đơn hàng ---
        [HttpPut("bookings/confirm/{orderType}/{orderId}")]
        public async Task<IActionResult> ConfirmOrder(string orderType, string orderId)
        {
            if (string.IsNullOrEmpty(orderType) || string.IsNullOrEmpty(orderId))
            {
                return BadRequest("Loại đơn hàng và Mã đơn hàng là bắt buộc.");
            }
            
            var success = await _orderService.ConfirmOrderAsync(orderType, orderId);
            
            if (success) 
                return Ok(new { message = $"Đã xử lý xác nhận cho đơn hàng {orderId}." });
            else 
                return NotFound($"Không tìm thấy đơn hàng {orderId} hoặc lỗi khi xác nhận.");
        }
        
        // Endpoint cũ của Hotel (để trống hoặc trả về rỗng để không lỗi 404 nếu JS lỡ gọi)
        [HttpGet("hotel-bookings")]
        public IActionResult GetHotelBookings()
        {
            return Ok(new List<object>());
        }


        // --- 5. Thống kê doanh thu theo tháng (MỚI) ---
        [HttpGet("monthly-stats")]
        public async Task<IActionResult> GetMonthlyStats([FromQuery] int year = 0)
        {
            if (year == 0) year = DateTime.Now.Year;

            // 1. Lấy thống kê chuyến bay (Đã xác nhận)
            var flightStats = await _db.FlightOrders
                .Where(o => o.CreatedAt.Year == year && o.Status.Contains("CONFIRMED"))
                .GroupBy(o => o.CreatedAt.Month)
                .Select(g => new { Month = g.Key, Revenue = g.Sum(x => x.TotalPrice), Count = g.Count() })
                .ToListAsync();

            // 2. Lấy thống kê Tour (Đã xác nhận)
            var tourStats = await _db.TourBookings
                .Where(o => o.BookingDate.Year == year && o.Status.Contains("CONFIRMED"))
                .GroupBy(o => o.BookingDate.Month)
                .Select(g => new { Month = g.Key, Revenue = g.Sum(x => x.TotalPrice), Count = g.Count() })
                .ToListAsync();

            // 3. Gộp dữ liệu cho đủ 12 tháng
            var monthlyData = new List<object>();
            
            for (int i = 1; i <= 12; i++)
            {
                var f = flightStats.FirstOrDefault(x => x.Month == i);
                var t = tourStats.FirstOrDefault(x => x.Month == i);

                decimal revenue = (f?.Revenue ?? 0) + (t?.Revenue ?? 0);
                int count = (f?.Count ?? 0) + (t?.Count ?? 0);

                monthlyData.Add(new
                {
                    Month = i,
                    Revenue = revenue,
                    OrderCount = count
                });
            }

            return Ok(monthlyData);
        }
        // --- 6. Lấy chi tiết đơn hàng theo tháng (CHO CLICK BIỂU ĐỒ) ---
        [HttpGet("monthly-details")]
        public async Task<IActionResult> GetMonthlyDetails([FromQuery] int month, [FromQuery] int year)
        {
            if (year == 0) year = DateTime.Now.Year;

            // 1. Lấy chi tiết Tour
            var tours = await _db.TourBookings
                .Include(t => t.TourDeparture).ThenInclude(td => td.TourPackage)
                .Where(b => b.BookingDate.Month == month && b.BookingDate.Year == year && b.Status.Contains("CONFIRMED"))
                .Select(b => new
                {
                    Type = "Tour",
                    OrderId = b.OrderId,
                    Name = b.TourDeparture.TourPackage.Title,
                    Customer = b.ContactName,
                    Date = b.BookingDate,
                    Total = b.TotalPrice
                })
                .ToListAsync();

            // 2. Lấy chi tiết Flight
            var flights = await _db.FlightOrders
                .Where(f => f.CreatedAt.Month == month && f.CreatedAt.Year == year && f.Status.Contains("CONFIRMED"))
                .Select(f => new
                {
                    Type = "Flight",
                    OrderId = f.OrderId,
                    Name = $"Chuyến bay {f.FlightOfferId}", // Hoặc thông tin hãng bay
                    Customer = f.TravelerName,
                    Date = f.CreatedAt,
                    Total = f.TotalPrice
                })
                .ToListAsync();

            // Gộp lại và sắp xếp ngày mới nhất lên đầu
            var allOrders = tours.Cast<object>().Concat(flights).OrderByDescending(x => ((dynamic)x).Date).ToList();

            return Ok(allOrders);
        }

        // --- 6. Xóa đơn hàng (Admin) ---
        [HttpDelete("orders/{orderType}/{orderId}")]
        public async Task<IActionResult> DeleteOrder(string orderType, string orderId)
        {
            if (orderType.ToLower() == "flight")
            {
                var order = await _db.FlightOrders.FirstOrDefaultAsync(o => o.OrderId == orderId);
                if (order == null) return NotFound();
                _db.FlightOrders.Remove(order);
            }
            else if (orderType.ToLower() == "tour")
            {
                var booking = await _db.TourBookings.FirstOrDefaultAsync(b => b.OrderId == orderId);
                if (booking == null) return NotFound();
                _db.TourBookings.Remove(booking);
            }
            else return BadRequest("Loại đơn hàng không hợp lệ.");

            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã xóa đơn hàng thành công." });
        }
    }
}