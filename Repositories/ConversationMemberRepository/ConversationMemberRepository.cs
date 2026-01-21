using ip_connect.Data;
using ip_connect.Models;
using Microsoft.EntityFrameworkCore;

namespace ip_connect.Repositories.ConversationMemberRepository
{
    public class ConversationMemberRepository : Repository<ConversationMember>, IConversationMemberRepository
    {
        public ConversationMemberRepository(ApplicationDbContext context) : base(context) { }

        public async Task<ConversationMember?> GetMembershipAsync(int conversationId, string userId)
        {
            return await _context.ConversationMembers
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId
                                        && cm.UserId == userId);
        }

        public async Task UpdateLastReadAtAsync(int conversationId, string userId)
        {
            var membership = await GetMembershipAsync(conversationId, userId);

            if (membership != null)
            {
                membership.LastReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<ConversationMember>> GetConversationMembersAsync(int conversationId)
        {
            return await _context.ConversationMembers
                .Where(cm => cm.ConversationId == conversationId)
                .ToListAsync();
        }
    }
}