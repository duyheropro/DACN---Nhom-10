// TRONG FILE MỚI: Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations; // Cần cho DateOnly nếu dùng .NET 6+

namespace BookingTourAPI.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData] // Đánh dấu dữ liệu cá nhân (tùy chọn)
        [MaxLength(100)]
        public string? FullName { get; set; }

        [PersonalData]
        public DateOnly? DateOfBirth { get; set; } // Dùng DateOnly (tốt hơn DateTime cho ngày sinh)
        // Hoặc dùng DateTime? nếu bạn dùng .NET phiên bản cũ hơn
        // public DateTime? DateOfBirth { get; set; }
    }
}