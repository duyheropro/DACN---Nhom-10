// TRONG FILE MỚI: Models/TopDestination.cs
using System.ComponentModel.DataAnnotations;

namespace BookingTourAPI.Models
{
    public class TopDestination
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string CityName { get; set; } = string.Empty; // Tên hiển thị: "Đà nẵng"

        [Required]
        public string ImageUrl { get; set; } = string.Empty; // Link ảnh

        [Required]
        [MaxLength(100)]
        public string SearchTerm { get; set; } = string.Empty; // Từ khóa tìm kiếm: "đà nẵng"

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = "tour"; // "tour" hoặc "hotel"

        public int DisplayOrder { get; set; } = 0; // Để sắp xếp
    }
}