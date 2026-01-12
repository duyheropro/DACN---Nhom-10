namespace BookingTourAPI.Models
{
    public class Flight
    {
        public int Id { get; set; }
        public string FlightId { get; set; } = string.Empty;
        public string? Airline { get; set; }
        public string? Origin { get; set; }
        public string? Destination { get; set; }
        public DateTime? DepartureTime { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public string? Duration { get; set; }
        public string? TravelClass { get; set; }
        public decimal Price { get; set; }
        public string? Currency { get; set; }
    }
}
