using ip_connect.DTOs.Notification;
using ip_connect.Models.Enums;

namespace ip_connect.Services.NotificationService
{
    public interface INotificationService
    {
        // Get user's notifications
        Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, int limit = 20);

        // Get unread notifications
        Task<List<NotificationDto>> GetUnreadNotificationsAsync(string userId);

        // Get unread count
        Task<int> GetUnreadCountAsync(string userId);

        // Mark notification as read
        Task<bool> MarkAsReadAsync(string userId, int notificationId);

        // Mark all notifications as read
        Task<bool> MarkAllAsReadAsync(string userId);

        // Delete notification
        Task<bool> DeleteNotificationAsync(string userId, int notificationId);

        // Create friend request notification
        Task CreateFriendRequestNotificationAsync(string recipientUserId, string senderUserId, int friendshipId);

        // Create friend accepted notification
        Task CreateFriendAcceptedNotificationAsync(string recipientUserId, string acceptedByUserId, int friendshipId);

        // Delete notifications by related entity (for cleanup)
        Task DeleteNotificationsByEntityAsync(NotificationType type, int relatedEntityId);

        Task SendNotificationToUserAsync(string userId, object notification);
        Task SendNotificationCountToUserAsync(string userId, int count);

        // Create friend removed notification
        Task CreateFriendRemovedNotificationAsync(string recipientUserId, string removedByUserId, int friendshipId);
    }
}