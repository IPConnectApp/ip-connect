using FluentAssertions;
using ip_connect.DTOs.Notification;
using ip_connect.Exceptions;
using ip_connect.Hubs;
using ip_connect.Models;
using ip_connect.Models.Enums;
using ip_connect.Repositories.NotificationRepository;
using ip_connect.Services.NotificationService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace ip_connect.Tests.ServiceTests
{
    public class NotificationServiceTests
    {
        private Mock<UserManager<ApplicationUser>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        private Mock<IHubContext<NotificationHub>> CreateMockHubContext()
        {
            var mockHubContext = new Mock<IHubContext<NotificationHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();

            mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

            return mockHubContext;
        }

        [Fact]
        public async Task GetUserNotificationsAsync_HasNotifications_ReturnsDtos()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            var relatedUser = new ApplicationUser { Id = "user2", UserName = "bob", ProfilePictureUrl = "/pic2.jpg" };

            var notifications = new List<Notification>
            {
                new Notification
                {
                    Id = 1,
                    UserId = "user1",
                    Type = NotificationType.FriendRequest,
                    RelatedUserId = "user2",
                    RelatedUser = relatedUser,
                    RelatedEntityId = 10,
                    Message = "bob sent you a friend request",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            mockNotificationRepo.Setup(r => r.GetUserNotificationsAsync("user1", 20))
                .ReturnsAsync(notifications);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            var result = await service.GetUserNotificationsAsync("user1", 20);

            // Assert
            result.Should().HaveCount(1);
            result[0].Id.Should().Be(1);
            result[0].Type.Should().Be(NotificationType.FriendRequest);
            result[0].RelatedUsername.Should().Be("bob");
            result[0].RelatedUserProfilePicture.Should().Be("/pic2.jpg");
        }

        [Fact]
        public async Task GetUserNotificationsAsync_NoNotifications_ReturnsEmptyList()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            mockNotificationRepo.Setup(r => r.GetUserNotificationsAsync("user1", 20))
                .ReturnsAsync(new List<Notification>());

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            var result = await service.GetUserNotificationsAsync("user1", 20);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUnreadNotificationsAsync_HasUnread_ReturnsUnreadOnly()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            var relatedUser = new ApplicationUser { Id = "user2", UserName = "bob" };

            var notifications = new List<Notification>
            {
                new Notification
                {
                    Id = 1,
                    UserId = "user1",
                    Type = NotificationType.FriendRequest,
                    RelatedUserId = "user2",
                    RelatedUser = relatedUser,
                    Message = "Friend request",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Notification
                {
                    Id = 2,
                    UserId = "user1",
                    Type = NotificationType.FriendAccepted,
                    RelatedUserId = "user2",
                    RelatedUser = relatedUser,
                    Message = "Friend accepted",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            mockNotificationRepo.Setup(r => r.GetUnreadNotificationsAsync("user1"))
                .ReturnsAsync(notifications);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            var result = await service.GetUnreadNotificationsAsync("user1");

            // Assert
            result.Should().HaveCount(2);
            result.All(n => n.IsRead == false).Should().BeTrue();
        }

        [Fact]
        public async Task GetUnreadNotificationsAsync_NoUnread_ReturnsEmptyList()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            mockNotificationRepo.Setup(r => r.GetUnreadNotificationsAsync("user1"))
                .ReturnsAsync(new List<Notification>());

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            var result = await service.GetUnreadNotificationsAsync("user1");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUnreadCountAsync_HasUnread_ReturnsCorrectCount()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            mockNotificationRepo.Setup(r => r.GetUnreadCountAsync("user1"))
                .ReturnsAsync(5);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            var result = await service.GetUnreadCountAsync("user1");

            // Assert
            result.Should().Be(5);
        }

        [Fact]
        public async Task GetUnreadCountAsync_NoUnread_ReturnsZero()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            mockNotificationRepo.Setup(r => r.GetUnreadCountAsync("user1"))
                .ReturnsAsync(0);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            var result = await service.GetUnreadCountAsync("user1");

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task MarkAsReadAsync_ValidNotification_MarksAsRead()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            var notification = new Notification
            {
                Id = 1,
                UserId = "user1",
                Type = NotificationType.FriendRequest,
                Message = "Test",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            mockNotificationRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(notification);

            mockNotificationRepo.Setup(r => r.MarkAsReadAsync(1))
                .ReturnsAsync(true);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            var result = await service.MarkAsReadAsync("user1", 1);

            // Assert
            result.Should().BeTrue();
            mockNotificationRepo.Verify(r => r.MarkAsReadAsync(1), Times.Once);
        }

        [Fact]
        public async Task MarkAsReadAsync_NotificationNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            mockNotificationRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Notification?)null);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            Func<Task> act = async () => await service.MarkAsReadAsync("user1", 999);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Notification not found*");
        }

        [Fact]
        public async Task MarkAsReadAsync_NotOwner_ThrowsForbiddenException()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            var notification = new Notification
            {
                Id = 1,
                UserId = "user1",
                Type = NotificationType.FriendRequest,
                Message = "Test",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            mockNotificationRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(notification);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            Func<Task> act = async () => await service.MarkAsReadAsync("user2", 1);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*You cannot mark this notification as read*");
        }

        [Fact]
        public async Task MarkAllAsReadAsync_HasUnread_MarksAllAsRead()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            mockNotificationRepo.Setup(r => r.MarkAllAsReadAsync("user1"))
                .ReturnsAsync(true);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            var result = await service.MarkAllAsReadAsync("user1");

            // Assert
            result.Should().BeTrue();
            mockNotificationRepo.Verify(r => r.MarkAllAsReadAsync("user1"), Times.Once);
        }

        [Fact]
        public async Task DeleteNotificationAsync_ValidNotification_DeletesNotification()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            var notification = new Notification
            {
                Id = 1,
                UserId = "user1",
                Type = NotificationType.FriendRequest,
                Message = "Test",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            mockNotificationRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(notification);

            mockNotificationRepo.Setup(r => r.DeleteAsync(1))
                .ReturnsAsync(true);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            var result = await service.DeleteNotificationAsync("user1", 1);

            // Assert
            result.Should().BeTrue();
            mockNotificationRepo.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteNotificationAsync_NotificationNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            mockNotificationRepo.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Notification?)null);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            Func<Task> act = async () => await service.DeleteNotificationAsync("user1", 999);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("*Notification not found*");
        }

        [Fact]
        public async Task DeleteNotificationAsync_NotOwner_ThrowsForbiddenException()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            var notification = new Notification
            {
                Id = 1,
                UserId = "user1",
                Type = NotificationType.FriendRequest,
                Message = "Test",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            mockNotificationRepo.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(notification);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            Func<Task> act = async () => await service.DeleteNotificationAsync("user2", 1);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>().WithMessage("*You cannot delete this notification*");
        }

        [Fact]
        public async Task CreateFriendRequestNotificationAsync_ValidData_CreatesNotification()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            var sender = new ApplicationUser { Id = "user1", UserName = "alice" };

            mockUserManager.Setup(m => m.FindByIdAsync("user1"))
                .ReturnsAsync(sender);

            mockNotificationRepo.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                .ReturnsAsync((Notification n) => n);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            await service.CreateFriendRequestNotificationAsync("user2", "user1", 5);

            // Assert
            mockNotificationRepo.Verify(r => r.CreateAsync(It.Is<Notification>(n =>
                n.UserId == "user2" &&
                n.Type == NotificationType.FriendRequest &&
                n.RelatedUserId == "user1" &&
                n.RelatedEntityId == 5 &&
                n.Message.Contains("alice"))), Times.Once);
        }

        [Fact]
        public async Task CreateFriendRequestNotificationAsync_UserNotFound_DoesNotCreateNotification()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            mockUserManager.Setup(m => m.FindByIdAsync("user1"))
                .ReturnsAsync((ApplicationUser?)null);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            await service.CreateFriendRequestNotificationAsync("user2", "user1", 5);

            // Assert
            mockNotificationRepo.Verify(r => r.CreateAsync(It.IsAny<Notification>()), Times.Never);
        }

        [Fact]
        public async Task CreateFriendAcceptedNotificationAsync_ValidData_CreatesNotification()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            var acceptedBy = new ApplicationUser { Id = "user2", UserName = "bob" };

            mockUserManager.Setup(m => m.FindByIdAsync("user2"))
                .ReturnsAsync(acceptedBy);

            mockNotificationRepo.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                .ReturnsAsync((Notification n) => n);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            await service.CreateFriendAcceptedNotificationAsync("user1", "user2", 5);

            // Assert
            mockNotificationRepo.Verify(r => r.CreateAsync(It.Is<Notification>(n =>
                n.UserId == "user1" &&
                n.Type == NotificationType.FriendAccepted &&
                n.RelatedUserId == "user2" &&
                n.RelatedEntityId == 5 &&
                n.Message.Contains("bob"))), Times.Once);
        }

        [Fact]
        public async Task CreateFriendRemovedNotificationAsync_ValidData_CreatesNotification()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            var removedBy = new ApplicationUser { Id = "user1", UserName = "alice" };

            mockUserManager.Setup(m => m.FindByIdAsync("user1"))
                .ReturnsAsync(removedBy);

            mockNotificationRepo.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                .ReturnsAsync((Notification n) => n);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            await service.CreateFriendRemovedNotificationAsync("user2", "user1", 5);

            // Assert
            mockNotificationRepo.Verify(r => r.CreateAsync(It.Is<Notification>(n =>
                n.UserId == "user2" &&
                n.Type == NotificationType.FriendRemoved &&
                n.RelatedUserId == "user1" &&
                n.RelatedEntityId == 5 &&
                n.Message.Contains("alice"))), Times.Once);
        }

        [Fact]
        public async Task DeleteNotificationsByEntityAsync_HasNotifications_DeletesAll()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            var notifications = new List<Notification>
            {
                new Notification { Id = 1, UserId = "user1", Type = NotificationType.FriendRequest, RelatedEntityId = 5, Message = "Test", IsRead = false, CreatedAt = DateTime.UtcNow },
                new Notification { Id = 2, UserId = "user2", Type = NotificationType.FriendRequest, RelatedEntityId = 5, Message = "Test", IsRead = false, CreatedAt = DateTime.UtcNow }
            };

            mockNotificationRepo.Setup(r => r.GetNotificationsByTypeAsync(NotificationType.FriendRequest, null))
                .ReturnsAsync(notifications);

            mockNotificationRepo.Setup(r => r.DeleteAsync(It.IsAny<int>()))
                .ReturnsAsync(true);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            await service.DeleteNotificationsByEntityAsync(NotificationType.FriendRequest, 5);

            // Assert
            mockNotificationRepo.Verify(r => r.DeleteAsync(1), Times.Once);
            mockNotificationRepo.Verify(r => r.DeleteAsync(2), Times.Once);
        }

        [Fact]
        public async Task DeleteNotificationsByEntityAsync_NoMatchingNotifications_DeletesNone()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();

            var notifications = new List<Notification>
            {
                new Notification { Id = 1, UserId = "user1", Type = NotificationType.FriendRequest, RelatedEntityId = 10, Message = "Test", IsRead = false, CreatedAt = DateTime.UtcNow }
            };

            mockNotificationRepo.Setup(r => r.GetNotificationsByTypeAsync(NotificationType.FriendRequest, null))
                .ReturnsAsync(notifications);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            await service.DeleteNotificationsByEntityAsync(NotificationType.FriendRequest, 5);

            // Assert
            mockNotificationRepo.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SendNotificationToUserAsync_ValidData_SendsSignalR()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();

            var notification = new NotificationDto { Id = 1, Message = "Test notification" };

            mockClients.Setup(c => c.User("user1")).Returns(mockClientProxy.Object);
            mockHub.Setup(h => h.Clients).Returns(mockClients.Object);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            await service.SendNotificationToUserAsync("user1", notification);

            // Assert
            mockClientProxy.Verify(p => p.SendCoreAsync(
                "ReceiveNotification",
                It.Is<object[]>(o => o.Length == 1),
                default), Times.Once);
        }

        [Fact]
        public async Task SendNotificationCountToUserAsync_ValidData_SendsCount()
        {
            // Arrange
            var mockNotificationRepo = new Mock<INotificationRepository>();
            var mockUserManager = CreateMockUserManager();
            var mockHub = CreateMockHubContext();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();

            mockClients.Setup(c => c.User("user1")).Returns(mockClientProxy.Object);
            mockHub.Setup(h => h.Clients).Returns(mockClients.Object);

            var service = new NotificationService(mockNotificationRepo.Object, mockUserManager.Object, mockHub.Object);

            // Act
            await service.SendNotificationCountToUserAsync("user1", 7);

            // Assert
            mockClientProxy.Verify(p => p.SendCoreAsync(
                "UpdateNotificationCount",
                It.Is<object[]>(o => o.Length == 1 && (int)o[0] == 7),
                default), Times.Once);
        }
    }
}
