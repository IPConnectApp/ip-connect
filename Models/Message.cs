using System.ComponentModel.DataAnnotations;

namespace ip_connect.Models
{
    public class Message
    {
        public int Id { get; set; }
        
        public int ConversationId { get; set; }
        
        [Required]
        public string SenderId { get; set; } = string.Empty;
        
        [Required]
        public string Text { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public Conversation Conversation { get; set; } = null!;
        public ApplicationUser Sender { get; set; } = null!;
    }
}