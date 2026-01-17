using ip_connect.Models;

namespace ip_connect.Services.ConversationService
{
    public interface IConversationService
    {
        //find existing conversation OR create new one (prevents duplicates)
        Task<Conversation?> GetOrCreatePrivateConversationAsync(string user1Id, string user2Id);

        Task<List<Conversation>> GetUserConversationsAsync(string userId);
        
        Task<Conversation?> GetConversationByIdAsync(int id);
    }
}