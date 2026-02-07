using FluentAssertions;
using ip_connect.Models;
using ip_connect.Repositories.AlbumRepository;
using ip_connect.Tests.Helpers;

namespace ip_connect.Tests.RepositoryTests
{
    public class AlbumRepositoryTests
    {
        [Fact]
        public async Task GetUserAlbumsAsync_HasAlbums_ReturnsAlbumsOrderedByCreatedAt()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new AlbumRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var album1 = new Album
            {
                UserId = user.Id,
                Name = "Old Album",
                Description = "First",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };

            var album2 = new Album
            {
                UserId = user.Id,
                Name = "Recent Album",
                Description = "Second",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };

            var album3 = new Album
            {
                UserId = user.Id,
                Name = "Newest Album",
                Description = "Third",
                CreatedAt = DateTime.UtcNow
            };

            context.Albums.AddRange(album1, album2, album3);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUserAlbumsAsync(user.Id);

            // Assert
            result.Should().HaveCount(3);
            result[0].Name.Should().Be("Newest Album");
            result[1].Name.Should().Be("Recent Album");
            result[2].Name.Should().Be("Old Album");
            result.Should().BeInDescendingOrder(a => a.CreatedAt);
        }

        [Fact]
        public async Task GetUserAlbumsAsync_NoAlbums_ReturnsEmptyList()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new AlbumRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUserAlbumsAsync(user.Id);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserAlbumsAsync_OnlyReturnsUserAlbums()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new AlbumRepository(context);

            var user1 = new ApplicationUser { Id = "user1", UserName = "user1" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "user2" };
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();

            var user1Album = new Album { UserId = user1.Id, Name = "User 1 Album", CreatedAt = DateTime.UtcNow };
            var user2Album = new Album { UserId = user2.Id, Name = "User 2 Album", CreatedAt = DateTime.UtcNow };

            context.Albums.AddRange(user1Album, user2Album);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUserAlbumsAsync(user1.Id);

            // Assert
            result.Should().HaveCount(1);
            result[0].Name.Should().Be("User 1 Album");
        }

        [Fact]
        public async Task IsAlbumOwnerAsync_UserOwnsAlbum_ReturnsTrue()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new AlbumRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var album = new Album { UserId = user.Id, Name = "My Album", CreatedAt = DateTime.UtcNow };
            context.Albums.Add(album);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.IsAlbumOwnerAsync(album.Id, user.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsAlbumOwnerAsync_UserDoesNotOwnAlbum_ReturnsFalse()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new AlbumRepository(context);

            var owner = new ApplicationUser { Id = "owner", UserName = "owner" };
            var otherUser = new ApplicationUser { Id = "other", UserName = "other" };
            context.Users.AddRange(owner, otherUser);
            await context.SaveChangesAsync();

            var album = new Album { UserId = owner.Id, Name = "Owner's Album", CreatedAt = DateTime.UtcNow };
            context.Albums.Add(album);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.IsAlbumOwnerAsync(album.Id, otherUser.Id);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetAlbumByIdAsync_AlbumExists_ReturnsAlbum()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new AlbumRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var album = new Album
            {
                UserId = user.Id,
                Name = "Test Album",
                Description = "Test Description",
                CreatedAt = DateTime.UtcNow
            };
            context.Albums.Add(album);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAlbumByIdAsync(album.Id);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Test Album");
            result.Description.Should().Be("Test Description");
        }

        [Fact]
        public async Task GetAlbumByIdAsync_AlbumDoesNotExist_ReturnsNull()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new AlbumRepository(context);

            // Act
            var result = await repository.GetAlbumByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }
    }
}