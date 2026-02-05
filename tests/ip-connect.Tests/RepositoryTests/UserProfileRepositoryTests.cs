using ip_connect.Models;
using ip_connect.Tests.Helpers;
using FluentAssertions;
using ip_connect.Repositories.UserProfileRepository;

namespace ip_connect.Tests.RepositoryTests
{
    public class UserProfileRepositoryTests
    {
        [Fact]
        public async Task GetByUserIdAsync_ExistingUser_ReturnsProfile()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new UserProfileRepository(context);
            
            var profile = new UserProfile
            {
                UserId = "user123",
                DisplayName = "Test User",
                Bio = "Test bio",
                CreatedAt = DateTime.UtcNow
            };
            
            context.UserProfiles.Add(profile);
            await context.SaveChangesAsync();
            
            // Act
            var result = await repository.GetByUserIdAsync("user123");
            
            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be("user123");
            result.DisplayName.Should().Be("Test User");
            result.Bio.Should().Be("Test bio");
        }
        
        [Fact]
        public async Task GetByUserIdAsync_NonExistingUser_ReturnsNull()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new UserProfileRepository(context);
            
            // Act
            var result = await repository.GetByUserIdAsync("nonexistent");
            
            // Assert
            result.Should().BeNull();
        }
    }
}