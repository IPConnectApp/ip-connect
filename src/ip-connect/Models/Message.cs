using System.ComponentModel.DataAnnotations;

namespace ip_connect.Models
{
    public class Message
    {
        public int Id { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int ConversationId { get; set; }
        
        [Required]
        [StringLength(450)]
        public string SenderId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string Text { get; set; } = string.Empty;
        
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Conversation Conversation { get; set; } = null!;
        public ApplicationUser Sender { get; set; } = null!;
    }
}