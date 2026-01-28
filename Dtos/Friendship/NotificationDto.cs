using ip_connect.Models.Enums;

namespace ip_connect.DTOs.Notification
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public NotificationType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? RelatedUserId { get; set; }
        public string? RelatedUsername { get; set; }
        public string? RelatedUserProfilePicture { get; set; }
        public int? RelatedEntityId { get; set; }
    }
}
