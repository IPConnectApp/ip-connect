using FluentAssertions;
using ip_connect.Services.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Moq;

namespace ip_connect.Tests.ServiceTests
{
    public class EmailServiceTests
    {
        [Fact]
        public async Task SendWelcomeEmailAsync_ValidConfiguration_DoesNotThrowConfigurationError()
        {
            // Arrange
            var configData = new Dictionary<string, string>
            {
                {"Email:SmtpHost", "smtp.gmail.com"},
                {"Email:SmtpPort", "587"},
                {"Email:SmtpUsername", "test@test.com"},
                {"Email:SmtpPassword", "testpassword"},
                {"Email:FromEmail", "test@test.com"},
                {"Email:FromName", "Test Service"}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData!)
                .Build();

            var mockWebHostEnv = new Mock<IWebHostEnvironment>();
            mockWebHostEnv.Setup(e => e.WebRootPath).Returns("C:\\test\\wwwroot");

            var service = new EmailService(configuration, mockWebHostEnv.Object);

            // Act & Assert
            // Note: Will throw SmtpException because no real SMTP server
            // But won't throw NullReferenceException from missing config
            Func<Task> act = async () => await service.SendWelcomeEmailAsync("user@test.com", "TestUser");

            // We expect it to fail at SMTP level, not config level
            await act.Should().ThrowAsync<Exception>()
                .Where(e => e.GetType().Name != "NullReferenceException");
        }

        [Fact]
        public void EmailService_Constructor_WithValidConfig_DoesNotThrow()
        {
            // Arrange
            var configData = new Dictionary<string, string>
            {
                {"Email:SmtpHost", "smtp.gmail.com"},
                {"Email:SmtpPort", "587"},
                {"Email:SmtpUsername", "test@test.com"},
                {"Email:SmtpPassword", "testpassword"},
                {"Email:FromEmail", "test@test.com"},
                {"Email:FromName", "Test Service"}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData!)
                .Build();

            var mockWebHostEnv = new Mock<IWebHostEnvironment>();
            mockWebHostEnv.Setup(e => e.WebRootPath).Returns("C:\\test\\wwwroot");

            // Act
            Action act = () => new EmailService(configuration, mockWebHostEnv.Object);

            // Assert
            act.Should().NotThrow();
        }
    }
}