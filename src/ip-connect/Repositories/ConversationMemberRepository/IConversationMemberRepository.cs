using ip_connect.Models;

namespace ip_connect.Repositories.ConversationMemberRepository
{
    public interface IConversationMemberRepository : IRepository<ConversationMember>
    {
        Task<ConversationMember?> GetMembershipAsync(int conversationId, string userId);
        Task UpdateLastReadAtAsync(int conversationId, string userId);
        Task<List<ConversationMember>> GetConversationMembersAsync(int conversationId);
    }
}