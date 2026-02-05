using ip_connect.Models;

namespace ip_connect.Repositories.MessageRepository
{
    public interface IMessageRepository : IRepository<Message>
    {
        Task<List<Message>> GetConversationMessagesAsync(int conversationId);
    }
}