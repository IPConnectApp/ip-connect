using ip_connect.Data;
using ip_connect.Models;
using ip_connect.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ip_connect.Repositories.NotificationRepository
{
    public class NotificationRepository : Repository<Notification>, INotificationRepository
    {
        public NotificationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int limit = 20)
        {
            return await _dbSet
                .Include(n => n.RelatedUser)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
        {
            return await _dbSet
                .Include(n => n.RelatedUser)
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _dbSet
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            var notification = await GetByIdAsync(notificationId);
            if (notification == null)
                return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(string userId)
        {
            var unreadNotifications = await _dbSet
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Notification>> GetNotificationsByTypeAsync(NotificationType type, string? userId = null)
        {
            var query = _dbSet
                .Include(n => n.RelatedUser)
                .Where(n => n.Type == type);

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(n => n.UserId == userId);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> DeleteOldReadNotificationsAsync(string userId, int daysOld = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

            var oldNotifications = await _dbSet
                .Where(n => n.UserId == userId && n.IsRead && n.CreatedAt < cutoffDate)
                .ToListAsync();

            _dbSet.RemoveRange(oldNotifications);
            await _context.SaveChangesAsync();

            return oldNotifications.Count;
        }
    }
}
