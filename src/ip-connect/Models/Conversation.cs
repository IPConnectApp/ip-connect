using System.ComponentModel.DataAnnotations;

namespace ip_connect.Models
{
    public class Conversation
    {
        public int Id { get; set; }

        [StringLength(100, ErrorMessage = "Conversation name cannot exceed 100 characters")]
        public string? Name { get; set; }

        [Required]
        public bool IsGroup { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Creator ID is required")]
        [StringLength(450)]
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation properties
        public ApplicationUser Creator { get; set; } = null!;
        public ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}