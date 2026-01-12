using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingTourAPI.Models
{
    public class TourBooking
    {
        [Key]
        public int Id { get; set; }
        
        public string OrderId { get; set; }
        public string? UserId { get; set; }

        public int TourDepartureId { get; set; }
        [ForeignKey("TourDepartureId")]
        public virtual TourDeparture? TourDeparture { get; set; }

        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;

        // --- BỔ SUNG SỐ LƯỢNG NGƯỜI ---
        public int NumAdults { get; set; } = 1;
        public int NumChildren { get; set; } = 0;
        public int NumInfants { get; set; } = 0;
        // ------------------------------

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "PENDING_CONFIRMATION";
    }
}