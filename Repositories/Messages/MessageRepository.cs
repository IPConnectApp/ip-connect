using ip_connect.Data;
using ip_connect.Models;
using ip_connect.Repositories.MessageRepository;
using Microsoft.EntityFrameworkCore;

namespace ip_connect.Repositories.Messages
{
    public class MessageRepository : Repository<Message>, IMessageRepository
    {

        public MessageRepository(ApplicationDbContext context) : base(context) { }

        public async Task<List<Message>> GetConversationMessagesAsync(int conversationId)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .Include(m => m.Sender)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }
    }
}
