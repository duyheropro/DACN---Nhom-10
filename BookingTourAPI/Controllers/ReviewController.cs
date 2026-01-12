using BookingTourAPI.Data;
using BookingTourAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BookingTourAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // DTO nhận từ client
        public class CreateReviewRequest
        {
            public int TourPackageId { get; set; }
            public int Rating { get; set; }   // 1-5
            public string Comment { get; set; } = string.Empty;
        }

        // ========== USER GỬI ĐÁNH GIÁ ==========
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateReviewRequest request)
        {
            if (request.Rating < 1 || request.Rating > 5)
            {
                return BadRequest(new { message = "Điểm đánh giá phải từ 1 đến 5." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Check tour tồn tại
            var tour = await _context.TourPackages.FindAsync(request.TourPackageId);
            if (tour == null)
            {
                return NotFound(new { message = "Tour không tồn tại." });
            }

            // ❌ KHÔNG CHẶN NỮA: cho phép 1 user đánh giá nhiều lần / 1 tour

            var review = new Review
            {
                UserId = user.Id,
                TourPackageId = request.TourPackageId,
                Rating = request.Rating,
                Comment = request.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Gửi đánh giá thành công." });
        }

        // ========== LẤY DANH SÁCH ĐÁNH GIÁ THEO TOUR ==========
        [HttpGet("by-tour/{tourId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByTour(int tourId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.TourPackageId == tourId)
                .OrderByDescending(r => r.CreatedAt)
                .Include(r => r.User)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    UserEmail = r.User != null ? r.User.Email : ""
                })
                .ToListAsync();

            double average = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;

            return Ok(new
            {
                averageRating = average,
                totalReviews = reviews.Count,
                items = reviews
            });
        }

        // ========== LẤY ĐÁNH GIÁ CỦA CHÍNH USER ==========
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyReviews()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var reviews = await _context.Reviews
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    r.TourPackageId
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // ================== API ADMIN QUẢN LÝ COMMENT ==================

        // Lấy tất cả review, có thể filter theo tourId
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminGetAll([FromQuery] int? tourId)
        {
            var query = _context.Reviews
                .Include(r => r.User)
                .Include(r => r.TourPackage)
                .AsQueryable();

            if (tourId.HasValue)
            {
                query = query.Where(r => r.TourPackageId == tourId.Value);
            }

            var list = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    TourId = r.TourPackageId,
                    TourName = r.TourPackage != null ? r.TourPackage.Title : "",
                    UserEmail = r.User != null ? r.User.Email : ""
                })
                .ToListAsync();

            return Ok(list);
        }

        // Admin xoá review
        [HttpDelete("admin/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDelete(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xoá đánh giá." });
        }
    }
}
