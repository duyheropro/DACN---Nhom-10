// TRONG FILE MỚI: Models/DailyItinerary.cs
public class DailyItinerary
{
    public int Id { get; set; }
    public int DayNumber { get; set; } // Ngày 1, Ngày 2...
    public string Title { get; set; } // "Ngày 1: TP.HCM - Bangkok"
    public string Description { get; set; } // Nội dung chi tiết (HTML hoặc text)

    public int TourPackageId { get; set; } // Foreign key
    public virtual TourPackage? TourPackage { get; set; }
}