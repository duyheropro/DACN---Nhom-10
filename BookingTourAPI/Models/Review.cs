using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingTourAPI.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        // Người đánh giá
        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        // Tour được đánh giá
        public int TourPackageId { get; set; }
        [ForeignKey(nameof(TourPackageId))]
        public TourPackage? TourPackage { get; set; }

        // Điểm và nội dung
        [Range(1, 5)]
        public int Rating { get; set; }   // 1–5 sao

        [MaxLength(2000)]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
