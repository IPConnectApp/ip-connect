using ip_connect.Data;
using ip_connect.Models;
using ip_connect.Repositories.ConversationRepository;
using Microsoft.EntityFrameworkCore;

namespace ip_connect.Repositories.ConversationRepository
{
    public class ConversationRepository : Repository<Conversation>, IConversationRepository
    {

        public ConversationRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Conversation?> GetPrivateConversationAsync(string user1Id, string user2Id)
        {
            return await _context.Conversations
                .Include(c => c.Members)
                .Where(c => !c.IsGroup)
                .Where(c => c.Members.Count == 2)
                .Where(c => c.Members.Any(m => m.UserId == user1Id))
                .Where(c => c.Members.Any(m => m.UserId == user2Id))
                .FirstOrDefaultAsync();
        }

        public async Task<List<Conversation>> GetUserConversationsAsync(string userId)
        {
            return await _context.ConversationMembers
                .Where(cm => cm.UserId == userId)
                .Include(cm => cm.Conversation)
                    .ThenInclude(c => c.Members)
                        .ThenInclude(m => m.User)
                .Include(cm => cm.Conversation)
                    .ThenInclude(c => c.Messages)
                .Select(cm => cm.Conversation)
                .Where(c => c.Messages.Any())
                .ToListAsync();
        }
    }
}