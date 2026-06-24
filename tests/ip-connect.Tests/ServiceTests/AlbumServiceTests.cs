using FluentAssertions;
using ip_connect.DTOs;
using ip_connect.Exceptions;
using ip_connect.Models;
using ip_connect.Repositories.AlbumRepository;
using ip_connect.Repositories.FriendshipRepository;
using ip_connect.Services.Albums;
using Moq;

namespace ip_connect.Tests.ServiceTests
{
    public class AlbumServiceTests
    {
        [Fact]
        public async Task GetUserAlbumsAsync_HasAlbums_ReturnsAlbumDtos()
        {
            // Arrange
            var mockAlbumRepo = new Mock<IAlbumRepository>();
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();

            var albums = new List<Album>
            {
                new Album { Id = 1, UserId = "user1", Name = "Vacation", Description = "Summer trip", CreatedAt = DateTime.UtcNow.AddDays(-5) },
                new Album { Id = 2, UserId = "user1", Name = "Family", Description = "Family photos", CreatedAt = DateTime.UtcNow }
            };

            mockAlbumRepo.Setup(r => r.GetUserAlbumsAsync("user1"))
                .ReturnsAsync(albums);

            var service = new AlbumService(mockAlbumRepo.Object, mockFriendshipRepo.Object);

            // Act
            var result = await service.GetUserAlbumsAsync("user1");

            // Assert
            result.Should().HaveCount(2);
            result[0].Name.Should().Be("Vacation");
            result[1].Name.Should().Be("Family");
            result.All(a => a.UserId == "user1").Should().BeTrue();
        }

        [Fact]
        public async Task GetUserAlbumsAsync_NoAlbums_ReturnsEmptyList()
        {
            // Arrange
            var mockAlbumRepo = new Mock<IAlbumRepository>();
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();

            mockAlbumRepo.Setup(r => r.GetUserAlbumsAsync("user1"))
                .ReturnsAsync(new List<Album>());

            var service = new AlbumService(mockAlbumRepo.Object, mockFriendshipRepo.Object);

            // Act
            var result = await service.GetUserAlbumsAsync("user1");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAlbumByIdAsync_AlbumExists_ReturnsAlbumDto()
        {
            // Arrange
            var mockAlbumRepo = new Mock<IAlbumRepository>();
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();

            var album = new Album
            {
                Id = 1,
                UserId = "user1",
                Name = "Vacation",
                Description = "Trip",
                CreatedAt = DateTime.UtcNow
            };

            mockAlbumRepo.Setup(r => r.GetAlbumByIdAsync(1))
                .ReturnsAsync(album);

            var service = new AlbumService(mockAlbumRepo.Object, mockFriendshipRepo.Object);

            // Act
            var result = await service.GetAlbumByIdAsync(1, "user1");

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Name.Should().Be("Vacation");
        }

        [Fact]
        public async Task GetAlbumByIdAsync_AlbumDoesNotExist_ReturnsNull()
        {
            // Arrange
            var mockAlbumRepo = new Mock<IAlbumRepository>();
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();

            mockAlbumRepo.Setup(r => r.GetAlbumByIdAsync(999))
                .ReturnsAsync((Album?)null);

            var service = new AlbumService(mockAlbumRepo.Object, mockFriendshipRepo.Object);

            // Act & Assert
            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.GetAlbumByIdAsync(999, "user1"));
        }

        [Fact]
        public async Task CreateAlbumAsync_ValidData_ReturnsCreatedAlbumDto()
        {
            // Arrange
            var mockAlbumRepo = new Mock<IAlbumRepository>();
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();

            var createDto = new CreateAlbumDto { Name = "New Album", Description = "My new album" };

            mockAlbumRepo.Setup(r => r.GetUserAlbumsAsync("user1"))
                .ReturnsAsync(new List<Album>());

            mockAlbumRepo.Setup(r => r.CreateAsync(It.IsAny<Album>()))
                .ReturnsAsync((Album a) =>
                {
                    a.Id = 1;
                    return a;
                });

            var service = new AlbumService(mockAlbumRepo.Object, mockFriendshipRepo.Object);

            // Act
            var result = await service.CreateAlbumAsync("user1", createDto);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("New Album");
            result.Description.Should().Be("My new album");
            result.UserId.Should().Be("user1");
            mockAlbumRepo.Verify(r => r.CreateAsync(It.IsAny<Album>()), Times.Once);
        }

