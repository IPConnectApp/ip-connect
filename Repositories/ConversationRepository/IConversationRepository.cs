using ip_connect.Models;

namespace ip_connect.Repositories.ConversationRepository
{
    public interface IConversationRepository : IRepository<Conversation>
    {
        Task<Conversation?> GetPrivateConversationAsync(string user1Id, string user2Id);
        Task<List<Conversation>> GetUserConversationsAsync(string userId);
    }
}