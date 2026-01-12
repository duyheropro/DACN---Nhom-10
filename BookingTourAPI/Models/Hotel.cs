namespace BookingTourAPI.Models
{
    public class Hotel
    {
        public int Id { get; set; }
        public string? HotelId { get; set; }      // ID của Amadeus (ví dụ "PARBHG")
        public string? Name { get; set; }
        public string? CityCode { get; set; }
        public string? CountryCode { get; set; } // Sẽ được điền từ API v3 (hotel.address.countryCode)
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Address { get; set; }
        
        // === SỬA ĐỔI: THÊM TRƯỜNG MỚI ===
        public string? ChainCode { get; set; } // Mã chuỗi khách sạn (ví dụ "MC" cho Marriott)
    }
}