using FluentAssertions;
using ip_connect.DTOs.Notification;
using ip_connect.Exceptions;
using ip_connect.Models;
using ip_connect.Models.Enums;
using ip_connect.Repositories.FriendshipRepository;
using ip_connect.Services.FriendshipService;
using ip_connect.Services.NotificationService;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace ip_connect.Tests.ServiceTests
{
    public class FriendshipServiceTests
    {
        private Mock<UserManager<ApplicationUser>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        [Fact]
        public async Task SendFriendRequestAsync_ValidUsers_CreatesRequest()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var user1 = new ApplicationUser { Id = "user1", UserName = "alice", ProfilePictureUrl = "/pic1.jpg" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "bob", ProfilePictureUrl = "/pic2.jpg" };

            mockUserManager.Setup(m => m.FindByIdAsync("user1")).ReturnsAsync(user1);
            mockUserManager.Setup(m => m.FindByIdAsync("user2")).ReturnsAsync(user2);

            mockFriendshipRepo.Setup(r => r.GetFriendshipAsync("user1", "user2"))
                .ReturnsAsync((Friendship?)null);

            mockFriendshipRepo.Setup(r => r.CreateAsync(It.IsAny<Friendship>()))
                .ReturnsAsync((Friendship f) => { f.Id = 1; return f; });

            mockNotificationService.Setup(s => s.CreateFriendRequestNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            mockNotificationService.Setup(s => s.SendNotificationToUserAsync(It.IsAny<string>(), It.IsAny<NotificationDto>()))
                .Returns(Task.CompletedTask);

            mockNotificationService.Setup(s => s.GetUnreadCountAsync(It.IsAny<string>()))
                .ReturnsAsync(5);

            mockNotificationService.Setup(s => s.SendNotificationCountToUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            var result = await service.SendFriendRequestAsync("user1", "user2");

            // Assert
            result.Should().BeTrue();
            mockFriendshipRepo.Verify(r => r.CreateAsync(It.Is<Friendship>(f =>
                f.UserId == "user1" && f.FriendId == "user2" && f.Status == FriendshipStatus.Pending)), Times.Once);
        }

        [Fact]
        public async Task SendFriendRequestAsync_ValidUsers_CreatesNotification()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var user1 = new ApplicationUser { Id = "user1", UserName = "alice", ProfilePictureUrl = "/pic1.jpg" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "bob", ProfilePictureUrl = "/pic2.jpg" };

            mockUserManager.Setup(m => m.FindByIdAsync("user1")).ReturnsAsync(user1);
            mockUserManager.Setup(m => m.FindByIdAsync("user2")).ReturnsAsync(user2);

            mockFriendshipRepo.Setup(r => r.GetFriendshipAsync("user1", "user2"))
                .ReturnsAsync((Friendship?)null);

            mockFriendshipRepo.Setup(r => r.CreateAsync(It.IsAny<Friendship>()))
                .ReturnsAsync((Friendship f) => { f.Id = 1; return f; });

            mockNotificationService.Setup(s => s.CreateFriendRequestNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            mockNotificationService.Setup(s => s.SendNotificationToUserAsync(It.IsAny<string>(), It.IsAny<NotificationDto>()))
                .Returns(Task.CompletedTask);

            mockNotificationService.Setup(s => s.GetUnreadCountAsync(It.IsAny<string>()))
                .ReturnsAsync(5);

            mockNotificationService.Setup(s => s.SendNotificationCountToUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            await service.SendFriendRequestAsync("user1", "user2");

            // Assert
            mockNotificationService.Verify(s => s.CreateFriendRequestNotificationAsync("user2", "user1", 1), Times.Once);
            mockNotificationService.Verify(s => s.SendNotificationToUserAsync("user2", It.IsAny<NotificationDto>()), Times.Once);
            mockNotificationService.Verify(s => s.SendNotificationCountToUserAsync("user2", It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public async Task SendFriendRequestAsync_SenderNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            mockUserManager.Setup(m => m.FindByIdAsync("user1")).ReturnsAsync((ApplicationUser?)null);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            Func<Task> act = async () => await service.SendFriendRequestAsync("user1", "user2");

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("*User not found*");
        }

        [Fact]
        public async Task SendFriendRequestAsync_RecipientNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var user1 = new ApplicationUser { Id = "user1", UserName = "alice" };

            mockUserManager.Setup(m => m.FindByIdAsync("user1")).ReturnsAsync(user1);
            mockUserManager.Setup(m => m.FindByIdAsync("user2")).ReturnsAsync((ApplicationUser?)null);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            Func<Task> act = async () => await service.SendFriendRequestAsync("user1", "user2");

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("*User not found*");
        }

        [Fact]
        public async Task SendFriendRequestAsync_SendToSelf_ThrowsBadRequestException()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var user1 = new ApplicationUser { Id = "user1", UserName = "alice" };

            mockUserManager.Setup(m => m.FindByIdAsync("user1")).ReturnsAsync(user1);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            Func<Task> act = async () => await service.SendFriendRequestAsync("user1", "user1");

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("*Cannot send friend request to yourself*");
        }

        [Fact]
        public async Task SendFriendRequestAsync_AlreadyPending_ThrowsBadRequestException()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var user1 = new ApplicationUser { Id = "user1", UserName = "alice" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "bob" };

            mockUserManager.Setup(m => m.FindByIdAsync("user1")).ReturnsAsync(user1);
            mockUserManager.Setup(m => m.FindByIdAsync("user2")).ReturnsAsync(user2);

            var existingFriendship = new Friendship 
            { 
                Id = 1, 
                UserId = "user1", 
                FriendId = "user2", 
                Status = FriendshipStatus.Pending 
            };

            mockFriendshipRepo.Setup(r => r.GetFriendshipAsync("user1", "user2"))
                .ReturnsAsync(existingFriendship);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            Func<Task> act = async () => await service.SendFriendRequestAsync("user1", "user2");

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("*Friend request already sent*");
        }

        [Fact]
        public async Task SendFriendRequestAsync_AlreadyFriends_ThrowsBadRequestException()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var user1 = new ApplicationUser { Id = "user1", UserName = "alice" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "bob" };

            mockUserManager.Setup(m => m.FindByIdAsync("user1")).ReturnsAsync(user1);
            mockUserManager.Setup(m => m.FindByIdAsync("user2")).ReturnsAsync(user2);

            var existingFriendship = new Friendship 
            { 
                Id = 1, 
                UserId = "user1", 
                FriendId = "user2", 
                Status = FriendshipStatus.Accepted 
            };

            mockFriendshipRepo.Setup(r => r.GetFriendshipAsync("user1", "user2"))
                .ReturnsAsync(existingFriendship);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            Func<Task> act = async () => await service.SendFriendRequestAsync("user1", "user2");

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("*Already friends*");
        }

        [Fact]
        public async Task AcceptFriendRequestAsync_ValidRequest_UpdatesStatus()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var user1 = new ApplicationUser { Id = "user1", UserName = "alice" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "bob", ProfilePictureUrl = "/pic2.jpg" };

            var friendship = new Friendship 
            { 
                Id = 1, 
                UserId = "user1", 
                User = user1,
                FriendId = "user2", 
                Friend = user2,
                Status = FriendshipStatus.Pending 
            };

            mockFriendshipRepo.Setup(r => r.GetFriendshipWithUsersAsync(1))
                .ReturnsAsync(friendship);

            mockFriendshipRepo.Setup(r => r.UpdateStatusAsync(1, FriendshipStatus.Accepted))
                .ReturnsAsync(true);

            mockUserManager.Setup(m => m.FindByIdAsync("user2")).ReturnsAsync(user2);

            mockNotificationService.Setup(s => s.CreateFriendAcceptedNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            mockNotificationService.Setup(s => s.SendNotificationToUserAsync(It.IsAny<string>(), It.IsAny<NotificationDto>()))
                .Returns(Task.CompletedTask);

            mockNotificationService.Setup(s => s.GetUnreadCountAsync(It.IsAny<string>()))
                .ReturnsAsync(3);

            mockNotificationService.Setup(s => s.SendNotificationCountToUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            mockNotificationService.Setup(s => s.GetUserNotificationsAsync("user2", 100))
                .ReturnsAsync(new List<NotificationDto>());

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            var result = await service.AcceptFriendRequestAsync("user2", 1);

            // Assert
            result.Should().BeTrue();
            mockFriendshipRepo.Verify(r => r.UpdateStatusAsync(1, FriendshipStatus.Accepted), Times.Once);
        }

        [Fact]
        public async Task AcceptFriendRequestAsync_ValidRequest_CreatesNotification()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var user1 = new ApplicationUser { Id = "user1", UserName = "alice" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "bob", ProfilePictureUrl = "/pic2.jpg" };

            var friendship = new Friendship 
            { 
                Id = 1, 
                UserId = "user1", 
                User = user1,
                FriendId = "user2", 
                Friend = user2,
                Status = FriendshipStatus.Pending 
            };

            mockFriendshipRepo.Setup(r => r.GetFriendshipWithUsersAsync(1))
                .ReturnsAsync(friendship);

            mockFriendshipRepo.Setup(r => r.UpdateStatusAsync(1, FriendshipStatus.Accepted))
                .ReturnsAsync(true);

            mockUserManager.Setup(m => m.FindByIdAsync("user2")).ReturnsAsync(user2);

            mockNotificationService.Setup(s => s.CreateFriendAcceptedNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            mockNotificationService.Setup(s => s.SendNotificationToUserAsync(It.IsAny<string>(), It.IsAny<NotificationDto>()))
                .Returns(Task.CompletedTask);

            mockNotificationService.Setup(s => s.GetUnreadCountAsync(It.IsAny<string>()))
                .ReturnsAsync(3);

            mockNotificationService.Setup(s => s.SendNotificationCountToUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            mockNotificationService.Setup(s => s.GetUserNotificationsAsync("user2", 100))
                .ReturnsAsync(new List<NotificationDto>());

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            await service.AcceptFriendRequestAsync("user2", 1);

            // Assert
            mockNotificationService.Verify(s => s.CreateFriendAcceptedNotificationAsync("user1", "user2", 1), Times.Once);
            mockNotificationService.Verify(s => s.SendNotificationToUserAsync("user1", It.IsAny<NotificationDto>()), Times.Once);
        }

        [Fact]
        public async Task AcceptFriendRequestAsync_RequestNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            mockFriendshipRepo.Setup(r => r.GetFriendshipWithUsersAsync(999))
                .ReturnsAsync((Friendship?)null);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            Func<Task> act = async () => await service.AcceptFriendRequestAsync("user2", 999);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Friend request not found*");
        }

        [Fact]
        public async Task AcceptFriendRequestAsync_NotRecipient_ThrowsForbiddenException()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var user1 = new ApplicationUser { Id = "user1", UserName = "alice" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "bob" };

            var friendship = new Friendship 
            { 
                Id = 1, 
                UserId = "user1", 
                User = user1,
                FriendId = "user2", 
                Friend = user2,
                Status = FriendshipStatus.Pending 
            };

            mockFriendshipRepo.Setup(r => r.GetFriendshipWithUsersAsync(1))
                .ReturnsAsync(friendship);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            Func<Task> act = async () => await service.AcceptFriendRequestAsync("user3", 1);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*You cannot accept this friend request*");
        }

        [Fact]
        public async Task AcceptFriendRequestAsync_AlreadyAccepted_ThrowsBadRequestException()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var user1 = new ApplicationUser { Id = "user1", UserName = "alice" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "bob" };

            var friendship = new Friendship 
            { 
                Id = 1, 
                UserId = "user1", 
                User = user1,
                FriendId = "user2", 
                Friend = user2,
                Status = FriendshipStatus.Accepted 
            };

            mockFriendshipRepo.Setup(r => r.GetFriendshipWithUsersAsync(1))
                .ReturnsAsync(friendship);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            Func<Task> act = async () => await service.AcceptFriendRequestAsync("user2", 1);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("*Friend request is not pending*");
        }

        [Fact]
        public async Task RejectFriendRequestAsync_ValidRequest_DeletesFriendship()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var friendship = new Friendship 
            { 
                Id = 1, 
                UserId = "user1", 
                FriendId = "user2", 
                Status = FriendshipStatus.Pending 
            };

            mockFriendshipRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(friendship);

            mockFriendshipRepo.Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(true);

            mockNotificationService.Setup(s => s.DeleteNotificationsByEntityAsync(NotificationType.FriendRequest, 1))
                .Returns(Task.CompletedTask);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            var result = await service.RejectFriendRequestAsync("user2", 1);

            // Assert
            result.Should().BeTrue();
            mockFriendshipRepo.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task RejectFriendRequestAsync_ValidRequest_DeletesNotifications()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var friendship = new Friendship 
            { 
                Id = 1, 
                UserId = "user1", 
                FriendId = "user2", 
                Status = FriendshipStatus.Pending 
            };

            mockFriendshipRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(friendship);

            mockFriendshipRepo.Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(true);

            mockNotificationService.Setup(s => s.DeleteNotificationsByEntityAsync(NotificationType.FriendRequest, 1))
                .Returns(Task.CompletedTask);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            await service.RejectFriendRequestAsync("user2", 1);

            // Assert
            mockNotificationService.Verify(s => s.DeleteNotificationsByEntityAsync(NotificationType.FriendRequest, 1), Times.Once);
        }

        [Fact]
        public async Task RejectFriendRequestAsync_RequestNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            mockFriendshipRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Friendship?)null);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            Func<Task> act = async () => await service.RejectFriendRequestAsync("user2", 999);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Friend request not found*");
        }

        [Fact]
        public async Task RejectFriendRequestAsync_NotParticipant_ThrowsForbiddenException()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var friendship = new Friendship 
            { 
                Id = 1, 
                UserId = "user1", 
                FriendId = "user2", 
                Status = FriendshipStatus.Pending 
            };

            mockFriendshipRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(friendship);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            Func<Task> act = async () => await service.RejectFriendRequestAsync("user3", 1);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*You cannot reject this friend request*");
        }

        [Fact]
        public async Task RejectFriendRequestAsync_AlreadyAccepted_ThrowsBadRequestException()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var friendship = new Friendship 
            { 
                Id = 1, 
                UserId = "user1", 
                FriendId = "user2", 
                Status = FriendshipStatus.Accepted 
            };

            mockFriendshipRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(friendship);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            Func<Task> act = async () => await service.RejectFriendRequestAsync("user2", 1);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("*Friend request is not pending*");
        }

        [Fact]
        public async Task RemoveFriendAsync_ValidFriendship_DeletesFriendship()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var user1 = new ApplicationUser { Id = "user1", UserName = "alice", ProfilePictureUrl = "/pic1.jpg" };

            var friendship = new Friendship 
            { 
                Id = 1, 
                UserId = "user1", 
                FriendId = "user2", 
                Status = FriendshipStatus.Accepted 
            };

            mockFriendshipRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(friendship);

            mockFriendshipRepo.Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(true);

            mockUserManager.Setup(m => m.FindByIdAsync("user1"))
                .ReturnsAsync(user1);

            mockNotificationService.Setup(s => s.CreateFriendRemovedNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            mockNotificationService.Setup(s => s.SendNotificationToUserAsync(It.IsAny<string>(), It.IsAny<NotificationDto>()))
                .Returns(Task.CompletedTask);

            mockNotificationService.Setup(s => s.GetUnreadCountAsync(It.IsAny<string>()))
                .ReturnsAsync(2);

            mockNotificationService.Setup(s => s.SendNotificationCountToUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            var result = await service.RemoveFriendAsync("user1", 1);

            // Assert
            result.Should().BeTrue();
            mockFriendshipRepo.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task RemoveFriendAsync_ValidFriendship_NotifiesOtherUser()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var user1 = new ApplicationUser { Id = "user1", UserName = "alice", ProfilePictureUrl = "/pic1.jpg" };

            var friendship = new Friendship 
            { 
                Id = 1, 
                UserId = "user1", 
                FriendId = "user2", 
                Status = FriendshipStatus.Accepted 
            };

            mockFriendshipRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(friendship);

            mockFriendshipRepo.Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(true);

            mockUserManager.Setup(m => m.FindByIdAsync("user1"))
                .ReturnsAsync(user1);

            mockNotificationService.Setup(s => s.CreateFriendRemovedNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            mockNotificationService.Setup(s => s.SendNotificationToUserAsync(It.IsAny<string>(), It.IsAny<NotificationDto>()))
                .Returns(Task.CompletedTask);

            mockNotificationService.Setup(s => s.GetUnreadCountAsync(It.IsAny<string>()))
                .ReturnsAsync(2);

            mockNotificationService.Setup(s => s.SendNotificationCountToUserAsync(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            await service.RemoveFriendAsync("user1", 1);

            // Assert
            mockNotificationService.Verify(s => s.CreateFriendRemovedNotificationAsync("user2", "user1", 1), Times.Once);
            mockNotificationService.Verify(s => s.SendNotificationToUserAsync("user2", It.IsAny<NotificationDto>()), Times.Once);
        }

        [Fact]
        public async Task RemoveFriendAsync_FriendshipNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            mockFriendshipRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Friendship?)null);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            Func<Task> act = async () => await service.RemoveFriendAsync("user1", 999);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Friendship not found*");
        }

        [Fact]
        public async Task RemoveFriendAsync_NotParticipant_ThrowsForbiddenException()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var friendship = new Friendship 
            { 
                Id = 1, 
                UserId = "user1", 
                FriendId = "user2", 
                Status = FriendshipStatus.Accepted 
            };

            mockFriendshipRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(friendship);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            Func<Task> act = async () => await service.RemoveFriendAsync("user3", 1);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*You cannot remove this friendship*");
        }

        [Fact]
        public async Task GetFriendsAsync_HasFriends_ReturnsFriendDtos()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var user1 = new ApplicationUser { Id = "user1", UserName = "alice", ProfilePictureUrl = "/pic1.jpg" };
            var user2 = new ApplicationUser { Id = "user2", UserName = "bob", ProfilePictureUrl = "/pic2.jpg" };
            var user3 = new ApplicationUser { Id = "user3", UserName = "charlie", ProfilePictureUrl = "/pic3.jpg" };

            var friendships = new List<Friendship>
            {
                new Friendship 
                { 
                    Id = 1, 
                    UserId = "user1", 
                    User = user1,
                    FriendId = "user2", 
                    Friend = user2,
                    Status = FriendshipStatus.Accepted,
                    RequestedAt = DateTime.UtcNow.AddDays(-5),
                    AcceptedAt = DateTime.UtcNow.AddDays(-4)
                },
                new Friendship 
                { 
                    Id = 2, 
                    UserId = "user3", 
                    User = user3,
                    FriendId = "user1", 
                    Friend = user1,
                    Status = FriendshipStatus.Accepted,
                    RequestedAt = DateTime.UtcNow.AddDays(-2),
                    AcceptedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            mockFriendshipRepo.Setup(r => r.GetFriendsAsync("user1"))
                .ReturnsAsync(friendships);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            var result = await service.GetFriendsAsync("user1");

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(f => f.UserId == "user2" && f.Username == "bob");
            result.Should().Contain(f => f.UserId == "user3" && f.Username == "charlie");
        }

        [Fact]
        public async Task GetFriendsAsync_NoFriends_ReturnsEmptyList()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            mockFriendshipRepo.Setup(r => r.GetFriendsAsync("user1"))
                .ReturnsAsync(new List<Friendship>());

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            var result = await service.GetFriendsAsync("user1");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPendingRequestsAsync_HasRequests_ReturnsPendingRequestDtos()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var user2 = new ApplicationUser { Id = "user2", UserName = "bob", ProfilePictureUrl = "/pic2.jpg" };
            var user3 = new ApplicationUser { Id = "user3", UserName = "charlie", ProfilePictureUrl = "/pic3.jpg" };

            var requests = new List<Friendship>
            {
                new Friendship 
                { 
                    Id = 1, 
                    UserId = "user2", 
                    User = user2,
                    FriendId = "user1",
                    Status = FriendshipStatus.Pending,
                    RequestedAt = DateTime.UtcNow.AddHours(-2)
                },
                new Friendship 
                { 
                    Id = 2, 
                    UserId = "user3", 
                    User = user3,
                    FriendId = "user1",
                    Status = FriendshipStatus.Pending,
                    RequestedAt = DateTime.UtcNow.AddHours(-1)
                }
            };

            mockFriendshipRepo.Setup(r => r.GetPendingRequestsAsync("user1"))
                .ReturnsAsync(requests);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            var result = await service.GetPendingRequestsAsync("user1");

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(r => r.UserId == "user2" && r.Username == "bob");
            result.Should().Contain(r => r.UserId == "user3" && r.Username == "charlie");
        }

        [Fact]
        public async Task GetPendingRequestsAsync_NoRequests_ReturnsEmptyList()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            mockFriendshipRepo.Setup(r => r.GetPendingRequestsAsync("user1"))
                .ReturnsAsync(new List<Friendship>());

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            var result = await service.GetPendingRequestsAsync("user1");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetFriendshipStatusAsync_NoFriendship_ReturnsCorrectStatus()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            mockFriendshipRepo.Setup(r => r.GetFriendshipAsync("user1", "user2"))
                .ReturnsAsync((Friendship?)null);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            var result = await service.GetFriendshipStatusAsync("user1", "user2");

            // Assert
            result.AreFriends.Should().BeFalse();
            result.HasPendingRequest.Should().BeFalse();
            result.IsSentByMe.Should().BeFalse();
            result.FriendshipId.Should().BeNull();
        }

        [Fact]
        public async Task GetFriendshipStatusAsync_PendingSentByMe_ReturnsCorrectStatus()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var friendship = new Friendship 
            { 
                Id = 1, 
                UserId = "user1", 
                FriendId = "user2", 
                Status = FriendshipStatus.Pending 
            };

            mockFriendshipRepo.Setup(r => r.GetFriendshipAsync("user1", "user2"))
                .ReturnsAsync(friendship);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            var result = await service.GetFriendshipStatusAsync("user1", "user2");

            // Assert
            result.AreFriends.Should().BeFalse();
            result.HasPendingRequest.Should().BeTrue();
            result.IsSentByMe.Should().BeTrue();
            result.FriendshipId.Should().Be(1);
        }

        [Fact]
        public async Task GetFriendshipStatusAsync_PendingSentByOther_ReturnsCorrectStatus()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var friendship = new Friendship 
            { 
                Id = 1, 
                UserId = "user2", 
                FriendId = "user1", 
                Status = FriendshipStatus.Pending 
            };

            mockFriendshipRepo.Setup(r => r.GetFriendshipAsync("user1", "user2"))
                .ReturnsAsync(friendship);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            var result = await service.GetFriendshipStatusAsync("user1", "user2");

            // Assert
            result.AreFriends.Should().BeFalse();
            result.HasPendingRequest.Should().BeTrue();
            result.IsSentByMe.Should().BeFalse();
            result.FriendshipId.Should().Be(1);
        }

        [Fact]
        public async Task GetFriendshipStatusAsync_Accepted_ReturnsCorrectStatus()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            var friendship = new Friendship 
            { 
                Id = 1, 
                UserId = "user1", 
                FriendId = "user2", 
                Status = FriendshipStatus.Accepted 
            };

            mockFriendshipRepo.Setup(r => r.GetFriendshipAsync("user1", "user2"))
                .ReturnsAsync(friendship);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            var result = await service.GetFriendshipStatusAsync("user1", "user2");

            // Assert
            result.AreFriends.Should().BeTrue();
            result.HasPendingRequest.Should().BeFalse();
            result.IsSentByMe.Should().BeTrue();
            result.FriendshipId.Should().Be(1);
        }

        [Fact]
        public async Task AreFriendsAsync_AreFriends_ReturnsTrue()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            mockFriendshipRepo.Setup(r => r.AreFriendsAsync("user1", "user2"))
                .ReturnsAsync(true);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            var result = await service.AreFriendsAsync("user1", "user2");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task AreFriendsAsync_NotFriends_ReturnsFalse()
        {
            // Arrange
            var mockFriendshipRepo = new Mock<IFriendshipRepository>();
            var mockNotificationService = new Mock<INotificationService>();
            var mockUserManager = CreateMockUserManager();

            mockFriendshipRepo.Setup(r => r.AreFriendsAsync("user1", "user2"))
                .ReturnsAsync(false);

            var service = new FriendshipService(mockFriendshipRepo.Object, mockNotificationService.Object, mockUserManager.Object);

            // Act
            var result = await service.AreFriendsAsync("user1", "user2");

            // Assert
            result.Should().BeFalse();
        }
    }
}