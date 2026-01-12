// TRONG FILE MỚI: Controllers/AdminCmsController.cs
using BookingTourAPI.Data;
using BookingTourAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR; 
using BookingTourAPI.Hubs;

namespace BookingTourAPI.Controllers
{
    [ApiController]
    [Route("api/admin/cms")]
    [Authorize(Roles = "Admin")] // Chỉ Admin được vào
    public class AdminCmsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<PublicHub> _publicHub;

        public AdminCmsController(AppDbContext db, IHubContext<PublicHub> publicHub)
        {
            _db = db;
            _publicHub = publicHub;
        }

        [HttpGet("top-destinations")]
        public async Task<IActionResult> GetTopDestinations()
        {
            var destinations = await _db.TopDestinations.OrderBy(d => d.DisplayOrder).ToListAsync();
            return Ok(destinations);
        }

        [HttpPost("top-destinations")]
        public async Task<IActionResult> CreateTopDestination([FromBody] TopDestination destination)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _db.TopDestinations.Add(destination);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTopDestinations), new { id = destination.Id }, destination);
        }

        [HttpPut("top-destinations/{id}")]
        public async Task<IActionResult> UpdateTopDestination(int id, [FromBody] TopDestination destination)
        {
            if (id != destination.Id) return BadRequest("ID không khớp.");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existingDest = await _db.TopDestinations.FindAsync(id);
            if (existingDest == null) return NotFound("Không tìm thấy điểm đến.");

            _db.Entry(existingDest).CurrentValues.SetValues(destination);
            await _db.SaveChangesAsync();
            return Ok(existingDest);
        }

        [HttpDelete("top-destinations/{id}")]
        public async Task<IActionResult> DeleteTopDestination(int id)
        {
            var destination = await _db.TopDestinations.FindAsync(id);
            if (destination == null) return NotFound("Không tìm thấy điểm đến.");

            _db.TopDestinations.Remove(destination);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã xóa điểm đến." });
        }

        // --- QUẢN LÝ GÓI TOUR (TourPackage) ---

        // TRONG FILE: Controllers/AdminCmsController.cs

        [HttpGet("tours")] // GET /api/admin/cms/tours
        public async Task<IActionResult> GetTourPackages()
        {
            try
            {
                // Sắp xếp theo ID (mới lên trước) và CHỈ CHỌN CÁC TRƯỜNG CẦN THIẾT
                var tours = await _db.TourPackages
                                        .OrderByDescending(t => t.Id)
                                        .Select(t => new {
                                            // Chọn các trường mà bảng admin cần
                                            t.Id,
                                            t.Title,
                                            t.Duration,
                                            t.Price
                                        })
                                        .ToListAsync();
                return Ok(tours);
            }
            catch (Exception ex)
            {
                // Bắt lỗi nếu có
                Console.WriteLine($"[AdminCmsController] Lỗi GetTourPackages: {ex.Message}");
                return StatusCode(500, "Lỗi máy chủ nội bộ khi tải gói tour.");
            }
        }

        [HttpGet("tours/{id}")] // GET /api/admin/cms/tours/5
        public async Task<IActionResult> GetTourPackageById(int id)
        {
            // Dùng AsNoTracking() để EF không theo dõi, tăng hiệu suất
            var tourResult = await _db.TourPackages
                                        .AsNoTracking() 
                                        .Where(t => t.Id == id)
                                        // Dùng .Select() để tạo DTO an toàn ngay lập tức
                                        // Điều này NGĂN CHẶN lỗi vòng lặp
                                        .Select(t => new {
                                            // Tất cả các trường của TourPackage
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
                                            
                                            // Tạo danh sách DTO cho lịch trình
                                            Itineraries = t.Itineraries
                                                        .OrderBy(i => i.DayNumber)
                                                        .Select(i => new {
                                                            i.Id,
                                                            i.DayNumber,
                                                            i.Title,
                                                            i.Description,
                                                            i.TourPackageId // Thêm ID này để an toàn
                                                        })
                                                        .ToList()
                                        })
                                        .FirstOrDefaultAsync(); // Chỉ lấy 1
            
            if (tourResult == null) return NotFound();
            
            return Ok(tourResult); // Trả về DTO an toàn (không còn vòng lặp)
        }

        [HttpPost("tours")] // POST /api/admin/cms/tours
        public async Task<IActionResult> CreateTourPackage([FromBody] TourPackage newTour)
        {
            // Gán 1 list rỗng để đảm bảo nó không bị null
            newTour.Itineraries = new List<DailyItinerary>(); 
            
            _db.TourPackages.Add(newTour);
            await _db.SaveChangesAsync(); // newTour.Id đã được gán
            
            // --- SỬA LỖI 500 TẠI ĐÂY ---
            // 'newTour' chứa tham chiếu lồng (Itineraries -> TourPackage) gây lỗi vòng lặp.
            // Chúng ta chỉ trả về một đối tượng đơn giản (DTO) mà frontend cần.
            var resultDto = new {
                id = newTour.Id,
                title = newTour.Title,
                duration = newTour.Duration,
                price = newTour.Price
                // Frontend (dashboard.js) thực ra chỉ cần 'id',
                // nhưng chúng ta trả về thêm các trường khác để nó giống
                // đối tượng trong bảng.
            };

            // Trả về 201 Created, kèm đối tượng mới (đã an toàn)
            return CreatedAtAction(nameof(GetTourPackageById), new { id = newTour.Id }, resultDto);
        }

        [HttpPut("tours/{id}")] // PUT /api/admin/cms/tours/5
        public async Task<IActionResult> UpdateTourPackage(int id, [FromBody] TourPackage updatedTour)
        {
            if (id != updatedTour.Id) return BadRequest("ID không khớp");

            var existingTour = await _db.TourPackages.FindAsync(id);
            if (existingTour == null) return NotFound();

            // Cập nhật các trường
            existingTour.Title = updatedTour.Title;
            existingTour.ImageUrl = updatedTour.ImageUrl;
            existingTour.Price = updatedTour.Price;
            existingTour.Currency = updatedTour.Currency;
            existingTour.Duration = updatedTour.Duration;

            // --- SỬA LỖI & THÊM TÍNH NĂNG ---
            // (Các trường này bị thiếu trong code cũ của bạn khi cập nhật)
            existingTour.Country = updatedTour.Country;
            existingTour.Region = updatedTour.Region;
            existingTour.Area = updatedTour.Area; // <-- Trường phân loại mới
            // ---------------------------------

            existingTour.Highlights = updatedTour.Highlights;
            existingTour.PolicyIncludes = updatedTour.PolicyIncludes;
            existingTour.PolicyExcludes = updatedTour.PolicyExcludes;

            _db.Entry(existingTour).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            
            return Ok(existingTour);
        }
        [HttpDelete("tours/{id}")] // DELETE /api/admin/cms/tours/5
        public async Task<IActionResult> DeleteTourPackage(int id)
        {
            var tour = await _db.TourPackages.FindAsync(id);
            if (tour == null) return NotFound();

            // EF sẽ tự động xóa các DailyItinerary liên quan nếu bạn cấu hình OnDelete Cascade
            // (Nếu không, bạn phải xóa thủ công trước)
            _db.TourPackages.Remove(tour);
            await _db.SaveChangesAsync();
            
            return NoContent(); // Xóa thành công
        }


        // --- QUẢN LÝ LỊCH TRÌNH (DailyItinerary) ---

        [HttpPost("itineraries")] // POST /api/admin/cms/itineraries
        public async Task<IActionResult> AddItinerary([FromBody] DailyItinerary itineraryData)
        {
            // 1. Kiểm tra xem TourPackageId có tồn tại không
            var tourExists = await _db.TourPackages.AnyAsync(t => t.Id == itineraryData.TourPackageId);
            if (!tourExists) 
            {
                return BadRequest("Tour Package không tồn tại.");
            }

            // 2. TẠO MỘT ĐỐI TƯỢNG MỚI (Cách an toàn)
            // Không thêm 'itineraryData' trực tiếp vì nó có navigation property 'TourPackage' bị null
            var newItinerary = new DailyItinerary
            {
                DayNumber = itineraryData.DayNumber,
                Title = itineraryData.Title,
                Description = itineraryData.Description,
                TourPackageId = itineraryData.TourPackageId // Chỉ gán Foreign Key ID
            };

            // 3. Thêm đối tượng mới và lưu lại
            _db.DailyItineraries.Add(newItinerary);
            await _db.SaveChangesAsync();
            
            // 4. Trả về đối tượng đã được tạo (có ID mới)
            return Ok(newItinerary); 
        }

        [HttpPut("itineraries/{id}")] // PUT /api/admin/cms/itineraries/10
        public async Task<IActionResult> UpdateItinerary(int id, [FromBody] DailyItinerary updatedItinerary)
        {
            if (id != updatedItinerary.Id) return BadRequest();

            var existingItem = await _db.DailyItineraries.FindAsync(id);
            if (existingItem == null) return NotFound();

            existingItem.DayNumber = updatedItinerary.DayNumber;
            existingItem.Title = updatedItinerary.Title;
            existingItem.Description = updatedItinerary.Description;

            _db.Entry(existingItem).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            
            return Ok(existingItem);
        }

        [HttpDelete("itineraries/{id}")] // DELETE /api/admin/cms/itineraries/10
        public async Task<IActionResult> DeleteItinerary(int id)
        {
            var item = await _db.DailyItineraries.FindAsync(id);
            if (item == null) return NotFound();

            _db.DailyItineraries.Remove(item);
            await _db.SaveChangesAsync();
            
            return NoContent();
        }

        // TRONG FILE: Controllers/AdminCmsController.cs

        // 1. Lấy danh sách lịch khởi hành của 1 tour
        [HttpGet("tours/{tourId}/departures")]
        public async Task<IActionResult> GetDeparturesByTour(int tourId)
        {
            var departures = await _db.TourDepartures
                .Where(d => d.TourPackageId == tourId)
                .OrderBy(d => d.StartDate)
                .ToListAsync();
            return Ok(departures);
        }

        // 2. Thêm lịch khởi hành mới
        // TRONG FILE: Controllers/AdminCmsController.cs

        [HttpPost("departures")]
        public async Task<IActionResult> AddDeparture([FromBody] TourDeparture departure)
        {
            if (departure.TourPackageId == 0) return BadRequest("Chưa chọn tour.");
            
            // Tự tính ngày về nếu chưa có
            if (departure.EndDate == null)
            {
                var tour = await _db.TourPackages.FindAsync(departure.TourPackageId);
                int days = 1;
                if (tour != null && !string.IsNullOrEmpty(tour.Duration))
                {
                    // Xử lý chuỗi duration an toàn hơn
                    var durationStr = tour.Duration.ToUpper();
                    if(durationStr.Contains("N")) 
                    {
                        int.TryParse(durationStr.Split('N')[0], out days);
                    }
                }
                departure.EndDate = departure.StartDate.AddDays(days > 0 ? days - 1 : 0);
            }

            _db.TourDepartures.Add(departure);
            await _db.SaveChangesAsync();

            // --- SỬA LỖI 500 TẠI ĐÂY ---
            // Thay vì return Ok(departure); -> gây lỗi vòng lặp
            // Chúng ta tạo một đối tượng mới (DTO) chỉ chứa thông tin cần thiết
            var resultSafe = new 
            {
                departure.Id,
                departure.TourPackageId,
                departure.StartDate,
                departure.EndDate,
                departure.PriceAdult,
                departure.PriceChild,
                departure.PriceInfant,
                departure.AvailableSeats
            };

            return Ok(resultSafe);
        }

        // 3. Xóa lịch khởi hành
        [HttpDelete("departures/{id}")]
        public async Task<IActionResult> DeleteDeparture(int id)
        {
            var dep = await _db.TourDepartures.FindAsync(id);
            if (dep == null) return NotFound();
            _db.TourDepartures.Remove(dep);
            await _db.SaveChangesAsync();
            return Ok();
        }

        
        // 1. Xem danh sách khách đặt theo ngày khởi hành
        [HttpGet("tour-bookings/{departureId}")]
        public async Task<IActionResult> GetTourBookingsByDeparture(int departureId)
        {
            var bookings = await _db.TourBookings
                .Where(b => b.TourDepartureId == departureId && b.Status.Contains("CONFIRMED"))
                .Select(b => new {
                    b.OrderId,
                    b.ContactName,
                    b.ContactEmail,
                    b.ContactPhone,
                    People = $"{b.NumAdults} Lớn, {b.NumChildren} Trẻ, {b.NumInfants} Bé",
                    b.TotalPrice,
                    b.BookingDate
                })
                .ToListAsync();

            return Ok(bookings);
        }

        // 2. Khóa Tour (Set số chỗ về 0 để không ai đặt được nữa)
        [HttpPut("departures/{id}/lock")]
        public async Task<IActionResult> LockDeparture(int id)
        {
            var dep = await _db.TourDepartures.FindAsync(id);
            if (dep == null) return NotFound();

            dep.AvailableSeats = 0; // Khóa chỗ
            await _db.SaveChangesAsync();

            // Báo cho Client biết ngay lập tức (Bỏ comment đi)
            await _publicHub.Clients.Group($"Tour_{dep.TourPackageId}").SendAsync("UpdateSeats", id, 0);

            return Ok(new { message = "Đã khóa tour thành công." });
        }

        // 3. API Lấy danh sách các Tour sắp đầy (để hiển thị cố định)
        [HttpGet("warning-departures")]
        public async Task<IActionResult> GetWarningDepartures()
        {
            // Giả sử tổng chỗ mặc định là 20. Nếu bạn lưu tổng chỗ trong DB thì thay số 20 bằng cột đó.
            int totalSeats = 20;
            int warningThreshold = (int)(totalSeats * 0.66); // Ngưỡng 13-14 chỗ đã đặt
            // Tức là số chỗ còn lại <= (20 - 14) = 6 chỗ

            var warnings = await _db.TourDepartures
                .Include(d => d.TourPackage)
                .Where(d => d.AvailableSeats <= (totalSeats - warningThreshold) && d.AvailableSeats > 0) // Sắp đầy nhưng chưa hết hẳn
                .Select(d => new 
                {
                    DepartureId = d.Id,
                    TourName = d.TourPackage.Title,
                    StartDate = d.StartDate,
                    EndDate = d.EndDate,
                    PriceAdult = d.PriceAdult,
                    PriceChild = d.PriceChild,
                    PriceInfant = d.PriceInfant,
                    AvailableSeats = d.AvailableSeats,
                    TotalSeats = totalSeats,
                    BookedSeats = totalSeats - d.AvailableSeats
                })
                .OrderBy(d => d.StartDate)
                .ToListAsync();

            return Ok(warnings);
        }
    }
}