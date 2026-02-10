using FluentAssertions;
using ip_connect.DTOs.Account;
using ip_connect.Models;
using ip_connect.Services.AccountService;
using ip_connect.Services.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace ip_connect.Tests.ServiceTests
{
    public class AccountServiceTests
    {
        private Mock<UserManager<ApplicationUser>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        private Mock<SignInManager<ApplicationUser>> CreateMockSignInManager(
            Mock<UserManager<ApplicationUser>> mockUserManager)
        {
            return new Mock<SignInManager<ApplicationUser>>(
                mockUserManager.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
                null!, null!, null!, null!);
        }

        [Fact]
        public async Task RegisterAsync_ValidData_ReturnsSuccess()
        {
            // Arrange
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager);
            var mockEmailService = new Mock<IEmailService>();

            var registerDto = new RegisterDto
            {
                UserName = "testuser",
                Email = "test@test.com",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            mockUserManager.Setup(m => m.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            mockUserManager.Setup(m => m.FindByNameAsync(registerDto.UserName))
                .ReturnsAsync((ApplicationUser?)null);

            mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Success);

            mockEmailService.Setup(e => e.SendWelcomeEmailAsync(registerDto.Email, registerDto.UserName))
                .Returns(Task.CompletedTask);

            var service = new AccountService(mockUserManager.Object, mockSignInManager.Object, mockEmailService.Object);

            // Act
            var result = await service.RegisterAsync(registerDto);

            // Assert
            result.Succeeded.Should().BeTrue();
            mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_ValidData_SendsWelcomeEmail()
        {
            // Arrange
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager);
            var mockEmailService = new Mock<IEmailService>();

            var registerDto = new RegisterDto
            {
                UserName = "testuser",
                Email = "test@test.com",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            mockUserManager.Setup(m => m.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            mockUserManager.Setup(m => m.FindByNameAsync(registerDto.UserName))
                .ReturnsAsync((ApplicationUser?)null);

            mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Success);

            mockEmailService.Setup(e => e.SendWelcomeEmailAsync(registerDto.Email, registerDto.UserName))
                .Returns(Task.CompletedTask);

            var service = new AccountService(mockUserManager.Object, mockSignInManager.Object, mockEmailService.Object);

            // Act
            await service.RegisterAsync(registerDto);

            // Assert
            mockEmailService.Verify(e => e.SendWelcomeEmailAsync(registerDto.Email, registerDto.UserName), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_ReturnsFailWithDuplicateEmailError()
        {
            // Arrange
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager);
            var mockEmailService = new Mock<IEmailService>();

            var registerDto = new RegisterDto
            {
                UserName = "testuser",
                Email = "existing@test.com",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            var existingUser = new ApplicationUser { Id = "existing", Email = "existing@test.com" };

            mockUserManager.Setup(m => m.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync(existingUser);

            var service = new AccountService(mockUserManager.Object, mockSignInManager.Object, mockEmailService.Object);

            // Act
            var result = await service.RegisterAsync(registerDto);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "DuplicateEmail");
            mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_DuplicateUsername_ReturnsFailWithDuplicateUserNameError()
        {
            // Arrange
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager);
            var mockEmailService = new Mock<IEmailService>();

            var registerDto = new RegisterDto
            {
                UserName = "existinguser",
                Email = "new@test.com",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            mockUserManager.Setup(m => m.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            var existingUser = new ApplicationUser { Id = "existing", UserName = "existinguser" };

            mockUserManager.Setup(m => m.FindByNameAsync(registerDto.UserName))
                .ReturnsAsync(existingUser);

            var service = new AccountService(mockUserManager.Object, mockSignInManager.Object, mockEmailService.Object);

            // Act
            var result = await service.RegisterAsync(registerDto);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "DuplicateUserName");
            mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_CreateFails_ReturnsFailAndDoesNotSendEmail()
        {
            // Arrange
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager);
            var mockEmailService = new Mock<IEmailService>();

            var registerDto = new RegisterDto
            {
                UserName = "testuser",
                Email = "test@test.com",
                Password = "weak",
                ConfirmPassword = "weak"
            };

            mockUserManager.Setup(m => m.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            mockUserManager.Setup(m => m.FindByNameAsync(registerDto.UserName))
                .ReturnsAsync((ApplicationUser?)null);

            mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError
                {
                    Code = "PasswordTooShort",
                    Description = "Password is too short"
                }));

            var service = new AccountService(mockUserManager.Object, mockSignInManager.Object, mockEmailService.Object);

            // Act
            var result = await service.RegisterAsync(registerDto);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Code == "PasswordTooShort");
            mockEmailService.Verify(e => e.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_EmailSendFails_StillReturnsSuccess()
        {
            // Arrange
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager);
            var mockEmailService = new Mock<IEmailService>();

            var registerDto = new RegisterDto
            {
                UserName = "testuser",
                Email = "test@test.com",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            mockUserManager.Setup(m => m.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            mockUserManager.Setup(m => m.FindByNameAsync(registerDto.UserName))
                .ReturnsAsync((ApplicationUser?)null);

            mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Success);

            mockEmailService.Setup(e => e.SendWelcomeEmailAsync(registerDto.Email, registerDto.UserName))
                .ThrowsAsync(new Exception("SMTP server unavailable"));

            var service = new AccountService(mockUserManager.Object, mockSignInManager.Object, mockEmailService.Object);

            // Act
            var result = await service.RegisterAsync(registerDto);

            // Assert
            result.Succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task RegisterAsync_SetsCorrectUserProperties()
        {
            // Arrange
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager);
            var mockEmailService = new Mock<IEmailService>();

            var registerDto = new RegisterDto
            {
                UserName = "newuser",
                Email = "new@test.com",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            ApplicationUser? capturedUser = null;

            mockUserManager.Setup(m => m.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            mockUserManager.Setup(m => m.FindByNameAsync(registerDto.UserName))
                .ReturnsAsync((ApplicationUser?)null);

            mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
                .Callback<ApplicationUser, string>((user, _) => capturedUser = user)
                .ReturnsAsync(IdentityResult.Success);

            mockEmailService.Setup(e => e.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var service = new AccountService(mockUserManager.Object, mockSignInManager.Object, mockEmailService.Object);

            // Act
            await service.RegisterAsync(registerDto);

            // Assert
            capturedUser.Should().NotBeNull();
            capturedUser!.UserName.Should().Be("newuser");
            capturedUser.Email.Should().Be("new@test.com");
            capturedUser.ProfilePictureUrl.Should().Contain("profile-default.jpg");
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
        {
            // Arrange
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager);
            var mockEmailService = new Mock<IEmailService>();

            var loginDto = new LoginDto
            {
                Email = "test@test.com",
                Password = "Test123!",
                RememberMe = false
            };

            var user = new ApplicationUser { Id = "user1", UserName = "testuser", Email = "test@test.com" };

            mockUserManager.Setup(m => m.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);

            mockSignInManager.Setup(m => m.PasswordSignInAsync(user.UserName, loginDto.Password, loginDto.RememberMe, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var service = new AccountService(mockUserManager.Object, mockSignInManager.Object, mockEmailService.Object);

            // Act
            var (success, errorMessage) = await service.LoginAsync(loginDto);

            // Assert
            success.Should().BeTrue();
            errorMessage.Should().BeEmpty();
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_ReturnsFailWithMessage()
        {
            // Arrange
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager);
            var mockEmailService = new Mock<IEmailService>();

            var loginDto = new LoginDto
            {
                Email = "nonexistent@test.com",
                Password = "Test123!",
                RememberMe = false
            };

            mockUserManager.Setup(m => m.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            var service = new AccountService(mockUserManager.Object, mockSignInManager.Object, mockEmailService.Object);

            // Act
            var (success, errorMessage) = await service.LoginAsync(loginDto);

            // Assert
            success.Should().BeFalse();
            errorMessage.Should().Contain("No account found");
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ReturnsFailWithMessage()
        {
            // Arrange
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager);
            var mockEmailService = new Mock<IEmailService>();

            var loginDto = new LoginDto
            {
                Email = "test@test.com",
                Password = "WrongPassword!",
                RememberMe = false
            };

            var user = new ApplicationUser { Id = "user1", UserName = "testuser", Email = "test@test.com" };

            mockUserManager.Setup(m => m.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);

            mockSignInManager.Setup(m => m.PasswordSignInAsync(user.UserName, loginDto.Password, loginDto.RememberMe, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            var service = new AccountService(mockUserManager.Object, mockSignInManager.Object, mockEmailService.Object);

            // Act
            var (success, errorMessage) = await service.LoginAsync(loginDto);

            // Assert
            success.Should().BeFalse();
            errorMessage.Should().Contain("Invalid email or password");
        }

        [Fact]
        public async Task LoginAsync_WithRememberMe_PassesRememberMeToSignInManager()
        {
            // Arrange
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager);
            var mockEmailService = new Mock<IEmailService>();

            var loginDto = new LoginDto
            {
                Email = "test@test.com",
                Password = "Test123!",
                RememberMe = true
            };

            var user = new ApplicationUser { Id = "user1", UserName = "testuser", Email = "test@test.com" };

            mockUserManager.Setup(m => m.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);

            mockSignInManager.Setup(m => m.PasswordSignInAsync(user.UserName, loginDto.Password, true, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var service = new AccountService(mockUserManager.Object, mockSignInManager.Object, mockEmailService.Object);

            // Act
            var (success, _) = await service.LoginAsync(loginDto);

            // Assert
            success.Should().BeTrue();
            mockSignInManager.Verify(m => m.PasswordSignInAsync(user.UserName, loginDto.Password, true, false), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_ExceptionThrown_ReturnsFailWithGenericMessage()
        {
            // Arrange
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager);
            var mockEmailService = new Mock<IEmailService>();

            var loginDto = new LoginDto
            {
                Email = "test@test.com",
                Password = "Test123!",
                RememberMe = false
            };

            mockUserManager.Setup(m => m.FindByEmailAsync(loginDto.Email))
                .ThrowsAsync(new Exception("Database error"));

            var service = new AccountService(mockUserManager.Object, mockSignInManager.Object, mockEmailService.Object);

            // Act
            var (success, errorMessage) = await service.LoginAsync(loginDto);

            // Assert
            success.Should().BeFalse();
            errorMessage.Should().Contain("unexpected error");
        }

        [Fact]
        public async Task LogoutAsync_CallsSignOutAsync()
        {
            // Arrange
            var mockUserManager = CreateMockUserManager();
            var mockSignInManager = CreateMockSignInManager(mockUserManager);
            var mockEmailService = new Mock<IEmailService>();

            mockSignInManager.Setup(m => m.SignOutAsync())
                .Returns(Task.CompletedTask);

            var service = new AccountService(mockUserManager.Object, mockSignInManager.Object, mockEmailService.Object);

            // Act
            await service.LogoutAsync();

            // Assert
            mockSignInManager.Verify(m => m.SignOutAsync(), Times.Once);
        }
    }
}
