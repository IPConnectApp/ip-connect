using ip_connect.Models;

namespace ip_connect.Services.MessageService
{
    public interface IMessageService
    {
        Task<Message> SendMessageAsync(int conversationId, string senderId, string text);
        Task<List<Message>> GetConversationMessagesAsync(int conversationId);
        Task MarkConversationAsReadAsync(int conversationId, string userId);
    }
}