        [Fact]
        public async Task CreateAlbumAsync_DuplicateName_ThrowsBadRequestException()
        {
            // Arrange
            var mockAlbumRepo = new Mock<IAlbumRepository>();
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();

            var existingAlbums = new List<Album>
            {
                new Album { Id = 1, UserId = "user1", Name = "Vacation", CreatedAt = DateTime.UtcNow }
            };

            var createDto = new CreateAlbumDto { Name = "Vacation", Description = "Another vacation" };

            mockAlbumRepo.Setup(r => r.GetUserAlbumsAsync("user1"))
                .ReturnsAsync(existingAlbums);

            var service = new AlbumService(mockAlbumRepo.Object, mockFriendshipRepo.Object);

            // Act
            Func<Task> act = async () => await service.CreateAlbumAsync("user1", createDto);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("*already have an album*");
            mockAlbumRepo.Verify(r => r.CreateAsync(It.IsAny<Album>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAlbumAsync_ValidOwner_ReturnsUpdatedAlbumDto()
        {
            // Arrange
            var mockAlbumRepo = new Mock<IAlbumRepository>();
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();

            var existingAlbum = new Album { Id = 1, UserId = "user1", Name = "Old Name", Description = "Old desc", CreatedAt = DateTime.UtcNow };
            var updateDto = new UpdateAlbumDto { Name = "New Name", Description = "New desc" };

            mockAlbumRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingAlbum);

            mockAlbumRepo.Setup(r => r.UpdateAsync(It.IsAny<Album>()))
                .ReturnsAsync((Album a) => a);

            var service = new AlbumService(mockAlbumRepo.Object, mockFriendshipRepo.Object);

            // Act
            var result = await service.UpdateAlbumAsync(1, "user1", updateDto);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("New Name");
            result.Description.Should().Be("New desc");
        }

        [Fact]
        public async Task UpdateAlbumAsync_AlbumNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var mockAlbumRepo = new Mock<IAlbumRepository>();
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();

            var updateDto = new UpdateAlbumDto { Name = "New Name", Description = "New desc" };

            mockAlbumRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Album?)null);

            var service = new AlbumService(mockAlbumRepo.Object, mockFriendshipRepo.Object);

            // Act
            Func<Task> act = async () => await service.UpdateAlbumAsync(999, "user1", updateDto);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateAlbumAsync_NotOwner_ThrowsForbiddenException()
        {
            // Arrange
            var mockAlbumRepo = new Mock<IAlbumRepository>();
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();

            var existingAlbum = new Album { Id = 1, UserId = "owner", Name = "Album", CreatedAt = DateTime.UtcNow };
            var updateDto = new UpdateAlbumDto { Name = "Hacked Name" };

            mockAlbumRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingAlbum);

            var service = new AlbumService(mockAlbumRepo.Object, mockFriendshipRepo.Object);

            // Act
            Func<Task> act = async () => await service.UpdateAlbumAsync(1, "attacker", updateDto);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
            mockAlbumRepo.Verify(r => r.UpdateAsync(It.IsAny<Album>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAlbumAsync_ValidOwner_DeletesAlbum()
        {
            // Arrange
            var mockAlbumRepo = new Mock<IAlbumRepository>();
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();

            var existingAlbum = new Album { Id = 1, UserId = "user1", Name = "Album", CreatedAt = DateTime.UtcNow };

            mockAlbumRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingAlbum);

            mockAlbumRepo.Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(true);

            var service = new AlbumService(mockAlbumRepo.Object, mockFriendshipRepo.Object);

            // Act
            await service.DeleteAlbumAsync(1, "user1");

            // Assert
            mockAlbumRepo.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteAlbumAsync_AlbumNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var mockAlbumRepo = new Mock<IAlbumRepository>();
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();

            mockAlbumRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Album?)null);

