// Trong Models/LoginRequest.cs
using System.ComponentModel.DataAnnotations;

namespace BookingTourAPI.Models
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}