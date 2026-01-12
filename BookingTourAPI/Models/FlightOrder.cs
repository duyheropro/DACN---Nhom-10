using System.ComponentModel.DataAnnotations;

namespace BookingTourAPI.Models
{
    public class FlightOrder
    {
        [Key]
        public required string OrderId { get; set; }
        public string? FlightOfferId { get; set; }
        public string? UserId { get; set; } // Thêm dòng này
        public string? TravelerName { get; set; }
        public string? TravelerEmail { get; set; }
        public string? TravelerPhone { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Currency { get; set; }
        public string Status { get; set; } = "CONFIRMED";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
