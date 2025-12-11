using ip_connect.Models;

namespace ip_connect.Repositories.Messages
{
    public interface IMessageRepository
    {
        Task<Message> AddAsync(Message message);

        Task<List<Message>> GetAllAsync();

        Task<List<Message>> GetRecentAsync(int count);
    }
}
