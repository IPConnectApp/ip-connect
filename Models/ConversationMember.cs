using System.ComponentModel.DataAnnotations;

namespace ip_connect.Models
{
    public class ConversationMember
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Conversation ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid conversation ID")]
        public int ConversationId { get; set; }

        [Required(ErrorMessage = "User ID is required")]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastReadAt { get; set; }

        // Navigation properties
        public Conversation Conversation { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}