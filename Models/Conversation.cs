using System.ComponentModel.DataAnnotations;

namespace ip_connect.Models
{
    public class Conversation
    {
        public int Id { get; set; }
        
        [MaxLength(100)]
        public string? Name { get; set; }
        
        public bool IsGroup { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Required]
        public string CreatedBy { get; set; } = string.Empty;
        
        // Navigation properties
        public ApplicationUser Creator { get; set; } = null!;
        public ICollection<ConversationMember> Members { get; set; } = new List<ConversationMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}