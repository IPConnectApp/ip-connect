namespace ip_connect.Services.Email
{
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(string toEmail, string username);
    }
}