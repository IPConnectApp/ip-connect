using ip_connect.DTOs.Chat;
using ip_connect.Models;

namespace ip_connect.Services.ConversationService
{
    public interface IConversationService
    {
        //find existing conversation OR create new one (prevents duplicates)
        Task<ConversationDto> GetOrCreatePrivateConversationAsync(string user1Id, string user2Id);

        Task<List<ConversationDto>> GetUserConversationsAsync(string userId);
        
        Task<ConversationDto?> GetConversationByIdAsync(int id);
    }
}