            var service = new AlbumService(mockAlbumRepo.Object, mockFriendshipRepo.Object);

            // Act
            Func<Task> act = async () => await service.DeleteAlbumAsync(999, "user1");

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task DeleteAlbumAsync_NotOwner_ThrowsForbiddenException()
        {
            // Arrange
            var mockAlbumRepo = new Mock<IAlbumRepository>();
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();

            var existingAlbum = new Album { Id = 1, UserId = "owner", Name = "Album", CreatedAt = DateTime.UtcNow };

            mockAlbumRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingAlbum);

            var service = new AlbumService(mockAlbumRepo.Object, mockFriendshipRepo.Object);

            // Act
            Func<Task> act = async () => await service.DeleteAlbumAsync(1, "attacker");

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
            mockAlbumRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetAlbumByIdAsync_Owner_ReturnsAlbum()
        {
            // Arrange
            var mockAlbumRepo = new Mock<IAlbumRepository>();
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();

            var album = new Album { Id = 1, UserId = "user1", Name = "My Album", Description = "Private", CreatedAt = DateTime.UtcNow };

            mockAlbumRepo.Setup(r => r.GetAlbumByIdAsync(1))
                .ReturnsAsync(album);

            var service = new AlbumService(mockAlbumRepo.Object, mockFriendshipRepo.Object);

            // Act
            var result = await service.GetAlbumByIdAsync(1, "user1");

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("My Album");
            mockFriendshipRepo.Verify(r => r.AreFriendsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetAlbumByIdAsync_Friend_ReturnsAlbum()
        {
            // Arrange
            var mockAlbumRepo = new Mock<IAlbumRepository>();
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();

            var album = new Album { Id = 1, UserId = "owner", Name = "Friend Album", CreatedAt = DateTime.UtcNow };

            mockAlbumRepo.Setup(r => r.GetAlbumByIdAsync(1))
                .ReturnsAsync(album);

            mockFriendshipRepo.Setup(r => r.AreFriendsAsync("viewer", "owner"))
                .ReturnsAsync(true);

            var service = new AlbumService(mockAlbumRepo.Object, mockFriendshipRepo.Object);

            // Act
            var result = await service.GetAlbumByIdAsync(1, "viewer");

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Friend Album");
        }

        [Fact]
        public async Task GetAlbumByIdAsync_Stranger_ThrowsForbiddenException()
        {
            // Arrange
            var mockAlbumRepo = new Mock<IAlbumRepository>();
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();

            var album = new Album { Id = 1, UserId = "owner", Name = "Private Album", CreatedAt = DateTime.UtcNow };

            mockAlbumRepo.Setup(r => r.GetAlbumByIdAsync(1))
                .ReturnsAsync(album);

            mockFriendshipRepo.Setup(r => r.AreFriendsAsync("stranger", "owner"))
                .ReturnsAsync(false);

            var service = new AlbumService(mockAlbumRepo.Object, mockFriendshipRepo.Object);

            // Act
            Func<Task> act = async () => await service.GetAlbumByIdAsync(1, "stranger");

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task GetAlbumByIdAsync_AlbumNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var mockAlbumRepo = new Mock<IAlbumRepository>();
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();

            mockAlbumRepo.Setup(r => r.GetAlbumByIdAsync(999))
                .ReturnsAsync((Album?)null);

            var service = new AlbumService(mockAlbumRepo.Object, mockFriendshipRepo.Object);

            // Act
            Func<Task> act = async () => await service.GetAlbumByIdAsync(999, "user1");

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
