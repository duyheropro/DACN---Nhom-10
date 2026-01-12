namespace BookingTourAPI.Models
{
    public class HotelBooking
    {
        public int Id { get; set; }
        public string? AmadeusOrderId { get; set; } // Mã đơn đặt của Amadeus
        public string? UserId { get; set; }
        public int HotelId { get; set; }

        public virtual Hotel? Hotel { get; set; }
        
        public DateTime BookingDate { get; set; }
        public double TotalPrice { get; set; }
        public string? Status { get; set; } // Confirmed, Canceled, Pending
    }
}