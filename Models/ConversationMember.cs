namespace ip_connect.Models
{
    public class ConversationMember
    {
        public int Id { get; set; }
        
        public int ConversationId { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastReadAt { get; set; }
        
        //Many-to-many relationship navigation properties
        public Conversation Conversation { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}