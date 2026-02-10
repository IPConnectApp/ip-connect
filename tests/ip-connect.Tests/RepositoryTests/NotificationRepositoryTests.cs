using FluentAssertions;
using ip_connect.Models;
using ip_connect.Models.Enums;
using ip_connect.Repositories.NotificationRepository;
using ip_connect.Tests.Helpers;

namespace ip_connect.Tests.RepositoryTests
{
    public class NotificationRepositoryTests
    {
        [Fact]
        public async Task GetUserNotificationsAsync_HasNotifications_ReturnsLimitedNotifications()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new NotificationRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            var relatedUser = new ApplicationUser { Id = "user2", UserName = "related" };
            context.Users.AddRange(user, relatedUser);
            await context.SaveChangesAsync();

            // Create 25 notifications
            for (int i = 0; i < 25; i++)
            {
                var notification = new Notification
                {
                    UserId = user.Id,
                    RelatedUserId = relatedUser.Id,
                    Type = NotificationType.FriendRequest,
                    Message = $"Notification {i}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                };
                context.Notifications.Add(notification);
            }
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUserNotificationsAsync(user.Id, 10);

            // Assert
            result.Should().HaveCount(10);
            result.Should().BeInDescendingOrder(n => n.CreatedAt);
        }

        [Fact]
        public async Task GetUserNotificationsAsync_NoNotifications_ReturnsEmptyList()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new NotificationRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUserNotificationsAsync(user.Id);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserNotificationsAsync_DefaultLimit_Returns20()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new NotificationRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            var relatedUser = new ApplicationUser { Id = "user2", UserName = "related" };
            context.Users.AddRange(user, relatedUser);
            await context.SaveChangesAsync();

            // Create 30 notifications
            for (int i = 0; i < 30; i++)
            {
                var notification = new Notification
                {
                    UserId = user.Id,
                    RelatedUserId = relatedUser.Id,
                    Type = NotificationType.FriendRequest,
                    Message = $"Notification {i}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                };
                context.Notifications.Add(notification);
            }
            await context.SaveChangesAsync();

            // Act - No limit specified, should default to 20
            var result = await repository.GetUserNotificationsAsync(user.Id);

            // Assert
            result.Should().HaveCount(20);
        }

        [Fact]
        public async Task GetUnreadNotificationsAsync_HasUnreadNotifications_ReturnsOnlyUnread()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new NotificationRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            var relatedUser = new ApplicationUser { Id = "user2", UserName = "related" };
            context.Users.AddRange(user, relatedUser);
            await context.SaveChangesAsync();

            var unreadNotif = new Notification
            {
                UserId = user.Id,
                RelatedUserId = relatedUser.Id,
                Type = NotificationType.FriendRequest,
                Message = "Unread",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            var readNotif = new Notification
            {
                UserId = user.Id,
                RelatedUserId = relatedUser.Id,
                Type = NotificationType.FriendAccepted,
                Message = "Read",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            };

            context.Notifications.AddRange(unreadNotif, readNotif);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUnreadNotificationsAsync(user.Id);

            // Assert
            result.Should().HaveCount(1);
            result[0].Message.Should().Be("Unread");
            result[0].IsRead.Should().BeFalse();
        }

        [Fact]
        public async Task GetUnreadNotificationsAsync_NoUnreadNotifications_ReturnsEmptyList()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new NotificationRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUnreadNotificationsAsync(user.Id);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUnreadCountAsync_HasUnreadNotifications_ReturnsCorrectCount()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new NotificationRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            var relatedUser = new ApplicationUser { Id = "user2", UserName = "related" };
            context.Users.AddRange(user, relatedUser);
            await context.SaveChangesAsync();

