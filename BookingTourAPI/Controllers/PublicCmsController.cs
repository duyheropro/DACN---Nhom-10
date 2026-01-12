// TRONG FILE MỚI: Controllers/PublicCmsController.cs
using BookingTourAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookingTourAPI.Controllers
{
    [ApiController]
    [Route("api/public/cms")]
    public class PublicCmsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PublicCmsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("top-destinations")]
        public async Task<IActionResult> GetTopDestinations()
        {
            var destinations = await _db.TopDestinations
                                        .OrderBy(d => d.DisplayOrder)
                                        .Take(8) // Lấy 8 điểm đến
                                        .ToListAsync();
            return Ok(destinations);
        }

        // GET: api/public/cms/tours
        [HttpGet("tours")] // GET: api/public/cms/tours
        public async Task<IActionResult> GetPublicTourPackages([FromQuery] string? searchTerm = null)
        {
            var query = _db.TourPackages.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(t => t.Title.Contains(searchTerm) || t.Country.Contains(searchTerm) || t.Region.Contains(searchTerm));
            }

            var tours = await query
                                .OrderByDescending(t => t.Id) // Sắp xếp tour mới lên trước
                                .Select(t => new {
                                    t.Id,
                                    t.Title,
                                    t.ImageUrl, // Thêm ImageUrl vào đây
                                    t.Duration,
                                    t.Price,
                                    t.Currency,
                                    t.Country,
                                    t.Region,
                                    t.Highlights, // Cần hiển thị Highlights cho phần tóm tắt
                                    t.Area
                                })
                                .ToListAsync();

            return Ok(tours);
        }

        // GET: api/public/cms/tours/5
        [HttpGet("tours/{id}")]
        public async Task<IActionResult> GetPublicTourDetails(int id)
        {
            try
            {
                // SỬA LỖI: Dùng .Select() để tạo DTO
                // và ngăn chặn lỗi vòng lặp (ERR_INCOMPLETE_CHUNKED_ENCODING)
                var tour = await _db.TourPackages
                                        .AsNoTracking()
                                        .Where(t => t.Id == id)
                                        .Select(t => new {
                                            // Các trường của TourPackage
                                            t.Id,
                                            t.Title,
                                            t.ImageUrl,
                                            t.Price,
                                            t.Currency,
                                            t.Duration,
                                            t.Country,
                                            t.Region,
                                            t.Area,
                                            t.Highlights,
                                            t.PolicyIncludes,
                                            t.PolicyExcludes,
                                            
                                            Departures = t.Departures
                                                .Where(d => d.StartDate > DateTime.Now) // Chỉ lấy ngày tương lai
                                                .OrderBy(d => d.StartDate)
                                                .Select(d => new {
                                                    d.Id,
                                                    d.StartDate,
                                                    d.EndDate,
                                                    d.PriceAdult,
                                                    d.PriceChild,
                                                    d.PriceInfant,
                                                    d.AvailableSeats,
                                                    // LOGIC KHÓA TOUR TẠI API:
                                                    // 1. Hết chỗ (AvailableSeats <= 0)
                                                    // 2. Hoặc: Còn < 10 ngày khởi hành VÀ số chỗ còn ít (ví dụ < 5 chỗ) thì coi như khóa sổ
                                                    IsLocked = d.AvailableSeats <= 0 || ( (d.StartDate - DateTime.Now).TotalDays <= 10 && d.AvailableSeats < 5 ),
                                                    d.Airline,
                                                    d.FlightNumberOut, d.FlightNumberIn,
                                                    d.FlightTimeOut, d.FlightTimeIn
                                                }).ToList(),
                                            // Tạo danh sách DTO cho lịch trình
                                            Itineraries = t.Itineraries
                                                        .OrderBy(i => i.DayNumber) // Sắp xếp luôn ở đây
                                                        .Select(i => new {
                                                            i.Id,
                                                            i.DayNumber,
                                                            i.Title,
                                                            i.Description
                                                        })
                                                        .ToList()
                                        })
                                        .FirstOrDefaultAsync();

                if (tour == null)
                {
                    return NotFound("Không tìm thấy tour này.");
                }
                
                return Ok(tour); // Trả về DTO an toàn
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PublicCmsController] Lỗi GetPublicTourDetails {id}: {ex.Message}");
                return StatusCode(500, $"Lỗi server nội bộ: {ex.Message}");
            }
        }
    }
}