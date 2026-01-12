// Trong Models/ActivityBooking.cs
using System.ComponentModel.DataAnnotations;

namespace BookingTourAPI.Models
{
    public class ActivityBooking
    {
        [Key]
        public int Id { get; set; }
        public string? AmadeusOrderId { get; set; } // Mã đơn của Amadeus
        public string? UserId { get; set; } // Dùng cho Bước 4 (Bảo mật)
        
        [Required]
        public string? ActivityId { get; set; } // Mã của Activity
        
        [Required]
        public string? ActivityName { get; set; } // Lưu tên để dễ truy vấn
        
        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public decimal TotalPrice { get; set; }
        
        public string? Currency { get; set; }
        
        [Required]
        public string? Status { get; set; } // Ví dụ: CONFIRMED, CANCELLED
    }
}