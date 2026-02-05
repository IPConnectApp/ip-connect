using System.Net;
using System.Net.Mail;

namespace ip_connect.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public EmailService(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        public async Task SendWelcomeEmailAsync(string toEmail, string username)
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["Email:SmtpUsername"];
            var smtpPassword = _configuration["Email:SmtpPassword"];
            var fromEmail = _configuration["Email:FromEmail"];
            var fromName = _configuration["Email:FromName"];

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail ?? "", fromName ?? "IP Connect"),
                Subject = "Welcome to IP Connect!",
                Body = await GetWelcomeEmailHtmlAsync(username),
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }

        private async Task<string> GetWelcomeEmailHtmlAsync(string username)
        {
            var templatePath = Path.Combine(_environment.WebRootPath, "email-templates", "welcome-email.html");
            var template = await File.ReadAllTextAsync(templatePath);

            // Replace placeholders
            var html = template
                .Replace("{{USERNAME}}", username)
                .Replace("{{PROFILE_URL}}", "https://ipconnect-app-brahf2cjgqh5g8af.westeurope-01.azurewebsites.net/profile");

            return html;
        }
    }
}