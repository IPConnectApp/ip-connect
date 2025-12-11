using ip_connect.Data;
using ip_connect.Models;
using Microsoft.EntityFrameworkCore;

namespace ip_connect.Repositories.Messages
{
    public class MessageRepository : IMessageRepository
    {
        private readonly ApplicationDbContext _context;

        public MessageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Message> AddAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<List<Message>> GetAllAsync()
        {
            return await _context.Messages
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task<List<Message>> GetRecentAsync(int count)
        {
            return await _context.Messages
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }
    }
}
