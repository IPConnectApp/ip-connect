using System.ComponentModel.DataAnnotations;
using ip_connect.Models.Enums;

namespace ip_connect.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public NotificationType Type { get; set; }

        [StringLength(450)]
        public string? RelatedUserId { get; set; }

        public int? RelatedEntityId { get; set; }

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [Required]
        public bool IsRead { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public ApplicationUser? RelatedUser { get; set; }
    }
}