            // Create 5 unread and 3 read notifications
            for (int i = 0; i < 5; i++)
            {
                context.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    RelatedUserId = relatedUser.Id,
                    Type = NotificationType.FriendRequest,
                    Message = "Unread",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            for (int i = 0; i < 3; i++)
            {
                context.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    RelatedUserId = relatedUser.Id,
                    Type = NotificationType.FriendAccepted,
                    Message = "Read",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUnreadCountAsync(user.Id);

            // Assert
            result.Should().Be(5);
        }

        [Fact]
        public async Task GetUnreadCountAsync_NoUnreadNotifications_ReturnsZero()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new NotificationRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUnreadCountAsync(user.Id);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task MarkAsReadAsync_NotificationExists_MarksAsRead()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new NotificationRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            var relatedUser = new ApplicationUser { Id = "user2", UserName = "related" };
            context.Users.AddRange(user, relatedUser);
            await context.SaveChangesAsync();

            var notification = new Notification
            {
                UserId = user.Id,
                RelatedUserId = relatedUser.Id,
                Type = NotificationType.FriendRequest,
                Message = "Test",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.MarkAsReadAsync(notification.Id);

            // Assert
            result.Should().BeTrue();

            var updated = await context.Notifications.FindAsync(notification.Id);
            updated.Should().NotBeNull();
            updated!.IsRead.Should().BeTrue();
        }

        [Fact]
        public async Task MarkAsReadAsync_NotificationDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new NotificationRepository(context);

            // Act
            var result = await repository.MarkAsReadAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task MarkAllAsReadAsync_HasUnreadNotifications_MarksAllAsRead()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new NotificationRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            var relatedUser = new ApplicationUser { Id = "user2", UserName = "related" };
            context.Users.AddRange(user, relatedUser);
            await context.SaveChangesAsync();

            for (int i = 0; i < 5; i++)
            {
                context.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    RelatedUserId = relatedUser.Id,
                    Type = NotificationType.FriendRequest,
                    Message = $"Notification {i}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();

            // Act
            await repository.MarkAllAsReadAsync(user.Id);

            // Assert
            var unreadCount = await repository.GetUnreadCountAsync(user.Id);
            unreadCount.Should().Be(0);
        }

        [Fact]
        public async Task MarkAllAsReadAsync_NoNotifications_DoesNotThrow()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new NotificationRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            Func<Task> act = async () => await repository.MarkAllAsReadAsync(user.Id);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task GetNotificationsByTypeAsync_FiltersByType_ReturnsOnlyMatchingType()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new NotificationRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            var relatedUser = new ApplicationUser { Id = "user2", UserName = "related" };
            context.Users.AddRange(user, relatedUser);
            await context.SaveChangesAsync();

            context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                RelatedUserId = relatedUser.Id,
                Type = NotificationType.FriendRequest,
                Message = "Friend request",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                RelatedUserId = relatedUser.Id,
                Type = NotificationType.FriendAccepted,
                Message = "Friend accepted",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                RelatedUserId = relatedUser.Id,
                Type = NotificationType.FriendRequest,
                Message = "Another friend request",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetNotificationsByTypeAsync(NotificationType.FriendRequest, user.Id);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(n => n.Type == NotificationType.FriendRequest);
        }

        [Fact]
        public async Task GetNotificationsByTypeAsync_NoMatchingType_ReturnsEmptyList()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new NotificationRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetNotificationsByTypeAsync(NotificationType.FriendRequest, user.Id);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteOldReadNotificationsAsync_DeletesOldReadNotifications()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new NotificationRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            var relatedUser = new ApplicationUser { Id = "user2", UserName = "related" };
            context.Users.AddRange(user, relatedUser);
            await context.SaveChangesAsync();

            // Old read notification (should be deleted)
            context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                RelatedUserId = relatedUser.Id,
                Type = NotificationType.FriendRequest,
                Message = "Old read",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddDays(-40)
            });

            // Recent read notification (should NOT be deleted)
            context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                RelatedUserId = relatedUser.Id,
                Type = NotificationType.FriendAccepted,
                Message = "Recent read",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            });

            // Old unread notification (should NOT be deleted)
            context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                RelatedUserId = relatedUser.Id,
                Type = NotificationType.FriendRequest,
                Message = "Old unread",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddDays(-40)
            });
            await context.SaveChangesAsync();

            // Act
            await repository.DeleteOldReadNotificationsAsync(user.Id, 30);

            // Assert
            var remaining = await repository.GetUserNotificationsAsync(user.Id, 100);
            remaining.Should().HaveCount(2);
            remaining.Should().NotContain(n => n.Message == "Old read");
        }

        [Fact]
        public async Task DeleteOldReadNotificationsAsync_NoOldNotifications_DoesNotThrow()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryDbContext();
            var repository = new NotificationRepository(context);

            var user = new ApplicationUser { Id = "user1", UserName = "testuser" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            Func<Task> act = async () => await repository.DeleteOldReadNotificationsAsync(user.Id, 30);

            // Assert
            await act.Should().NotThrowAsync();
        }
    }
}
