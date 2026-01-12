// TRONG FILE: Models/TourPackage.cs
using BookingTourAPI.Models;

public class TourPackage
{
    public int Id { get; set; }
    public string Title { get; set; } // Giữ nguyên - Bắt buộc
    public string? ImageUrl { get; set; } // SỬA (thêm ?)
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public string? Duration { get; set; } // SỬA (thêm ?)
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? Area { get; set; }
    public string? Highlights { get; set; } // SỬA (thêm ?)
    public string? PolicyIncludes { get; set; } // SỬA (thêm ?)
    public string? PolicyExcludes { get; set; } // SỬA (thêm ?)

    // Liên kết đến lịch trình
    public virtual ICollection<DailyItinerary> Itineraries { get; set; } = new List<DailyItinerary>();
    // ... (các property cũ)
    public virtual ICollection<TourDeparture> Departures { get; set; } = new List<TourDeparture>();
}