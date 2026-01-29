using ip_connect.Models;
using ip_connect.Models.Enums;

namespace ip_connect.Repositories.NotificationRepository
{
    public interface INotificationRepository : IRepository<Notification>
    {
        // Get all notifications for a user (ordered by newest first)
        Task<List<Notification>> GetUserNotificationsAsync(string userId, int limit = 20);

        // Get unread notifications for a user
        Task<List<Notification>> GetUnreadNotificationsAsync(string userId);

        // Get unread count for a user
        Task<int> GetUnreadCountAsync(string userId);

        // Mark notification as read
        Task<bool> MarkAsReadAsync(int notificationId);

        // Mark all notifications as read for a user
        Task<bool> MarkAllAsReadAsync(string userId);

        // Get notifications by type
        Task<List<Notification>> GetNotificationsByTypeAsync(NotificationType type, string? userId = null);

        // Delete old read notifications (cleanup)
        Task<int> DeleteOldReadNotificationsAsync(string userId, int daysOld = 30);
    }
}