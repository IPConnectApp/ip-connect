using ip_connect.DTOs.Chat;

namespace ip_connect.Services.MessageService
{
    public interface IMessageService
    {
        Task<MessageDto> SendMessageAsync(int conversationId, string senderId, string senderUsername, string text);
        Task<List<MessageDto>> GetConversationMessagesAsync(int conversationId);
        Task MarkConversationAsReadAsync(int conversationId, string userId);
    }
}