using FluentAssertions;
using ip_connect.Dtos.UserProfile;
using ip_connect.Models;
using ip_connect.Repositories.UserProfileRepository;
using ip_connect.Services.UserProfileService;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace ip_connect.Tests.ServiceTests
{
    public class UserProfileServiceTests
    {
        private Mock<UserManager<ApplicationUser>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        [Fact]
        public async Task GetOrCreateProfileAsync_ProfileExists_ReturnsExistingProfile()
        {
            // Arrange
            var mockRepo = new Mock<IUserProfileRepository>();
            var mockUserManager = CreateMockUserManager();

            var existingProfile = new UserProfile
            {
                Id = 1,
                UserId = "user1",
                DisplayName = "John Doe",
                Bio = "Test bio",
                Gender = "Male",
                BirthDate = new DateTime(1990, 1, 1),
                CreatedAt = DateTime.UtcNow
            };

            var user = new ApplicationUser
            {
                Id = "user1",
                UserName = "johndoe",
                ProfilePictureUrl = "/images/john.jpg"
            };

            mockRepo.Setup(r => r.GetByUserIdAsync("user1"))
                .ReturnsAsync(existingProfile);

            mockUserManager.Setup(m => m.FindByIdAsync("user1"))
                .ReturnsAsync(user);

            var service = new UserProfileService(mockRepo.Object, mockUserManager.Object);

            // Act
            var result = await service.GetOrCreateProfileAsync("user1", "Default Name");

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be("user1");
            result.DisplayName.Should().Be("John Doe");
            result.Bio.Should().Be("Test bio");
            result.ProfilePictureUrl.Should().Be("/images/john.jpg");
        }

        [Fact]
        public async Task GetOrCreateProfileAsync_ProfileDoesNotExist_CreatesNewProfile()
        {
            // Arrange
            var mockRepo = new Mock<IUserProfileRepository>();
            var mockUserManager = CreateMockUserManager();

            var user = new ApplicationUser
            {
                Id = "user1",
                UserName = "johndoe",
                ProfilePictureUrl = "/images/default.jpg"
            };

            mockRepo.Setup(r => r.GetByUserIdAsync("user1"))
                .ReturnsAsync((UserProfile?)null);

            mockRepo.Setup(r => r.CreateAsync(It.IsAny<UserProfile>()))
                .ReturnsAsync((UserProfile profile) => profile);

            mockUserManager.Setup(m => m.FindByIdAsync("user1"))
                .ReturnsAsync(user);

            var service = new UserProfileService(mockRepo.Object, mockUserManager.Object);

            // Act
            var result = await service.GetOrCreateProfileAsync("user1", "New User");

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be("user1");
            result.DisplayName.Should().Be("New User");
            mockRepo.Verify(r => r.CreateAsync(It.IsAny<UserProfile>()), Times.Once);
        }

        [Fact]
        public async Task UpdateProfileAsync_ValidDto_UpdatesProfile()
        {
            // Arrange
            var mockRepo = new Mock<IUserProfileRepository>();
            var mockUserManager = CreateMockUserManager();

            var existingProfile = new UserProfile
            {
                Id = 1,
                UserId = "user1",
                DisplayName = "Old Name",
                Bio = "Old bio"
            };

            var updateDto = new UserProfileDto
            {
                Id = 1,
                UserId = "user1",
                DisplayName = "New Name",
                Bio = "New bio",
                Gender = "Male",
                BirthDate = new DateTime(1990, 1, 1)
            };

            mockRepo.Setup(r => r.GetByUserIdAsync("user1"))
                .ReturnsAsync(existingProfile);

            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<UserProfile>()))
                .ReturnsAsync((UserProfile p) => p);

            var service = new UserProfileService(mockRepo.Object, mockUserManager.Object);

            // Act
            await service.UpdateProfileAsync(updateDto);

            // Assert
            mockRepo.Verify(r => r.UpdateAsync(It.Is<UserProfile>(
                p => p.DisplayName == "New Name" && p.Bio == "New bio"
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateProfilePictureAsync_UserExists_UpdatesPictureUrl()
        {
            // Arrange
            var mockRepo = new Mock<IUserProfileRepository>();
            var mockUserManager = CreateMockUserManager();

            var user = new ApplicationUser
            {
                Id = "user1",
                UserName = "johndoe",
                ProfilePictureUrl = "/images/old.jpg"
            };

            mockUserManager.Setup(m => m.FindByIdAsync("user1"))
                .ReturnsAsync(user);

            mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var service = new UserProfileService(mockRepo.Object, mockUserManager.Object);

            // Act
            var result = await service.UpdateProfilePictureAsync("user1", "/images/new.jpg");

            // Assert
            result.Should().BeTrue();
            mockUserManager.Verify(m => m.UpdateAsync(It.Is<ApplicationUser>(
                u => u.ProfilePictureUrl == "/images/new.jpg"
            )), Times.Once);
        }

        [Fact]
        public async Task UpdateProfilePictureAsync_UserDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var mockRepo = new Mock<IUserProfileRepository>();
            var mockUserManager = CreateMockUserManager();

            mockUserManager.Setup(m => m.FindByIdAsync("nonexistent"))
                .ReturnsAsync((ApplicationUser?)null);

            var service = new UserProfileService(mockRepo.Object, mockUserManager.Object);

            // Act
            var result = await service.UpdateProfilePictureAsync("nonexistent", "/images/new.jpg");

            // Assert
            result.Should().BeFalse();
            mockUserManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }
    }
}
