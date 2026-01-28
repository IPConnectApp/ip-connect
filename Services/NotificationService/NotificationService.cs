using ip_connect.DTOs.Notification;
using ip_connect.Exceptions;
using ip_connect.Hubs;
using ip_connect.Models;
using ip_connect.Models.Enums;
using ip_connect.Repositories.NotificationRepository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace ip_connect.Services.NotificationService
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;


        public NotificationService(
            INotificationRepository notificationRepository,
            UserManager<ApplicationUser> userManager,
            IHubContext<NotificationHub> hubContext)
        {
            _notificationRepository = notificationRepository;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, int limit = 20)
        {
            var notifications = await _notificationRepository.GetUserNotificationsAsync(userId, limit);

            return notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                RelatedUserId = n.RelatedUserId,
                RelatedUsername = n.RelatedUser?.UserName,
                RelatedUserProfilePicture = n.RelatedUser?.ProfilePictureUrl,
                RelatedEntityId = n.RelatedEntityId
            }).ToList();
        }

        public async Task<List<NotificationDto>> GetUnreadNotificationsAsync(string userId)
        {
            var notifications = await _notificationRepository.GetUnreadNotificationsAsync(userId);

            return notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                RelatedUserId = n.RelatedUserId,
                RelatedUsername = n.RelatedUser?.UserName,
                RelatedUserProfilePicture = n.RelatedUser?.ProfilePictureUrl,
                RelatedEntityId = n.RelatedEntityId
            }).ToList();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _notificationRepository.GetUnreadCountAsync(userId);
        }

        public async Task<bool> MarkAsReadAsync(string userId, int notificationId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);

            if (notification == null)
                throw new NotFoundException("Notification not found");

            // Only the owner can mark as read
            if (notification.UserId != userId)
                throw new ForbiddenException("You cannot mark this notification as read");

            return await _notificationRepository.MarkAsReadAsync(notificationId);
        }

        public async Task<bool> MarkAllAsReadAsync(string userId)
        {
            return await _notificationRepository.MarkAllAsReadAsync(userId);
        }

        public async Task<bool> DeleteNotificationAsync(string userId, int notificationId)
        {
            var notification = await _notificationRepository.GetByIdAsync(notificationId);

            if (notification == null)
                throw new NotFoundException("Notification not found");

            // Only the owner can delete
            if (notification.UserId != userId)
                throw new ForbiddenException("You cannot delete this notification");

            return await _notificationRepository.DeleteAsync(notificationId);
        }

        public async Task CreateFriendRequestNotificationAsync(string recipientUserId, string senderUserId, int friendshipId)
        {
            var sender = await _userManager.FindByIdAsync(senderUserId);
            if (sender == null)
                return;

            var notification = new Notification
            {
                UserId = recipientUserId,
                Type = NotificationType.FriendRequest,
                RelatedUserId = senderUserId,
                RelatedEntityId = friendshipId,
                Message = $"{sender.UserName} sent you a friend request",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.CreateAsync(notification);
        }

        public async Task CreateFriendAcceptedNotificationAsync(string recipientUserId, string acceptedByUserId, int friendshipId)
        {
            var acceptedBy = await _userManager.FindByIdAsync(acceptedByUserId);
            if (acceptedBy == null)
                return;

            var notification = new Notification
            {
                UserId = recipientUserId,
                Type = NotificationType.FriendAccepted,
                RelatedUserId = acceptedByUserId,
                RelatedEntityId = friendshipId,
                Message = $"{acceptedBy.UserName} accepted your friend request",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.CreateAsync(notification);
        }

        public async Task DeleteNotificationsByEntityAsync(NotificationType type, int relatedEntityId)
        {
            // Get all notifications of this type (no user filter)
            var notifications = await _notificationRepository.GetNotificationsByTypeAsync(type);

            // Filter by related entity ID
            var toDelete = notifications.Where(n => n.RelatedEntityId == relatedEntityId).ToList();

            // Delete each one
            foreach (var notification in toDelete)
            {
                await _notificationRepository.DeleteAsync(notification.Id);
            }
        }

        public async Task SendNotificationToUserAsync(string userId, object notification)
        {
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification);
        }

        public async Task SendNotificationCountToUserAsync(string userId, int count)
        {
            await _hubContext.Clients.User(userId).SendAsync("UpdateNotificationCount", count);
        }
    }
}
