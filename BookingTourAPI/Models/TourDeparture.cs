using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingTourAPI.Models
{
    public class TourDeparture
    {
        public int Id { get; set; }
        
        public int TourPackageId { get; set; }
        [ForeignKey("TourPackageId")]
        public virtual TourPackage? TourPackage { get; set; }

        [Required]
        public DateTime StartDate { get; set; } // Ngày đi
        
        public DateTime? EndDate { get; set; } // Ngày về (tự tính hoặc nhập)

        // Giá cho từng đối tượng (nếu khác nhau theo ngày)
        public decimal PriceAdult { get; set; }
        public decimal PriceChild { get; set; } // Trẻ em
        public decimal PriceInfant { get; set; } // Em bé (dưới 2 tuổi)
        
        public int AvailableSeats { get; set; } = 20; // Số chỗ còn nhận
        
        // Thông tin chuyến bay (Optional - để hiển thị giống Vietravel)
        public string? Airline { get; set; } // Vd: Vietnam Airlines
        public string? FlightNumberOut { get; set; } // VN641
        public string? FlightNumberIn { get; set; } // VN640
        public string? FlightTimeOut { get; set; } // 10:25 - 15:00
        public string? FlightTimeIn { get; set; } // 16:00 - 18:40
    }
}   