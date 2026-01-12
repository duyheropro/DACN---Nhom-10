using System;

namespace BookingTourAPI.Models
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public bool EnableSsl { get; set; }
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
