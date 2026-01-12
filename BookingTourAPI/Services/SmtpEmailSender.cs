// File: Services/SmtpEmailSender.cs
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using BookingTourAPI.Models;
using Microsoft.Extensions.Options;

namespace BookingTourAPI.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;

        public SmtpEmailSender(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            try
            {
                using (var client = new SmtpClient())
                {
                    client.Host = _settings.SmtpServer;
                    client.Port = _settings.SmtpPort;
                    client.EnableSsl = _settings.EnableSsl;
                    client.Credentials = new NetworkCredential(_settings.UserName, _settings.Password);

                    var message = new MailMessage
                    {
                        From = new MailAddress(_settings.FromAddress, _settings.FromName),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    };

                    message.To.Add(toEmail);
                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
            }
        }
    }
}