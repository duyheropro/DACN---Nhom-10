// TRONG FILE MỚI: DTOs/PendingOrderRequest.cs
using System.Text.Json.Nodes; // Cần cho JsonObject
using System.ComponentModel.DataAnnotations;

namespace BookingTourAPI.DTOs
{
    // Giữ lại class TravelerInfo nếu đã tạo ở bước trước, nếu chưa thì tạo mới
    public class TravelerInfo
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Phone { get; set; } = string.Empty;
    }

    public class PendingOrderRequest
    {
        [Required]
        public string? ServiceType { get; set; } // Sẽ là "flight", "hotel", hoặc "tour"

        // Dùng JsonObject để nhận dữ liệu gốc của item (linh hoạt)
        [Required]
        public JsonObject? ItemData { get; set; }

        [Required]
        public TravelerInfo? Traveler { get; set; }
    }
}