using System.ComponentModel.DataAnnotations;

namespace ip_connect.DTOs.Chat
{
    public class SendMessageDto
    {
        [Required(ErrorMessage = "Conversation ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid conversation ID")]
        public int ConversationId { get; set; }

        [Required(ErrorMessage = "Message text is required")]
        [StringLength(2000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 2000 characters")]
        public string Text { get; set; } = string.Empty;
    }
}