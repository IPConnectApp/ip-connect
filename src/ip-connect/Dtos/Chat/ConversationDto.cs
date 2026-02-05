namespace ip_connect.DTOs.Chat
{
    public class ConversationDto
    {
        public int Id { get; set; }
        public bool IsGroup { get; set; }
        public string? OtherUserName { get; set; }
        public string? OtherUserProfilePicture { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? LastMessageText { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }
}