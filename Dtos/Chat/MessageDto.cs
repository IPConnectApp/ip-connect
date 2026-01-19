namespace ip_connect.DTOs.Chat
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderUsername { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}