//Service/EmailServices.cs
using Backend_ThriftFlowSystem.Interfaces;
using System.Net;
using System.Net.Mail;

namespace Backend_ThriftFlowSystem.Services
{
    public class EmailServices : IEmailServices
    {
        private readonly IConfiguration _config;

        public EmailServices(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(ResetPasswordEmail email)
        {
            // From appsettings / user-secrets
            var host = _config["EmailOptions:Host"]!;
            var port = int.Parse(_config["EmailOptions:Port"] ?? "587");
            var from = _config["EmailOptions:From"]!;
            var password = _config["EmailOptions:Password"]!;

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(from, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(from),
                Subject = email.Subject,
                Body = email.Body,
                IsBodyHtml = true
            };
            message.To.Add(email.Recipient);

            await client.SendMailAsync(message);
        }
    }
}
